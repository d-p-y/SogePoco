namespace System.Runtime.CompilerServices;

//I cannot use dotnet 5+ as source generators require netstandard2.0
//workaround
//https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
internal static class IsExternalInit {}
