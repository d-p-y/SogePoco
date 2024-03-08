using Newtonsoft.Json;
using SogePoco.Common;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

//TODO skip tables without primary keys?
//TODO add tables without auto_increment to schema + add tests?

namespace SogePoco.Impl.CodeGen; 

public static class PocoClassesGenerator {
    public static PocoSchema BuildRichModel(
        DbSchema model, GeneratorOptions options, ICodeConvention codeConv,
        IDatabaseDotnetDataMapperGenerator mapper) => new(
        model.AdoDbConnectionFullClassName, 
        model.AdoDbCommandFullClassName,
        BuildRichModelForTables(model.Tables, options, codeConv, mapper));
        
    public static IReadOnlyCollection<SqlTableForCodGen> BuildRichModelForTables(
        IReadOnlyCollection<SqlTable> model, GeneratorOptions options, ICodeConvention codeConv, 
        IDatabaseDotnetDataMapperGenerator mapper) {
            
        //1st pass
        List<SqlTableForCodGen> result = model
            .OrderBy(x => x.Schema)
            .ThenBy(x => x.Name)
            .SelectI((iTbl, tbl) => new {
                I = iTbl,
                Tbl = tbl,
                BaseClassName = codeConv.BuildDotnetClassNameFromSqlTableName(iTbl, tbl.Name)
            })
            .Select(x => new SqlTableForCodGen(
                Tbl:x.Tbl,
                TblIdx: x.I,
                BaseClassName: x.BaseClassName,
                FullClassName: options.PocoClassesNameSpace + '.' + x.BaseClassName,
                SortedColumns: codeConv
                    .SortColumns(x.Tbl.Columns.ToList())
                    .SelectI((iCol, col) => new SqlColumnForCodGen(
                        Col: col,
                        ColIdx: iCol,
                        PocoPropertyName: codeConv.BuildDotnetPropertyNameFromSqlColumnName(x.I, x.Tbl.Name, iCol,
                            col.Name),
                        PocoCtorParamName: codeConv.BuildDotnetConstructorParamPropertyNameFromSqlColumnName(x.I,
                            x.Tbl.Name, iCol, col.Name),
                        FullDotnetTypeName: mapper.DbTypeNameToDotnetTypeName(x.Tbl, col)
                            .NamespaceAndGenericClassName))
                    .ToList(),
                SortedForeignKeys: Array.Empty<SqlForeignKeyForCodGen>() // will be initialized in 2nd pass
            ))
            .ToList();

        SqlTableForCodGen FindPocoForTableNameOrFail(string tableName) =>
            result.FirstOrDefault(tbl => options.AreTableNamesTheSame(tbl.Tbl.Name, tableName)) 
            ?? throw new Exception($"could not find poco class name for table {tableName}");

        //2nd pass
        foreach (var i in Enumerable.Range(0, result.Count)) {
            var self = result[i];
            result[i] = self with {
                SortedForeignKeys = 
                codeConv
                    .SortForeignKeys(self.Tbl.ForeignKeys.ToList())
                    .SelectI((iFk, fk) => {
                        var primary = FindPocoForTableNameOrFail(fk.PrimaryKeyTableName);
                        var frgnColNames = fk.ForeignToPrimary.Select(x => x.foreignColumnName).ToList();

                        var name = self
                            .SortedColumns
                            .Select(priCol => frgnColNames.Contains(priCol.Col.Name) ? priCol.PocoPropertyName : null)
                            .WhereIsNotNull()
                            .ToList()
                            .Concatenate();
                        
                        return new SqlForeignKeyForCodGen(
                            Fk: fk,
                            FkIdx: iFk,
                            ForeignPocoFullClassName: primary.FullClassName,
                            DotnetFieldName:$"{primary.BaseClassName}_by_{name}"
                        );
                    })
                    .ToList()
            };
        }
            
        return result;
    }
        
