using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using Xunit;

namespace SogePoco.Impl.Tests.Schemas; 

public class SqlServerTestingSchema : ITestingSchema {
    private static ISqlParamNamingStrategy naming = new SqlServerNaming();
        
    public string? FooTable_ConcurrencyTokenPropertyName => "EntityVersion";
    public string? TableWithCompositePk_ConcurrencyTokenPropertyName => null;
        
    public object FooTable_AboolTrueValue => 1;
    public string FooTable_AboolPropertyName=> "NullableBool";//TODO rename to include 'nullable'. drop 'a'
        
    public string FooTableName => "Foo";
    public string FooTable_IdColumnName => "Id";
    public string FooTable_AboolColumnName => "NullableBool";
    public string TableWithCompositePkName => "TableWithCompositePk";
    public string TableWithCompositePk_IdColumnName => "Id";
    public string TableWithCompositePk_YearColumnName => "Year";

    private static string[] Schema = {
        @"CREATE TABLE Foo(
    Id INTEGER IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NullableText NVARCHAR(MAX),
    NotNullableText VARCHAR(MAX) NOT NULL,
    NotNullableIntWithSimpleDefault INTEGER NOT NULL DEFAULT 5,
    NotNullableIntWithComplexDefault INTEGER NOT NULL DEFAULT (abs(1006 % 100)),
    NotNullTextWithMaxLength NVARCHAR(200) NOT NULL,

    NullableInt INTEGER,
    NotNullableInt INTEGER NOT NULL,

    NullableBool bit,
    NotNullableBool bit NOT NULL,

    NullableDecimal numeric(18,5),
    NotNullableDecimal decimal(12,2) NOT NULL,
    
    NullableDateTime datetime2,
    NotNullableDateTime datetime2 NOT NULL,
    
    NullableBinaryData varbinary(max),
    NotNullableBinaryData varbinary(max) NOT NULL,

    FirstComputed AS (isnull(nullableText,'') + '1'),
    SecondComputed AS (notNullableIntWithSimpleDefault + 1) PERSISTED,
    EntityVersion ROWVERSION NOT NULL );",

        @"CREATE TABLE ChildOfFoo(
    Id INTEGER IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FooId INTEGER NOT NULL,
    SiblingId INTEGER,    
    EntityVersion TIMESTAMP NOT NULL,
    
