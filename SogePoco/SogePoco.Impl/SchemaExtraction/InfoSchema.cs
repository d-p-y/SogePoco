using System.Data.Common;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.SchemaExtraction; 

public record InfoSchemaPrimaryKey(string TableSchema, string TableName, int OrdinalPosition, string ColumnName);

public record InfoSchemaForeignKey(
    string ConstraintName, int OrdinalPosition, 
    string ForeignSchema, string ForeignTable, string ForeignColumn, 
    string PrimarySchema, string PrimaryTable, string PrimaryColumn);

public static class InfoSchema {
    public static IAsyncEnumerable<InfoSchemaPrimaryKey> ExtractPrimaryKeys(ISqlParamNamingStrategy naming, DbConnection dbConn) =>
        dbConn.ExecuteMappingMapAsNamesAndObjectArray(
            naming,
            y => new InfoSchemaPrimaryKey(
                TableSchema: y.values[y.names.IndexOf("table_schema")] switch {
                    string s => s,
                    _ => throw new Exception("table_schema is of unknown type")},
                TableName: y.values[y.names.IndexOf("table_name")] switch {
                    string s => s,
                    _ => throw new Exception("table_name is of unknown type")},
                OrdinalPosition: y.values[y.names.IndexOf("ordinal_position")] switch {
                    int i => i,
                    _ => throw new Exception("ordinal_position is of unknown type")},
                ColumnName: y.values[y.names.IndexOf("column_name")] switch {
                    string s => s,
                    _ => throw new Exception("column_name is of unknown type")}),
            @"SELECT kcu.table_schema, kcu.table_name, kcu.ordinal_position-1 as ordinal_position, kcu.column_name
                FROM information_schema.table_constraints as tc 
                JOIN information_schema.key_column_usage as kcu 
                     ON kcu.constraint_name = tc.constraint_name AND
                     kcu.constraint_schema = tc.constraint_schema AND
                     kcu.constraint_name = tc.constraint_name
                WHERE tc.constraint_type = 'PRIMARY KEY'");

    public static IAsyncEnumerable<InfoSchemaForeignKey> ExtractForeignKeys(ISqlParamNamingStrategy naming, DbConnection dbConn) =>
        dbConn.ExecuteMappingMapAsNamesAndObjectArray(
            naming,
            y => new InfoSchemaForeignKey(
                ConstraintName:y.values[y.names.IndexOf("constraint_name")] switch {
                    string s => s,
                    _ => throw new Exception("constraint_name is of unknown type")},
                OrdinalPosition:y.values[y.names.IndexOf("ordinal_position")] switch {
                    int i => i,
                    _ => throw new Exception("ordinal_position is of unknown type")},
                ForeignSchema:y.values[y.names.IndexOf("foreign_schema")] switch {
                    string s => s,
                    _ => throw new Exception("foreign_schema is of unknown type")},
                ForeignTable:y.values[y.names.IndexOf("foreign_table")] switch {
                    string s => s,
                    _ => throw new Exception("foreign_table is of unknown type")},
                ForeignColumn:y.values[y.names.IndexOf("foreign_column")] switch {
                    string s => s,
                    _ => throw new Exception("foreign_column is of unknown type")},
                PrimarySchema:y.values[y.names.IndexOf("primary_schema")] switch {
                    string s => s,
                    _ => throw new Exception("primary_schema is of unknown type")},
                PrimaryTable:y.values[y.names.IndexOf("primary_table")] switch {
                    string s => s,
                    _ => throw new Exception("primary_table is of unknown type")},
                PrimaryColumn:y.values[y.names.IndexOf("primary_column")] switch {
                    string s => s,
                    _ => throw new Exception("primary_column is of unknown type")}),
            @"SELECT tc.constraint_name, kcuForeign.ordinal_position,
                     kcuForeign.table_schema as foreign_schema, kcuForeign.table_name as foreign_table, kcuForeign.column_name as foreign_column,
                     kcuPrimary.table_schema as primary_schema, kcuPrimary.table_name AS primary_table, kcuPrimary.column_name AS primary_column
                FROM information_schema.table_constraints as tc 
                join information_schema.referential_constraints as rc
                     on tc.constraint_name = rc.constraint_name and 
                     tc.constraint_schema = rc.constraint_schema
                JOIN information_schema.key_column_usage as kcuForeign
                     on tc.constraint_name = kcuForeign.constraint_name and
                     tc.constraint_schema = kcuForeign.constraint_schema
                JOIN information_schema.key_column_usage as kcuPrimary
                     on rc.unique_constraint_name = kcuPrimary.constraint_name and
                     rc.unique_constraint_schema = kcuPrimary.constraint_schema and
                     kcuForeign.ordinal_position = kcuPrimary.ordinal_position
                where tc.constraint_type = 'FOREIGN KEY'; ");
}