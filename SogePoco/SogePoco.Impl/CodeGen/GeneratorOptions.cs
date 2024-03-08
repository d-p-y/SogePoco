namespace SogePoco.Impl.CodeGen;

public enum PocoMethod {
    Insert,
    Update,
    Delete,
    /// <summary>unconditional select / select without where / generated method named Fetch{typeNameHere}</summary>
    Select
}

public class GeneratorOptions {
    public string PocoClassesNameSpace { get; set; } = "SogePoco.Pocos";
    public string DatabaseClassFullName { get; set; } = "SogePoco.Database";
    public bool IsDatabaseSchemaCaseInsensitive { get; set; } = true;
    public string DatabaseClassNameSpace => 
        DatabaseClassFullName.Substring(0, DatabaseClassFullName.LastIndexOf(".", StringComparison.InvariantCulture)); 
    public string DatabaseClassSimpleName =>
        DatabaseClassFullName.Substring(DatabaseClassFullName.LastIndexOf(".", StringComparison.InvariantCulture)+1);

    public Func<string, string, bool> AreTableNamesTheSame { get; set; }
    public Func<(PocoMethod Kind, (string Schema, string Name) ForTable),bool> ShouldGenerateMethod { get; set; }
    
    public GeneratorOptions() {
        AreTableNamesTheSame = Default_AreTableNamesTheSame;
        ShouldGenerateMethod = Default_ShouldGenerateAllButFetch;
    }

    public bool Default_AreTableNamesTheSame(string fst, string snd) => 
        IsDatabaseSchemaCaseInsensitive
            ? string.Equals(fst, snd, StringComparison.OrdinalIgnoreCase)
            : string.Equals(fst, snd, StringComparison.Ordinal);

    public bool Default_ShouldGenerateAllButFetch((PocoMethod Kind, (string Schema, string Name) ForTable) input) =>
        input.Kind switch {
            PocoMethod.Select => false, //as normally we want to specify 'where' meaning selecting via query generator
            _ => true
        };

    public bool Default_ShouldGenerateAll((PocoMethod Kind, (string Schema, string Name) ForTable) input) => true;
}