    CONSTRAINT FK_ChildOfFoo_fooId FOREIGN KEY (fooId) REFERENCES Foo(id),
    CONSTRAINT FK_ChildOfFoo_siblingId FOREIGN KEY (siblingId) REFERENCES ChildOfFoo(id));",

        @"CREATE TABLE TableWithCompositePk(
    Id INTEGER NOT NULL,
	Year int NOT NULL,	
    Value NVARCHAR(MAX),    	
	CONSTRAINT PK_TableWithCompositePk PRIMARY KEY (id, year) );",

        @"CREATE TABLE ReferencesTableWithCompositePk(
    Id int identity(1,1) NOT NULL PRIMARY KEY,	
	ParentId int,
	ParentYear int,	
	FooId INTEGER,	
    Val NVARCHAR(MAX),	
	CONSTRAINT FK_ReferencesTableWithCompositePk_to_TableWithCompositePk 
		FOREIGN KEY (parentId, parentYear)
		REFERENCES TableWithCompositePk(id, year),		
	CONSTRAINT FK_ReferencesTableWithCompositePk_to_Foo
		FOREIGN KEY (fooId)
		REFERENCES foo(id) );" };
        
    public async Task CreateSchema(DbConnection dbConn) {
        foreach (var ddlQuery in Schema) {
            await dbConn.ExecuteNonQueryAsync(naming, ddlQuery);
        }
    }

    public async Task<TestData> CreateData(DbConnection dbConn) {
        var result = new TestData(
            Foos:new List<IDictionary<string, object?>>(), 
            ChildOfFoos:new List<IDictionary<string, object?>>());

        var (fooId, fooEntityVersion) = await dbConn.ExecuteMappingMapAsObjectArray(
            naming, 
            x => ((int)x[0], (byte[])x[1]), 
            @"
insert into Foo
    (nullableText, notNullableText, notNullTextWithMaxlength,
        NotNullableInt, NotNullableBool, NotNullableDecimal, NotNullableDateTime, NotNullableBinaryData)
    values
    ('nt', 'nnt', 'nntwml',
        11, cast(1 as bit), 1.1, @0, @1); 
SELECT Id,EntityVersion from Foo where id=SCOPE_IDENTITY();", new DateTime(2001, 02, 03), Array.Empty<byte>()).SingleAsync();
            
        result.Foos.Add(new Dictionary<string, object?> {
            {"Id", fooId},
            {"EntityVersion", fooEntityVersion},
            {"NullableText", "nt"},
            {"NotNullableText", "nnt"},
            {"NotNullTextWithMaxLength", "nntwml"},
            {"NotNullableIntWithSimpleDefault", 5},
            {"NotNullableIntWithComplexDefault", 6},
                
            {"NullableInt", null},
            {"NotNullableInt", 11},
                
            {"NullableBool", null},
            {"NotNullableBool", true},
                
            {"NullableDecimal", null},
            {"NotNullableDecimal", 1.1m},
                
            {"NullableDateTime", null},
            {"NotNullableDateTime", new DateTime(2001,2,3)},
                
            {"NullableBinaryData", null},
            {"NotNullableBinaryData", new byte[0]},
                
            {"FirstComputed", "nt1"},
            {"SecondComputed", 6} });

        Assert.Equal(
            1,
            await dbConn.ExecuteScalarAsync(naming, "select count(*) from Foo where nullableText = @0", "nt"));
           
        var (siblingLessId, siblingLessEntityVersion) = await dbConn.ExecuteMappingMapAsObjectArray(
            naming,
            x => ((int)x[0], (byte[])x[1]),
            @"
insert into ChildOfFoo
    (fooId)
    values
    (@0); 
SELECT Id,EntityVersion from ChildOfFoo where id=SCOPE_IDENTITY();", fooId).SingleAsync();
            
        result.ChildOfFoos.Add(new Dictionary<string, object?> {
            {"Id", siblingLessId},
            {"EntityVersion", siblingLessEntityVersion},
            {"FooId", fooId},
            {"SiblingId", null}} );
            
        var (siblingOwningId, siblingOwningEntityVersion) = await dbConn.ExecuteMappingMapAsObjectArray(
            naming,
            x => ((int)x[0], (byte[])x[1]),
            @"
insert into ChildOfFoo
    (fooId, siblingId)
    values
    (@0, @1); 
SELECT Id,EntityVersion from ChildOfFoo where id=SCOPE_IDENTITY();", fooId, siblingLessId).SingleAsync();

        result.ChildOfFoos.Add(new Dictionary<string, object?> {
            {"Id", siblingOwningId},
            {"EntityVersion", siblingOwningEntityVersion},
            {"FooId", fooId},
            {"SiblingId", siblingLessId}} );

        Assert.Equal(
            siblingLessId,
            Convert.ToDecimal(await dbConn.ExecuteScalarAsync(naming, "select id from ChildOfFoo where siblingId is null")));

        Assert.Equal(
            siblingOwningId,
            Convert.ToDecimal(await dbConn.ExecuteScalarAsync(naming, "select id from ChildOfFoo where siblingId is not null")));

        return result;
    }

    public IEnumerable<SqlTable> GetAsSyntheticModel() => new SqlTable[] {
        new("dbo", "ChildOfFoo",
            new HashSet<SqlColumn> {
                new(Name: "Id", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:true, IsConcurrencyToken:false),
                new(Name: "FooId", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "SiblingId", Type: "INT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "EntityVersion", Type: "TIMESTAMP", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:true) },
            new HashSet<SqlForeignKey> {
                new(PrimaryKeySchema:"dbo", PrimaryKeyTableName: "Foo",
                    new[] {(foreignColumnName: "FooId", primaryColumnName: "Id")}.ToHashSet()),
                new(PrimaryKeySchema:"dbo", PrimaryKeyTableName: "ChildOfFoo",
                    new[] {(foreignColumnName: "SiblingId", primaryColumnName: "Id")}.ToHashSet()) }),
        new("dbo", "Foo",
            new HashSet<SqlColumn> {
                new(Name: "Id", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:true, IsConcurrencyToken:false),
                new(Name: "NullableText", Type: "NVARCHAR(-1)", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableText", Type: "VARCHAR(-1)", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableIntWithSimpleDefault", Type: "INT", Nullable: false, DefaultValue: "((5))", 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableIntWithComplexDefault", Type: "INT", Nullable: false, DefaultValue: "(abs((1006)%(100)))", 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullTextWithMaxLength", Type: "NVARCHAR(200)", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NullableInt", Type: "INT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableInt", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NullableBool", Type: "BIT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableBool", Type: "BIT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NullableDecimal", Type: "NUMERIC", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableDecimal", Type: "DECIMAL", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NullableDateTime", Type: "DATETIME2", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableDateTime", Type: "DATETIME2", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NullableBinaryData", Type: "VARBINARY(-1)", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "NotNullableBinaryData", Type: "VARBINARY(-1)", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "EntityVersion", Type: "TIMESTAMP", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:true),
                new(Name: "FirstComputed", Type: "NVARCHAR(-1)", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "SecondComputed", Type: "INT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false) },
            new HashSet<SqlForeignKey> { }),
        new("dbo", "ReferencesTableWithCompositePk",
            new HashSet<SqlColumn> {
                new(Name: "Id", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:true, IsConcurrencyToken:false),
                new(Name: "ParentId", Type: "INT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "ParentYear", Type: "INT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "FooId", Type: "INT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "Val", Type: "NVARCHAR(-1)", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false) },
            new HashSet<SqlForeignKey> {
                new(PrimaryKeySchema:"dbo", PrimaryKeyTableName: "Foo",
                    new[] {(foreignColumnName: "FooId", primaryColumnName: "Id")}.ToHashSet()),
                new(PrimaryKeySchema:"dbo", PrimaryKeyTableName: "TableWithCompositePk", new[] {
                    (foreignColumnName: "ParentId", primaryColumnName: "Id"),
                    (foreignColumnName: "ParentYear", primaryColumnName: "Year") }.ToHashSet()) }),
        new("dbo", "TableWithCompositePk",
            new HashSet<SqlColumn> {
                new(Name: "Id", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "Year", Type: "INT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 1, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "Value", Type: "NVARCHAR(-1)", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false) },
            new HashSet<SqlForeignKey> { })  };        
}