    public static IReadOnlyCollection<SimpleNamedFile> GeneratePocos(
        IReadOnlyCollection<SqlTableForCodGen> model, GeneratorOptions options) =>
        model
            .Select(tbl => {
                var propsAsParamsInCtor =
                    tbl.SortedColumns.Select(col =>
                            col.FullDotnetTypeName + " " + DotnetKeywords.QuoteDotnetVariableIfNeeded(col.PocoCtorParamName))
                        .ConcatenateUsingSep(", ");
                    
                var defaultPropValuesInCtor =
                    tbl.SortedColumns
                        .Select(col => col.FullDotnetTypeName switch {
                            "string" => "\"\"", //nullable reference types 'gotcha'
                            var y when y.EndsWith("[]") => $"System.Array.Empty<{y.Substring(0, y.Length-2)}>()",
                            var y =>  "default(" + y + ")" })
                        .ConcatenateUsingSep(", ");
                    
                var propInitFromParamInCtor = 
                    tbl.SortedColumns.Select(col =>
                            "this." +
                            col.PocoPropertyName +
                            " = " +
                            DotnetKeywords.QuoteDotnetVariableIfNeeded(col.PocoCtorParamName) +
                            ";")
                        .ConcatenateUsingSep("\n            ");

                var propDefinitions =
                    tbl.SortedColumns.Select(col =>
                            "        public " + col.FullDotnetTypeName + " " + col.PocoPropertyName + " {get; set;}")
                        .ConcatenateUsingSep("\n");
                    
                var foreignKeysDefinitions =
                    tbl.SortedForeignKeys.Select(fk =>
                            $"            public SogePoco.Common.JoinInfo<{tbl.BaseClassName},{fk.ForeignPocoFullClassName}> {fk.DotnetFieldName} " +
                            $"                => {Consts.ForeignKeyPropertyBody};")
                        .ConcatenateUsingSep("\n");
                    
                var foreignKeys =
                    !tbl.SortedForeignKeys.Any() ? "" : $@"        
        public class ForeignKeysCollection {{
{foreignKeysDefinitions}
        }}
        public ForeignKeysCollection {Consts.ForeignKeysPropertyName} => {Consts.ForeignKeysPropertyBody};
";
                    
                return new SimpleNamedFile(
                    tbl.Tbl.Name + ".cs",
                    $@"namespace {options.PocoClassesNameSpace} {{
    public class {tbl.BaseClassName} {{
{propDefinitions}
{foreignKeys}

        public {tbl.BaseClassName}({propsAsParamsInCtor}) {{
            {propInitFromParamInCtor}
        }}

        public {tbl.BaseClassName}() : this({defaultPropValuesInCtor}) {{}}

        public override string ToString() => {tbl.Tbl.Name.StringAsCsCodeStringValue()};
    }} 
}}
"); }).ToList();
    
    public static string SerializedDbSchemaDefaultFileName => "dbschema.json";
    public static string SerializedPocoMetadataDefaultFileName => "pocometadata.json";
        
    public static SimpleNamedFile SerializeDbSchema(DbSchema schema) {
        var json = JsonConvert.SerializeObject(schema, Formatting.Indented);
        return new SimpleNamedFile(SerializedDbSchemaDefaultFileName, json);
    }

    public static DbSchema? DeserializeDbSchema(SimpleNamedFile serialized) => 
        JsonConvert.DeserializeObject<DbSchema>(serialized.Content);

    public static DbSchema DeserializeDbSchemaOrFail(SimpleNamedFile serialized) =>
        DeserializeDbSchema(serialized) ?? throw new Exception("deserializing dbschema failed");
        
    public static SimpleNamedFile SerializePocosMetadata(PocoSchema pocoMetadata) {
        var json = JsonConvert.SerializeObject(pocoMetadata, Formatting.Indented);
        return new SimpleNamedFile(SerializedPocoMetadataDefaultFileName, json);
    }

    public static PocoSchema? DeserializePocosMetadata(SimpleNamedFile serialized) =>
        JsonConvert.DeserializeObject<PocoSchema>(serialized.Content);
        
    public static PocoSchema DeserializePocosMetadataOrFail(SimpleNamedFile serialized) =>
        DeserializePocosMetadata(serialized)?? throw new Exception("deserializing pocosmetadata failed");
}