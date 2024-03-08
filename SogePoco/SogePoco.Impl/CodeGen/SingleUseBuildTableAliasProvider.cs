namespace SogePoco.Impl.CodeGen; 

public class SingleUseBuildTableAliasProvider {
    private int _counter = 0;
        
    public string Build() => $"t{_counter++}";
}