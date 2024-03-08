# pre-video cleanup

```sh
rm -rf ~/.nuget/packages/sogepoco.*
rm -rf ~/.templateengine/packages/SogePoco.*
mkdir -p ~/Playground
rm -rf ~/Playground/SogePocoIntroduction
cd ~/Playground
clear
```

# video

```sh
# let's start in some empty directory
mkdir SogePocoIntroduction
cd SogePocoIntroduction

# sogepoco currently supports Postgresql, Sql Server and Sqlite
# Let's use Sqlite for brevity
# First, we need to install solution template from nuget
dotnet new install SogePoco.Template.Sqlite

# Now let's instantiate that newly installed template
dotnet new sogepoco-template-sqlite -n SogePocoIntroduction

# solution with four projects were created
mc

# show content of solution permanently on terminal for explanation
ls -R

# `SogePocoIntroduction` is an executable console app. It relies on 
# `SogePocoIntroduction.SogePocoConfigAndSourceGenerator` source generator project. 
# That generator project generates POCO classes in `SogePocoIntroduction.Pocos` project.
# Shortly: it lets execute type safe insert/update/delete SQL statements.
# Generator also relies on those POCO classes to produce type safe selects SQL statements in 
# `SogePocoIntroduction.Queries` project. 
# note: dotnet doesn't allow to chain source generators hence two projects (Pocos and Queries) are required.

# let's use sogepoco to query example database
# we need example database. Either download it from github
#    https://raw.githubusercontent.com/d-p-y/sogepoco/master/docs/example_database.sqlite
# or copy it locally from checked out repo
cp ~/Projects/soge_poco/docs/example_database.sqlite .

# let's configure app. we need 
# 1) dev connection string for db schema extractor
# 2) directory path for serialized database schema (dbschema.json file)
# let's get full path to example db for copying it into clipboard
ls
echo `pwd`/example_database.sqlite

cd SogePocoIntroduction.SogePocoConfigAndSourceGenerator
ls
kate SogePocoSqliteConfig.cs 

# we need to call db schema extractor now so that source generator has all it needs to work
# but first look at its content

cat extract-sqlite.sh

# for ease of use it relies on dotnet-script hence we don't need another project in solution
dotnet tool install -g dotnet-script

# now we can call extraction process
chmod a+x extract-sqlite.sh
./extract-sqlite.sh

# let's quickly peek at example database (using `Db Browser`)

# let's switch to Rider now to see how generated classes look like and see how queries are requested
cd ..
/opt/rider/bin/rider.sh SogePocoIntroduction.sln >/dev/null 2>&1
```

* clean, rebuild
* edit InvokeGenerator.cs in Queries project and show that `Foo` class is known in Queries project

* edit InvokeGenerator.cs in Queries project and change it to be
```
using SogePoco.Common;

namespace SogePocoIntroduction.Queries;

[GenerateQueries]
public class InvokeGenerator {
    //TODO let's write equivalent of: select * from Foo where NotNullableText = 'nnt'    
}
```

//public void GetFoo(string ntVal) => Query.Register((Foo x) => x.NotNullableText == ntVal);

* edit Program.cs
```

//for clear flexible design, sogepoco doesn't manage connections. 
// We need to provide it an opened connection already
var connStr = 
    "Data Source=/home/dominik/Playground/SogePocoIntroduction/example_database.sqlite;Cache=Shared";
using var dbRaw = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
await dbRaw.OpenAsync();

//TODO create Database class instance and pass it opened `dbRaw` connection

//TODO call generated query and show id and content of another property `NullableText`
//TODO modify Foo record to change `NullableText` and re-query to show changes

Console.WriteLine($"ending");
```

* add manually to show code completion working

```
var db = new SogePocoIntroduction.Database(dbRaw);
await foreach (var foo in db.GetFoo("nt")) {
    Console.WriteLine($"Found foo id={foo.Id} NullableText={foo.NullableText}");
}
```

* change manually to show that updates are possible

```
var db = new SogePocoIntroduction.Database(dbRaw);
await foreach (var foo in db.GetFoo("nt")) {
    Console.WriteLine($"Modifying id={foo.Id} from {foo.NullableText}");

    foo.NullableText = "fffff";
    await db.Update();    
}

await foreach (var foo in db.GetFoo("nt")) {
    Console.WriteLine($"Found foo id={foo.Id} having NullableText={foo.NullableText}");
}
```