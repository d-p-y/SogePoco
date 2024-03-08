﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using Xunit;

namespace SogePoco.Impl.Tests.Schemas; 

public class PostgresqlTestingSchema : ITestingSchema {
    private static ISqlParamNamingStrategy naming = new PostgresqlNaming();
        
    public string? FooTable_ConcurrencyTokenPropertyName => "Xmin";
    public string? TableWithCompositePk_ConcurrencyTokenPropertyName => "Xmin";
        
    public object FooTable_AboolTrueValue => true; //TODO drop 'a'
    public string FooTable_AboolPropertyName=> "NullableBool"; //TODO drop 'a'
            
    public string FooTableName => "foo";
    public string FooTable_IdColumnName => "id";
    public string FooTable_AboolColumnName => "nullable_bool"; //TODO rename to include 'nullable'. drop 'a'
    public string TableWithCompositePkName => "table_with_composite_pk";
    public string TableWithCompositePk_IdColumnName => "id";
    public string TableWithCompositePk_YearColumnName => "year";
        
    private static string[] Schema =  {
        @"CREATE TABLE foo(
    id SERIAL NOT NULL PRIMARY KEY,
    nullable_text TEXT,
    not_nullable_text TEXT NOT NULL,
    not_nullable_int_with_simple_default INTEGER NOT NULL DEFAULT 5,
    not_nullable_int_with_complex_default INTEGER NOT NULL DEFAULT (abs(1006 % 100)),
    not_null_text_with_max_length VARCHAR(200) NOT NULL,

    nullable_int INTEGER,
    not_nullable_int INTEGER NOT NULL,

    nullable_bool BOOLEAN,
    not_nullable_bool BOOLEAN NOT NULL,
    
    nullable_decimal NUMERIC,
    not_nullable_decimal NUMERIC NOT NULL,
    
    nullable_date_time timestamp,
    not_nullable_date_time timestamp NOT NULL,
    
    nullable_binary_data bytea,
    not_nullable_binary_data bytea NOT NULL,
    
    first_computed TEXT GENERATED ALWAYS AS (COALESCE(nullable_text,'') || '1') STORED,
    second_computed INTEGER NOT NULL GENERATED ALWAYS AS (not_nullable_int_with_simple_default + 1) STORED);",
            
