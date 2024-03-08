#!/bin/bash
set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

rm -rf ../bin
rm -rf ~/.nuget/packages/sogepoco.*
rm -rf ~/.templateengine/packages/SogePoco.*

mkdir -p ../bin
DB_FILEPATH=$(realpath "${DIR}/../bin/sogepoco_tester.db")
export DB_FILEPATH

DB_CONNSTR="Data Source=${DB_FILEPATH};Cache=Shared"
export DB_CONNSTR

./dotnet_pack_all.sh

unzip -v ../bin/nuget_source/SogePoco.Template.Sqlite.*.nupkg

dotnet new install ../bin/nuget_source/SogePoco.Template.Sqlite.*.nupkg

TEMPLATE_INSTANCE_DIR="${DIR}/../bin/template_playground"

mkdir -p "${TEMPLATE_INSTANCE_DIR}"
cd "${TEMPLATE_INSTANCE_DIR}"

dotnet new sogepoco-template-sqlite -n VerifyTemplateInstanceApp


#
# prepare database (schema+content)
#

echo "
create table author (
    id INTEGER PRIMARY KEY AUTOINCREMENT not null,
    first_name TEXT not null,
    last_name TEXT not null
);

create table book (
    id INTEGER PRIMARY KEY AUTOINCREMENT not null,
    author_id INTEGER NOT NULL,
    title TEXT not null,
    published_year INTEGER NOT NULL,
    CONSTRAINT fk_author_id FOREIGN KEY (author_id) REFERENCES author(id)
);

" | sqlite3 -bail -line -nullvalue NULL "${DB_FILEPATH}"


#
# configure source generator
#
cd "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator"
ESCAPED_DB_FILEPATH=$(echo "${DB_FILEPATH}" | sed --regexp-extended 's/\./\\./g' | sed --regexp-extended 's/\//\\\//g' )
sed --in-place --regexp-extended 's/DeveloperConnectionString\s*=>([^;]+)/DeveloperConnectionString => "'"Data Source=${ESCAPED_DB_FILEPATH};Cache=Shared"'"/I' SogePocoSqliteConfig.cs

ESCAPED_SCHEMA_DIRPATH=$(realpath "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator")
ESCAPED_SCHEMA_DIRPATH=$(echo "${ESCAPED_SCHEMA_DIRPATH}" | sed --regexp-extended 's/\./\\./g' | sed --regexp-extended 's/\//\\\//g' )

sed --in-place --regexp-extended 's/SchemaDirPath\s*=>([^;]+)/SchemaDirPath => '"\"${ESCAPED_SCHEMA_DIRPATH}\""'/I' SogePocoSqliteConfig.cs

# dotnet script bug workaround
echo '
#r "nuget: SQLitePCLRaw.lib.e_sqlite3, 2.1.4"
#r "nuget: SQLitePCLRaw.provider.e_sqlite3, 2.1.4"
Console.WriteLine("");
' > /tmp/script.csx
dotnet script /tmp/script.csx
ln -s ~/.nuget/packages/sqlitepclraw.lib.e_sqlite3/2.1.4/runtimes/linux-x64/native/libe_sqlite3.so ~/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.4/lib/net6.0/libe_sqlite3.so

#
# fetch db schema for source generator
#

chmod a+x extract-sqlite.sh

./extract-sqlite.sh || echo "workaround, checking whether it actually failed"

if [[ ! -f "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator/dbschema.json" ]];
then
    echo "yep, it failed because schema file is not present"
    exit 1
fi

#
# build project -> generator will create POCO classes
#
cd ../VerifyTemplateInstanceApp
dotnet build
dotnet run

#
# verify whether POCO classes are available and actually work
#
echo '
using VerifyTemplateInstanceApp;
using VerifyTemplateInstanceApp.Pocos;

using var dbRaw = new Microsoft.Data.Sqlite.SqliteConnection(Environment.GetEnvironmentVariable("DB_CONNSTR") ?? throw new Exception("Missing conn str env var"));
await dbRaw.OpenAsync();
var db = new VerifyTemplateInstanceApp.Database(dbRaw);

var a1 = new Author();
a1.FirstName = "Douglas";
a1.LastName = "Adams";
await db.Insert(a1);

var b1 = new Book();
b1.AuthorId = a1.Id;
b1.PublishedYear = 1979;
b1.Title = "The Hitchhiker'"'"'s Guide to the Galaxy";
await db.Insert(b1);

var b2 = new Book();
b2.AuthorId = a1.Id;
b2.PublishedYear = 1980;
b2.Title = "The Restaurant at the End of the Universe";
await db.Insert(b2);

Console.WriteLine("authors[0].Id="+a1.Id);
' > Program.cs

dotnet build
ACTUAL_RESULT=$(dotnet run)

if [[ "authors[0].Id=1" != "${ACTUAL_RESULT}" ]];
then
	echo "generated program returned unexpected outcome: ${ACTUAL_RESULT}"
	exit 1
fi

#
# request query generation
#
cd ../VerifyTemplateInstanceApp.Queries

echo '
using VerifyTemplateInstanceApp.Pocos;
using SogePoco.Common;

namespace VerifyTemplateInstanceApp.Queries;

[GenerateQueries]
public class InvokeGenerator {
    public void FetchBooksPublishedInYear(int year) => Query.Register((Book x) => x.PublishedYear == year);
}
' > InvokeGenerator.cs

cd ../VerifyTemplateInstanceApp
dotnet build

#
# verify whether query was generated and actually works
#
echo '
using VerifyTemplateInstanceApp;

using var dbRaw = new Microsoft.Data.Sqlite.SqliteConnection(Environment.GetEnvironmentVariable("DB_CONNSTR") ?? throw new Exception("Missing conn str env var"));
await dbRaw.OpenAsync();
var db = new VerifyTemplateInstanceApp.Database(dbRaw);

var name = (await db.FetchBooksPublishedInYear(1979).ToListAsync()).FirstOrDefault()?.Title;
Console.WriteLine("books[0].Title="+name);
' > Program.cs

dotnet build
ACTUAL_RESULT=$(dotnet run)

EXPECTED="books[0].Title=The Hitchhiker's Guide to the Galaxy"

if [[ "${EXPECTED}" != "${ACTUAL_RESULT}" ]];
then
	echo "generated program returned unexpected outcome: ${ACTUAL_RESULT}"
	exit 1
fi

echo "OK"
