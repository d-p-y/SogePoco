using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public record SqlForeignKeyForCodGen(
    SqlForeignKey Fk,
    int FkIdx,
    string ForeignPocoFullClassName,
    string DotnetFieldName);

public record SqlTableForCodGen(
    SqlTable Tbl,
    int TblIdx,
    string BaseClassName,
    string FullClassName,
    IReadOnlyCollection<SqlColumnForCodGen> SortedColumns,
    IReadOnlyCollection<SqlForeignKeyForCodGen> SortedForeignKeys);