        @"CREATE TABLE child_of_foo(
    id SERIAL NOT NULL PRIMARY KEY,
    foo_id INTEGER NOT NULL,
    sibling_id INTEGER,    
    CONSTRAINT FK_ChildOfFoo_fooId FOREIGN KEY (foo_id) REFERENCES foo(id),
    CONSTRAINT FK_ChildOfFoo_siblingId FOREIGN KEY (sibling_id) REFERENCES child_of_foo(id) );",
            
        @"CREATE TABLE table_with_composite_pk(
    id INTEGER NOT NULL,
	year int NOT NULL,	
    value TEXT,    	
	CONSTRAINT PK_UsingCompositeKeys PRIMARY KEY (id, year) );",
            
        @"CREATE TABLE references_table_with_composite_pk(
    id SERIAL NOT NULL PRIMARY KEY,	
	parent_id int,
	parent_year int,	
	foo_id INTEGER,	
    val TEXT,	
	CONSTRAINT FK_ReferencesTableWithCompositePk_to_TableWithCompositePk 
		FOREIGN KEY (parent_id, parent_year)
		REFERENCES table_with_composite_pk(id, year),
	CONSTRAINT FK_ReferencesTableWithCompositePk_to_Foo
		FOREIGN KEY (foo_id)
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
            
        var (fooId, fooXmin) = await dbConn.ExecuteMappingMapAsObjectArray(
            naming,
            x => ((int)x[0], (uint)x[1]), 
            @"insert into foo
    (nullable_text, not_nullable_text, not_null_text_with_max_length,
        not_nullable_int, not_nullable_bool, not_nullable_decimal, not_nullable_date_time, not_nullable_binary_data)
    values
    ('nt', 'nnt', 'nntwml',
        11, TRUE, 1.1, :p0, :p1) 
    returning id, xmin;", new DateTime(2001, 02, 03), Array.Empty<byte>()).SingleAsync();
            
        result.Foos.Add(new Dictionary<string, object?> {
            {"Id", fooId},
            {"NullableText", "nt"},
            {"NotNullableText", "nnt"},
            {"NotNullableIntWithSimpleDefault", 5},
            {"NotNullableIntWithComplexDefault", 6},
            {"NotNullTextWithMaxLength", "nntwml"},
                
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
                
            {"Xmin", fooXmin}, 
            {"FirstComputed", "nt1"},
            {"SecondComputed", 6} });

        Assert.Equal(
            1L,
            await dbConn.ExecuteScalarAsync(naming, "select count(*) from Foo where nullable_text = :p0", "nt"));
           
        var (siblingLessId,siblingLessXmin) = await dbConn.ExecuteMappingMapAsObjectArray(
            naming,
            x => ((int)x[0], (uint)x[1]), 
            @"
insert into child_of_foo
    (foo_id)
    values
    (:p0) returning id, xmin;", fooId).SingleAsync();
            
        result.ChildOfFoos.Add(new Dictionary<string, object?> {
            {"Id", siblingLessId},
            {"FooId", fooId},
            {"SiblingId", null}, 
            {"Xmin", siblingLessXmin}} );
            
        var (siblingOwningId,siblingOwningXmin) = await dbConn.ExecuteMappingMapAsObjectArray(
            naming,
            x => ((int)x[0], (uint)x[1]),
            @"
insert into child_of_foo
    (foo_id, sibling_id)
    values
    (:p0, :p1) returning id, xmin;", fooId, siblingLessId).SingleAsync();

        result.ChildOfFoos.Add(new Dictionary<string, object?> {
            {"Id", siblingOwningId},
            {"FooId", fooId},
            {"SiblingId", siblingLessId}, 
            {"Xmin", siblingOwningXmin}} );

        Assert.Equal(
            siblingLessId,
            await dbConn.ExecuteScalarAsync(naming, "select id from child_of_foo where sibling_id is null"));

        Assert.Equal(
            siblingOwningId,
            await dbConn.ExecuteScalarAsync(naming, "select id from child_of_foo where sibling_id is not null"));

        return result;
    }

    public IEnumerable<SqlTable> GetAsSyntheticModel() => new SqlTable[] {
        new("public", "child_of_foo",
            new HashSet<SqlColumn> {
                new(Name: "id", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:true, IsConcurrencyToken:false),
                new(Name: "foo_id", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "sibling_id", Type: "INTEGER", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "xmin", Type: "XID", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:true) },
            new HashSet<SqlForeignKey> {
                new(PrimaryKeySchema:"public", PrimaryKeyTableName: "foo",
                    new[] {(foreignColumnName: "foo_id", primaryColumnName: "id")}.ToHashSet()),
                new(PrimaryKeySchema:"public", PrimaryKeyTableName: "child_of_foo",
                    new[] {(foreignColumnName: "sibling_id", primaryColumnName: "id")}.ToHashSet()) }),
        new("public", "foo",
            new HashSet<SqlColumn> {
                new(Name: "id", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:true, IsConcurrencyToken:false),
                new(Name: "nullable_text", Type: "TEXT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_text", Type: "TEXT", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_int_with_simple_default", Type: "INTEGER", Nullable: false, DefaultValue: "5", 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_int_with_complex_default", Type: "INTEGER", Nullable: false, DefaultValue: "abs((1006 % 100))", 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_null_text_with_max_length", Type: "CHARACTER VARYING(200)", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "nullable_int", Type: "INTEGER", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_int", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "nullable_bool", Type: "BOOLEAN", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_bool", Type: "BOOLEAN", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "nullable_decimal", Type: "NUMERIC", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_decimal", Type: "NUMERIC", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "nullable_date_time", Type: "TIMESTAMP WITHOUT TIME ZONE", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_date_time", Type: "TIMESTAMP WITHOUT TIME ZONE", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "nullable_binary_data", Type: "BYTEA", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "not_nullable_binary_data", Type: "BYTEA", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "xmin", Type: "XID", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:true),
                new(Name: "first_computed", Type: "TEXT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "second_computed", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false) },
            new HashSet<SqlForeignKey> { }),
        new("public", "references_table_with_composite_pk",
            new HashSet<SqlColumn> {
                new(Name: "id", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:true, IsConcurrencyToken:false),
                new(Name: "parent_id", Type: "INTEGER", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "parent_year", Type: "INTEGER", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "foo_id", Type: "INTEGER", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "val", Type: "TEXT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "xmin", Type: "XID", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:true) },
            new HashSet<SqlForeignKey> {
                new(PrimaryKeySchema:"public", PrimaryKeyTableName: "foo",
                    new[] {(foreignColumnName: "foo_id", primaryColumnName: "id")}.ToHashSet()),
                new(PrimaryKeySchema:"public", PrimaryKeyTableName: "table_with_composite_pk", new[] {
                    (foreignColumnName: "parent_id", primaryColumnName: "id"),
                    (foreignColumnName: "parent_year", primaryColumnName: "year") }.ToHashSet()) }),
        new("public", "table_with_composite_pk",
            new HashSet<SqlColumn> {
                new(Name: "id", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 0, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "year", Type: "INTEGER", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: 1, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "value", Type: "TEXT", Nullable: true, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:false, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:false),
                new(Name: "xmin", Type: "XID", Nullable: false, DefaultValue: null, 
                    PrimaryKeyIdx: null, IsComputedColumn:true, 
                    UniqueIdentityGeneratedByDb:false, IsConcurrencyToken:true) },
            new HashSet<SqlForeignKey> { }) };
}