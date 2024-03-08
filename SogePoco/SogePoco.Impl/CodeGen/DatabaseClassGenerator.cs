using System.Text.RegularExpressions;
using SogePoco.Common;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.CodeGen; 

public static class DatabaseClassGenerator {
    public static string BuildInsertMethodsCsCode(
            GeneratorOptions options,
            IReadOnlyCollection<SqlTableForCodGen> model, 
            ISqlParamNamingStrategy naming, 
            IDatabaseDotnetDataMapperGenerator mapper) =>
        model
            .Where(tbl => options.ShouldGenerateMethod((PocoMethod.Insert, (tbl.Tbl.Schema,tbl.Tbl.Name))))
            .Select(tbl => {
                var insertableCols = tbl.SortedColumns.Where(y => !y.Col.UniqueIdentityGeneratedByDb && !y.Col.IsComputedColumn);
                    
                var createSqlParamsFromInsertableColumns =
                    insertableCols
                        .Select(col => {

                            return col.Col.DefaultValue != null
                                ? $@"            if (_defaultableColumnShouldInsert((typeof({tbl.FullClassName}), nameof(p.{col.PocoPropertyName}), p, p.{col.PocoPropertyName}))) {{
{BuildCmdParametersAddCsCodeAutoNaming(mapper, indentLevel:4, naming, tbl, col)}
                insertColNames+=""{col.Col.Name},"";
                insertParamNames+=""{naming.NameForParameterUsage(col.ColIdx)},"";
            }}"
                                :  $@"{BuildCmdParametersAddCsCodeAutoNaming(mapper, indentLevel:3, naming, tbl, col)}
            insertColNames+=""{col.Col.Name},"";
            insertParamNames+=""{naming.NameForParameterUsage(col.ColIdx)},"";"; })
                        .ConcatenateUsingSep("\n");
                    
                var postInsertSelectPopulateProperies =
                    tbl.SortedColumns.Select(p =>
                            "p." + p.PocoPropertyName +
                            " = " +
                            mapper.CsCodeToMapDatabaseRawObjectToPoco(tbl.Tbl, p.Col, "rdr.GetValue(iCol++)") +";" )
                        .ConcatenateUsingSep("\n                ");

                var pkColsWithAutoIncrement = tbl.SortedColumns
                    .Where(x => x.Col.PrimaryKeyIdx.HasValue && x.Col.UniqueIdentityGeneratedByDb)
                    .Select(x => mapper.QuoteSqlIdentifier(x.Col.Name))
                    .ToList();
                var pkColWithoutAutoIncrementNameToParamName = tbl.SortedColumns
                    .Where(x => x.Col.PrimaryKeyIdx.HasValue && !x.Col.UniqueIdentityGeneratedByDb)
                    .Select(col => (mapper.QuoteSqlIdentifier(col.Col.Name), naming.NameForParameter(col.ColIdx)) )
                    .ToList();
                    
                var insertThenSelect = mapper.GenerateInsertFromParametersThenSelect(
                    tableNameWithSchema:$"{mapper.QuoteSqlIdentifier(tbl.Tbl.Schema)}.{mapper.QuoteSqlIdentifier(tbl.Tbl.Name)}", 
                    insertColumnNamesVar:"insertColNames",
                    insertColumnParamsVar:"insertParamNames",
                    selectableColumnsNames:tbl.SortedColumns
                        .Select(c => mapper.QuoteSqlIdentifier(c.Col.Name))
                        .ConcatenateUsingComma(),
                    pkColsWithAutoIncrement:pkColsWithAutoIncrement,
                    pkColWithoutAutoIncrement:pkColWithoutAutoIncrementNameToParamName);

                return
                    $@"        public async System.Threading.Tasks.Task Insert({tbl.FullClassName} p) {{
            var insertColNames = """";
            var insertParamNames = """";            
            await using var cmd = {Consts.GeneratedDatabaseClassDbConnFieldName}.CreateCommand();
            InitTransaction(cmd);

{createSqlParamsFromInsertableColumns}
            insertColNames = insertColNames.TrimEnd(',');
            insertParamNames = insertParamNames.TrimEnd(',');

            cmd.CommandText = ${insertThenSelect.StringAsCsCodeStringValue()};

            LastSqlText = cmd.CommandText;
            LastSqlParams = cmd.Parameters.Cast<System.Data.Common.DbParameter>().ToArray();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync()) {{
                var iCol = 0;
                {postInsertSelectPopulateProperies}

                return;
            }}

            throw new System.Exception(""Unexpectedly select after insert didn't yield result"");
        }} ";})
            .ToList()
            .ConcatenateUsingSep("\n");

