#!/bin/bash
set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

DB_NAME='sogepoco_tester_db'
NO_DB_CONNSTR="${1}"
TARGET_DB_CONNSTR=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/;Database=[^;]+/;Database='"${DB_NAME}"'/I')
DB_HOST=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Host=([^;$]+).*/\2/I')
DB_PORT=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Port=([^;$]+).*/\2/I')
DB_USER=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Username=([^;$]+).*/\2/I')
DB_PASSWD=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Password=([^;$]+).*/\2/I')

export TARGET_DB_CONNSTR
export PGPASSWORD="${DB_PASSWD}"

rm -rf ../bin
rm -rf ~/.nuget/packages/sogepoco.*
rm -rf ~/.templateengine/packages/SogePoco.*

mkdir -p ../bin

./dotnet_pack_all.sh

unzip -v ../bin/nuget_source/SogePoco.Template.Sqlite.*.nupkg

dotnet new install ../bin/nuget_source/SogePoco.Template.Postgres.*.nupkg

TEMPLATE_INSTANCE_DIR="${DIR}/../bin/template_playground"

mkdir -p "${TEMPLATE_INSTANCE_DIR}"
cd "${TEMPLATE_INSTANCE_DIR}"

dotnet new sogepoco-template-postgres -n VerifyTemplateInstanceApp


#
# prepare database (schema+content)
#
echo "create database ${DB_NAME};" | psql --echo-all -d postgres -f - "--host=${DB_HOST}" "--port=${DB_PORT}" -U "${DB_USER}"

echo "
create table author (
    id SERIAL NOT NULL PRIMARY KEY,
    first_name TEXT not null,
    last_name TEXT not null
);

create table book (
    id SERIAL NOT NULL PRIMARY KEY,
    author_id INTEGER NOT NULL,
    title TEXT not null,
    published_year INTEGER NOT NULL,
    CONSTRAINT fk_author_id FOREIGN KEY (author_id) REFERENCES author(id)
);
" | psql --echo-all -d "${DB_NAME}" -f - "--host=${DB_HOST}" "--port=${DB_PORT}" -U "${DB_USER}"



#
# configure source generator
#
cd "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator"
sed --in-place --regexp-extended 's/DeveloperConnectionString\s*=>([^;]+)/DeveloperConnectionString => "'"${TARGET_DB_CONNSTR}"'"/I' SogePocoPostgresConfig.cs

ESCAPED_DOTS_AND_SLASHES=$(realpath "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator")
ESCAPED_DOTS_AND_SLASHES=$(echo "${ESCAPED_DOTS_AND_SLASHES}" | sed --regexp-extended 's/\./\\./g' | sed --regexp-extended 's/\//\\\//g' )

sed --in-place --regexp-extended 's/SchemaDirPath\s*=>([^;]+)/SchemaDirPath => '"\"${ESCAPED_DOTS_AND_SLASHES}\""'/I' SogePocoPostgresConfig.cs

#
# fetch db schema for source generator
#
chmod a+x extract-postgres.sh
./extract-postgres.sh

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

using var dbRaw = new Npgsql.NpgsqlConnection(Environment.GetEnvironmentVariable("TARGET_DB_CONNSTR") ?? throw new Exception("Missing conn str env var"));
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

using var dbRaw = new Npgsql.NpgsqlConnection(Environment.GetEnvironmentVariable("TARGET_DB_CONNSTR") ?? throw new Exception("Missing conn str env var"));
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
