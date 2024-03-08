using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public record SqlColumnForCodGen(
    SqlColumn Col, 
    int ColIdx,
    string PocoPropertyName,
    string PocoCtorParamName,
    string FullDotnetTypeName);