    private static string BuildCmdParametersAddCsCodeAutoNaming(
        IDatabaseDotnetDataMapperGenerator mapper, int indentLevel,  ISqlParamNamingStrategy naming,
        SqlTableForCodGen tbl, SqlColumnForCodGen col) =>
        BuildCmdParametersAddCsCode(
            indentLevel, 
            naming.Build(col.ColIdx), 
            $"p.{col.PocoPropertyName}",
            mapper.CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(tbl.Tbl, col.Col));

    private static string BuildCmdParametersAddCsCode(IDatabaseDotnetDataMapperGenerator mapper, int indentLevel, SqlParamInfo inp) {
            
        if (mapper.CustomCsCodeToMapDotnetValueAsSqlParameter(inp.Name, inp.SourceDotnetTypeName, inp.SourceCsValue) is {} val) {
            val = val.Replace(
                "\n", 
                "\n" + new string(' ', 4 * indentLevel));
            return val;
        }

        return BuildCmdParametersAddCsCode(
            indentLevel, 
            inp.Name,
            inp.SourceCsValue,
            mapper.CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(inp.SourceDotnetTypeName));
    }
        
    private static string BuildCmdParametersAddCsCode(
        int indentLevel, SqlParamNamingResult name, string paramValueAsCs, string? fourthOptionalParam) {
            
        string Indented(int l) => new(' ', 4 * l);

        if (fourthOptionalParam != null) {
            fourthOptionalParam = @$",
{Indented(indentLevel + 2)}{fourthOptionalParam}";
        } else {
            fourthOptionalParam = "";
        }
            
        return 
            $@"{Indented(indentLevel)}cmd.Parameters.Add(
{Indented(indentLevel+1)}CreateParam(
{Indented(indentLevel+2)}cmd,
{Indented(indentLevel+2)}{name.LogicalName.StringAsCsCodeStringValue()}, 
{Indented(indentLevel+2)}((object?){paramValueAsCs} ?? System.DBNull.Value){fourthOptionalParam}));";
    }
        
    public static string BuildDeleteMethodsCsCode(
            GeneratorOptions options,
            IReadOnlyCollection<SqlTableForCodGen> model, ISqlParamNamingStrategy naming, 
            IDatabaseDotnetDataMapperGenerator mapper) =>
        model
            .Where(tbl => options.ShouldGenerateMethod((PocoMethod.Delete, (tbl.Tbl.Schema,tbl.Tbl.Name))))
            .Select(tbl => {
                var identifierCols = tbl.SortedColumns
                    .Where(y => y.Col.PrimaryKeyIdx.HasValue || y.Col.IsConcurrencyToken)
                    .ToList();
                    
                var identifierColNameToParamName =
                    identifierCols
                        .Select(col => $"{mapper.QuoteSqlIdentifier(col.Col.Name)} = {naming.NameForParameterUsage(col.ColIdx)}")
                        .ConcatenateUsingSep(" AND ");

                var createSqlParams =
                    identifierCols
                        .Select(x => BuildCmdParametersAddCsCodeAutoNaming(mapper, indentLevel:3, naming, tbl, x))
                        .ConcatenateUsingSep("\n");
                    
                var delete = mapper.GenerateDeleteFromParameters(
                    tableNameWithSchema:$"{mapper.QuoteSqlIdentifier(tbl.Tbl.Schema)}.{mapper.QuoteSqlIdentifier(tbl.Tbl.Name)}",
                    identifierColNameToParamName:identifierColNameToParamName);

                return
                    $@"        public async System.Threading.Tasks.Task<long> Delete({tbl.FullClassName} p) {{
            await using var cmd = {Consts.GeneratedDatabaseClassDbConnFieldName}.CreateCommand();
            InitTransaction(cmd);

{createSqlParams}
            
            cmd.CommandText = {delete.StringAsCsCodeStringValue()};

            LastSqlText = cmd.CommandText;
            LastSqlParams = cmd.Parameters.Cast<System.Data.Common.DbParameter>().ToArray();
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected != 1) {{
                _onRowsAffectedExpectedToBeExactlyOne($""delete of {tbl.FullClassName} has {{rowsAffected}} instead of 1"");
            }}
             
            return rowsAffected;
        }} ";})
            .ToList()
            .ConcatenateUsingSep("\n");
        
    public static string BuildUpdateMethodsCsCode(
            GeneratorOptions options,
            IReadOnlyCollection<SqlTableForCodGen> model, ISqlParamNamingStrategy naming, 
            IDatabaseDotnetDataMapperGenerator mapper) =>
        model
            .Where(tbl => options.ShouldGenerateMethod((PocoMethod.Update, (tbl.Tbl.Schema,tbl.Tbl.Name))))
            .Select(tbl => {
                var (nonIdentifierCols, rawIdentifierCols) = tbl.SortedColumns
                    .Partition(y => !y.Col.PrimaryKeyIdx.HasValue && !y.Col.IsConcurrencyToken);
                var identifierCols = rawIdentifierCols.ToList();
                        
                var updateableCols = nonIdentifierCols
                    .Where(x => !x.Col.IsComputedColumn)
                    .ToList();

                var updatableColNameToParamName =
                    updateableCols
                        .Select(col => $"{mapper.QuoteSqlIdentifier(col.Col.Name)} = {naming.NameForParameterUsage(col.ColIdx)}")
                        .ConcatenateUsingComma();
                        
                var identifierColsUpdateData =
                    identifierCols
                        .Select(col => (col, colToParamName:$"{mapper.QuoteSqlIdentifier(col.Col.Name)} = {naming.NameForParameterUsage(col.ColIdx)}"))
                        .ToList();

                var createSqlParams =
                    updateableCols
                        .Concat(identifierCols)
                        .Select(x => BuildCmdParametersAddCsCodeAutoNaming(mapper, indentLevel:3, naming, tbl, x))
                        .ConcatenateUsingSep("\n");
                    
                var postUpdateSelectPopulateProperies =
                    tbl.SortedColumns.Select(p =>
                            "p." + p.PocoPropertyName +
                            " = " +
                            mapper.CsCodeToMapDatabaseRawObjectToPoco(tbl.Tbl, p.Col, "rdr.GetValue(iCol++)") +";" )
                        .ConcatenateUsingSep("\n                ");

                var updateThenSelect = mapper.GenerateUpdateFromParametersThenSelect(
                    tableNameWithSchema: $"{mapper.QuoteSqlIdentifier(tbl.Tbl.Schema)}.{mapper.QuoteSqlIdentifier(tbl.Tbl.Name)}", 
                    updatableColNameToParamName: updatableColNameToParamName,
                    identifierCols: identifierColsUpdateData,
                    selectableColumnsNames: tbl.SortedColumns
                        .Select(c => mapper.QuoteSqlIdentifier(c.Col.Name))
                        .ConcatenateUsingComma() );

                return
                    $@"        public async System.Threading.Tasks.Task<long> Update({tbl.FullClassName} p) {{
            await using var cmd = {Consts.GeneratedDatabaseClassDbConnFieldName}.CreateCommand();
            InitTransaction(cmd);

{createSqlParams}
            
            cmd.CommandText = {updateThenSelect.StringAsCsCodeStringValue()};

            LastSqlText = cmd.CommandText;
            LastSqlParams = cmd.Parameters.Cast<System.Data.Common.DbParameter>().ToArray();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync()) {{
                var iCol = 0;
                var rowsAffected = System.Convert.ToInt64(rdr.GetValue(iCol++));
                
                //if column is supported it will have zero-or-positive value
                if (rowsAffected >= 0 && rowsAffected != 1) {{
                    _onRowsAffectedExpectedToBeExactlyOne($""first column in post-update-select of {tbl.FullClassName} has {{rowsAffected}} instead of 1"");
                    return rowsAffected;
                }}

                {postUpdateSelectPopulateProperies}

                if (await rdr.ReadAsync()) {{
                    _onRowsAffectedExpectedToBeExactlyOne(""post-update-select of {tbl.FullClassName} received more than one row"");
                    return 2L;
                }}

                return 1L;
            }}

            _onRowsAffectedExpectedToBeExactlyOne(""Unexpectedly select after update didn't yield result"");
            return 0L;
        }} ";})
            .ToList()
            .ConcatenateUsingSep("\n");

    private static QueryBuildingResult BuildQuery(
            IParameterRequester parameterRequester,
            IDatabaseDotnetDataMapperGenerator mapper, QueryToGenerateTreated query,
            Func<string,bool> paramNameIsUnavailable) 
        => SyntaxToWhereClauseConverter.BuildQuery(parameterRequester, mapper, query, paramNameIsUnavailable);

    public static string MaybeBuildSelectMethodAsCsCode(
            string methodName, TableAndAlias? tblIfQueryIsNull, ISqlParamNamingStrategy naming,
            IDatabaseDotnetDataMapperGenerator mapper, string? extensionOfClassFullName,
            QueryToGenerateTreated? query) {

        var tbls = query != null ? query.Result : tblIfQueryIsNull!.AsSingletonList();
            
        var prmReq = new DefaultParameterRequester(naming);
        bool ParamNameIsUnavailable(string paramName) =>
            Consts.ReservedLiteralsInQueryGenerator.Contains(paramName) ||
            Consts.ReservedNamesInQueryGenerator.Any(re => re.Matches(paramName).Count > 0);

        var maybeBuiltQuery = query != null ? BuildQuery(prmReq, mapper, query, ParamNameIsUnavailable) : null;

        var whereSectionContent = "";
        var sqlParamsCs = "";

        var mthParms = new List<string>();
            
        if (extensionOfClassFullName != null) {
            mthParms.Add($"this {extensionOfClassFullName} self");
        }
            
        if (maybeBuiltQuery is {} builtQuery) {
            if (!string.IsNullOrWhiteSpace(builtQuery.whereClause)) {
                whereSectionContent += $@"WHERE {builtQuery.whereClause}";
            }
            mthParms.AddRange(
                builtQuery.QueryMethodParams
                    .Select(x => 
                        $"{x.DotnetTypeName.NamespaceAndGenericClassName} {x.VariableName}" + 
                        (x.DefaultValueCs==null ? "" : $" = {x.DefaultValueCs}") 
                    ));
                
            sqlParamsCs +=
                builtQuery.SqlParams
                    .OrderBy(x => x.Name.LogicalName)
                    .Select(x => BuildCmdParametersAddCsCode(mapper, indentLevel:3, x))
                    .ConcatenateUsingNewLine();
        }
                
        //TODO add: table schema?
        var selectFragments = new List<string>();

        selectFragments.Add(
            "SELECT" + (
                query?.TakeItemCount is {} x && mapper.GenerateTopItemsCountClause(x) is {} topSql 
                ? $" {topSql}" 
                : "")
            );
        
            
        selectFragments.Add(
            tbls.Select(tbl => 
                    $"    {tbl.PocoType.SortedColumns.Select(c => $"{mapper.QuoteSqlIdentifier(tbl.Alias)}.{mapper.QuoteSqlIdentifier(c.Col.Name)}").ConcatenateUsingSep(", ")}")
                .ConcatenateUsingSep(",\n"));
        
            
        var itmsDecAndiColAdd =
            tbls.SelectI((i, tbl) => {
                var colsCount = tbl.PocoType.SortedColumns.Count;
                var ifRowColIsNullReturnNullElse =
                    tbl.PocoType.SortedColumns
                        .SelectI((i, c) => (i, c.Col.PrimaryKeyIdx.HasValue))
                        .Where(ic => ic.HasValue)
                        .Select(ic => $"rdr.GetValue(iCol+{ic.i}) is System.DBNull")
                        .ConcatenateUsingSep(" && ");
                            
                var constrParamInitFromRdr =
                    tbl.PocoType.SortedColumns
                        .Select(col =>
                            DotnetKeywords.QuoteDotnetVariableIfNeeded(col.PocoCtorParamName) +
                            ":" +
                            mapper.CsCodeToMapDatabaseRawObjectToPoco(tbl.PocoType.Tbl, col.Col, "rdr.GetValue(iCol++)") )
                        .ConcatenateUsingSep(", \n                    ");

                return 
                    !tbl.MayBeNull
                        ? $"                var itm{i} = new {tbl.PocoType.FullClassName}({constrParamInitFromRdr});" 
                        : $@"                var itm{i} = ({ifRowColIsNullReturnNullElse}) ? null : new {tbl.PocoType.FullClassName}(
                    {constrParamInitFromRdr});
                iCol = itm{i} == null ? (iCol+{colsCount}) : iCol;"; 
            }).ConcatenateUsingNewLine();
                
        var returnTypeSpec =
            tbls.Select(t => t.PocoType.FullClassName + (t.MayBeNull ? "?" : ""))
                .ConcatenateUsingSep(",")
                .With(x => tbls.Count <= 1 ? x : $"({x})");
                
        var yieldedValue = tbls.SelectI((i,_) => $"itm{i}")
            .ConcatenateUsingSep(",")
            .With(x => tbls.Count <= 1 ? x : $"({x})");
            
        var mainTbl = 
            (query is { } qry ? qry.AllTables.FirstOrDefault() : tblIfQueryIsNull) 
            ?? throw new Exception("bug: no main table");
            
        selectFragments.Add( 
            @$"FROM {mapper.QuoteSqlIdentifier(mainTbl.PocoType.Tbl.Name)} as {mapper.QuoteSqlIdentifier(mainTbl.Alias)}");
        var orderBySectionContent = "";
            
        if (query is { } q) {
            var joinsContent = q.JoinTables
                .Select(x => {
                    var onColsStr = x.fk.Fk.ForeignToPrimary
                        .Select(y => $"    {mapper.QuoteSqlIdentifier(x.IsInvertedJoin ? x.toAlias : x.fromAlias)}.{mapper.QuoteSqlIdentifier(y.foreignColumnName)} = {mapper.QuoteSqlIdentifier(x.IsInvertedJoin ? x.fromAlias : x.toAlias)}.{mapper.QuoteSqlIdentifier(y.primaryColumnName)}")
                        .ConcatenateUsingSep(" AND ");
                    
                    return 
                        @$"{x.GetJoinTypeAsSqlLiteral()} {mapper.QuoteSqlIdentifier(x.to.Tbl.Name)} as {mapper.QuoteSqlIdentifier(x.toAlias)} ON {onColsStr}"; })
                .ConcatenateUsingNewLine();

            if (!string.IsNullOrWhiteSpace(joinsContent)) {
                selectFragments.Add(joinsContent);
            }

            if (q.OrderBy.Any()) {
                orderBySectionContent = q.OrderBy
                    .Select(x => $"{mapper.QuoteSqlIdentifier(x.tbl.Alias)}.{mapper.QuoteSqlIdentifier(x.col.Col.Name)} {x.ord.AsSqlLiteral()}")
                    .ConcatenateUsingSep(",")
                    .With(x => $"ORDER BY {x}");
            }
        }

        if (!string.IsNullOrWhiteSpace(whereSectionContent)) {
            selectFragments.Add(whereSectionContent);    
        }

        if (!string.IsNullOrWhiteSpace(orderBySectionContent)) {
            selectFragments.Add(orderBySectionContent);
        }

        if (query?.TakeItemCount is {} y && mapper.GenerateLimitItemsCountClause(y) is {} limitSql) {
            selectFragments.Add(limitSql);
        }

        if (query?.PostgresForClause is { } z) {
            selectFragments.Add(z);
        }
        
        var maybeStatic = extensionOfClassFullName != null ? "static " : "";
        var thisPrefix = extensionOfClassFullName != null ? "self." : "";
        var mthParmsCs = mthParms.ConcatenateUsingSep(", ");
        var select = selectFragments.ConcatenateUsingNewLine() + ";";
        
        return
            $@"        public {maybeStatic}async System.Collections.Generic.IAsyncEnumerable<{returnTypeSpec}> {methodName}({mthParmsCs}) {{
            await using var cmd = {thisPrefix}CreateCommand();
{sqlParamsCs}                        
            cmd.CommandText = {select.StringAsCsCodeStringValue()};
            {thisPrefix}LastSqlText = cmd.CommandText;
            {thisPrefix}LastSqlParams = cmd.Parameters.Cast<System.Data.Common.DbParameter>().ToArray();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync()) {{
                var iCol = 0;

{itmsDecAndiColAdd}
                
                yield return {yieldedValue};
            }}            
        }} ";
    }

    public static string BuildFetchMethodsCsCode(
            GeneratorOptions options, IReadOnlyCollection<SqlTableForCodGen> model, 
            IDatabaseDotnetDataMapperGenerator mapper, ISqlParamNamingStrategy naming) =>
        model
            .Where(tbl => options.ShouldGenerateMethod((PocoMethod.Select, (tbl.Tbl.Schema,tbl.Tbl.Name))))
            .Select(x => MaybeBuildSelectMethodAsCsCode(
                $"Fetch{x.BaseClassName}",
                new TableAndAlias(x, mapper.BuildTableAliasProvider()(), false),
                naming,
                mapper,
                extensionOfClassFullName:null,
                query:null))
            .ToList()
            .ConcatenateUsingSep("\n");

    public static string CreateSqlParamMethods(string adoDbConnectionFullClassName) {
        var sqlCreateParamMethods = new List<string>();

        sqlCreateParamMethods.Add($@"
        private static System.Data.Common.DbParameter CreateParam(System.Data.Common.DbCommand cmd, string n, object? v) {{
            var result = cmd.CreateParameter();
            result.ParameterName = n;
            result.Value = v;
            return result;
        }}
");
            
        if (adoDbConnectionFullClassName == "Npgsql.NpgsqlConnection") {
            sqlCreateParamMethods.Add(
                @"        private static Npgsql.NpgsqlParameter CreateParam(Npgsql.NpgsqlCommand cmd, string n, object? v, NpgsqlTypes.NpgsqlDbType pdt) {{
            var result = cmd.CreateParameter();
            result.ParameterName = n;
            result.Value = v;
            result.NpgsqlDbType = pdt;
            return result;
        }}
");
        }

        if (adoDbConnectionFullClassName == "System.Data.SqlClient.SqlConnection") {
            sqlCreateParamMethods.Add(
                @"        private static System.Data.SqlClient.SqlParameter CreateParam(System.Data.SqlClient.SqlCommand cmd, string n, object? v, System.Data.SqlDbType dt) {{
            var result = cmd.CreateParameter();
            result.ParameterName = n;
            result.Value = v;
            result.SqlDbType = dt;
            return result;
        }}
");
        }

        return sqlCreateParamMethods.ConcatenateUsingNewLine();
    }
        
    public static SimpleNamedFile GenerateDatabaseClass(
            string adoDbConnectionFullClassName,
            string adoDbCommandFullClassName,
            IReadOnlyCollection<SqlTableForCodGen> model,
            ISqlParamNamingStrategy naming,
            GeneratorOptions options,
            IDatabaseDotnetDataMapperGenerator mapper) {
                    
        var perPocoInsertMethods = 
            BuildInsertMethodsCsCode(options, model, naming, mapper)
                .Map(x => string.IsNullOrWhiteSpace(x) ? x : $"\n{x}\n");
        var perPocoUpdateMethods = 
            BuildUpdateMethodsCsCode(options, model, naming, mapper)
                .Map(x => string.IsNullOrWhiteSpace(x) ? x : $"\n{x}\n");
        var perPocoDeleteMethods = 
            BuildDeleteMethodsCsCode(options, model, naming, mapper)
                .Map(x => string.IsNullOrWhiteSpace(x) ? x : $"\n{x}\n");
            
        var perPocoFetchMethods = 
            BuildFetchMethodsCsCode(options, model, mapper, naming)
                .Map(x => string.IsNullOrWhiteSpace(x) ? x : $"\n{x}\n");

        var sqlCreateParamMethods = CreateSqlParamMethods(adoDbConnectionFullClassName);
            
        var databaseClassContent = 
            $@"using System.Linq;

namespace {options.DatabaseClassNameSpace} {{

    public class {options.DatabaseClassSimpleName} : System.IDisposable {{
        private readonly System.Func<(System.Type poco, string pocoPropertyName, object pocoInstance, object? pocoPropertyValue),bool> _defaultableColumnShouldInsert;
        private System.Action<string> _onRowsAffectedExpectedToBeExactlyOne = msg => throw new System.Data.DBConcurrencyException(msg);

        private readonly {adoDbConnectionFullClassName} {Consts.GeneratedDatabaseClassDbConnFieldName};
        public string? LastSqlText {{ get; set;}}
        public System.Data.Common.DbParameter[]? LastSqlParams {{ get; set;}}
        private System.Data.Common.DbTransaction? _tran = null;

        public System.Action<string> OnRowsAffectedExpectedToBeExactlyOne {{
            set => _onRowsAffectedExpectedToBeExactlyOne = value;
        }}

        public {options.DatabaseClassSimpleName}(
                {adoDbConnectionFullClassName} dbConn, 
                System.Func<(System.Type poco, string pocoPropertyName, object pocoInstance, object? pocoPropertyValue),bool> defaultableColumnShouldInsert) {{

            {Consts.GeneratedDatabaseClassDbConnFieldName} = dbConn;
            _defaultableColumnShouldInsert = defaultableColumnShouldInsert;
        }}

        /// constructor ignoring default values on columns
        public {options.DatabaseClassSimpleName}(
                {adoDbConnectionFullClassName} dbConn) {{

            {Consts.GeneratedDatabaseClassDbConnFieldName} = dbConn;
            _defaultableColumnShouldInsert = _ => true;
        }}

        public void Dispose() => {Consts.GeneratedDatabaseClassDbConnFieldName}.Dispose();

        public {adoDbCommandFullClassName} CreateCommand() {{
            var result = {Consts.GeneratedDatabaseClassDbConnFieldName}.CreateCommand();
            InitTransaction(result);
            return result;
        }} 

        private void InitTransaction(System.Data.Common.DbCommand cmd) => cmd.Transaction = _tran;

        public async System.Threading.Tasks.Task<System.Data.Common.DbTransaction> BeginTransactionAsync() {{
            if (_tran != null) {{
                throw new System.Exception(""embedded transaction is not supported"");
            }}
            var t = await {Consts.GeneratedDatabaseClassDbConnFieldName}.BeginTransactionAsync();
            _tran = t;
            return t;
        }}

        public async System.Threading.Tasks.Task CommitAsync(bool autoDisposeTran = true) {{
            var t = _tran;
            
            if (t == null) {{
                throw new System.Exception(""not in transaction"");
            }}

            await t.CommitAsync();

            if (autoDisposeTran) {{
                t.Dispose();
            }}

            _tran = null;
        }}

        public async System.Threading.Tasks.Task RollbackAsync(bool autoDisposeTran = true) {{
            var t = _tran;
            
            if (t == null) {{
                throw new System.Exception(""not in transaction"");
            }}

            await t.RollbackAsync();

            if (autoDisposeTran) {{
                t.Dispose();
            }}

            _tran = null;
        }}
{sqlCreateParamMethods}

{perPocoInsertMethods}
{perPocoUpdateMethods}
{perPocoDeleteMethods}
{perPocoFetchMethods}
    }}
}}
";
        return new SimpleNamedFile("database.cs", databaseClassContent);
    }
}