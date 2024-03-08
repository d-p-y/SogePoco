#!/bin/bash
set -eou pipefail
set -x

DIR=$(realpath $(dirname "$0"))
cd "${DIR}"

DB_NAME='sogepoco_tester_db'
NO_DB_CONNSTR="$*"

#Data Source=127.0.0.1,14332;Initial Catalog=master;Trusted_Connection=False;User id=sogepoco_tester_user;Password=sogepoco_tester_passwd;Connection Timeout=2;TrustServerCertificate=True
TARGET_DB_CONNSTR=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/;Initial Catalog=[^;]+/;Initial Catalog='"${DB_NAME}"'/I')
DB_HOST=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Data Source=([^,;$]+).*/\2/I')
DB_PORT=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Data Source=[^,]+,([^;$]+).*/\2/I')
DB_USER=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)User id=([^;$]+).*/\2/I')
DB_PASSWD=$(echo "${NO_DB_CONNSTR}" | sed --regexp-extended 's/.*(^|;)Password=([^;$]+).*/\2/I')

export TARGET_DB_CONNSTR

rm -rf ../bin
rm -rf ~/.nuget/packages/sogepoco.*
rm -rf ~/.templateengine/packages/SogePoco.*

mkdir -p ../bin

./dotnet_pack_all.sh

unzip -v ../bin/nuget_source/SogePoco.Template.Sqlite.*.nupkg

dotnet new install ../bin/nuget_source/SogePoco.Template.SqlServer.*.nupkg

TEMPLATE_INSTANCE_DIR="${DIR}/../bin/template_playground"

mkdir -p "${TEMPLATE_INSTANCE_DIR}"
cd "${TEMPLATE_INSTANCE_DIR}"

dotnet new sogepoco-template-sqlserver -n VerifyTemplateInstanceApp


#
# prepare database (schema+content)
#
echo "
create database ${DB_NAME};
go
" | sqsh -S "${DB_HOST}:${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWD}" -G 8.0 -a 1 -s '|' -D master

echo "
create table Author (
    Id INTEGER IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FirstName TEXT not null,
    LastName TEXT not null
);

create table Book (
    Id INTEGER IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AuthorId INTEGER NOT NULL,
    Title TEXT not null,
    PublishedYear INTEGER NOT NULL,
    CONSTRAINT fk_author_id FOREIGN KEY (AuthorId) REFERENCES author(id)
);
go
" | sqsh -S "${DB_HOST}:${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWD}" -G 8.0 -a 1 -s '|' -D "${DB_NAME}"

#
# configure source generator
#
cd "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator"
sed --in-place --regexp-extended 's/DeveloperConnectionString\s*=>([^;]+)/DeveloperConnectionString => "'"${TARGET_DB_CONNSTR}"'"/I' SogePocoSqlServerConfig.cs

ESCAPED_DOTS_AND_SLASHES=$(realpath "${TEMPLATE_INSTANCE_DIR}/VerifyTemplateInstanceApp.SogePocoConfigAndSourceGenerator")
ESCAPED_DOTS_AND_SLASHES=$(echo "${ESCAPED_DOTS_AND_SLASHES}" | sed --regexp-extended 's/\./\\./g' | sed --regexp-extended 's/\//\\\//g' )

sed --in-place --regexp-extended 's/SchemaDirPath\s*=>([^;]+)/SchemaDirPath => '"\"${ESCAPED_DOTS_AND_SLASHES}\""'/I' SogePocoSqlServerConfig.cs

#
# fetch db schema for source generator
#
chmod a+x extract-sqlserver.sh
./extract-sqlserver.sh

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

using var dbRaw = new System.Data.SqlClient.SqlConnection(Environment.GetEnvironmentVariable("TARGET_DB_CONNSTR") ?? throw new Exception("Missing conn str env var"));
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

using var dbRaw = new System.Data.SqlClient.SqlConnection(Environment.GetEnvironmentVariable("TARGET_DB_CONNSTR") ?? throw new Exception("Missing conn str env var"));
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
