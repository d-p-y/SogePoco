using System.Text.RegularExpressions;

namespace SogePoco.Impl.Extensions; 

public static class StringExtensions {
    private static Regex ReUnwanted = new Regex(@"[\p{P}\p{S}]+");
    private static Regex ReSeparators = new Regex(@"[\s_\-]+");

    public static string StringAsCsCodeStringValue(this string val) => "@\"" + val.Replace("\"", "\"\"") + "\"";

    //TODO investigate Humanizer.Core as implementation
        
    public static string? ToUpperCamelCaseOrNull(this string self) =>
        ReSeparators
                .Split(self)
                .Select(x => ReUnwanted.Replace(x, ""))
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .ToList()
            switch {
                var x when x.Count < 1 => null,
                var x =>
                    //input looks like snake_case
                    x.Select(y => y switch {
                            var z when z.Length <= 1 => z.ToUpper(),
                            var z when Char.IsUpper(z[0]) => z, //looks formatted already
                            var z => Char.ToUpper(z[0]) + z.Substring(1).ToLower() })
                        .Concatenate()
            };
        
    public static string? ToLowerCamelCaseOrNull(this string self) =>
        ReSeparators
                .Split(self)
                .Select(x => ReUnwanted.Replace(x, ""))
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .ToList()
            switch {
                var x when x.Count < 1 => null,
                var x =>
                    //input looks like snake_case
                    x.SelectI((i,y) => 
                            i == 0
                                ? y switch {
                                    var z when z.Length <= 1 => z.ToLower(),
                                    var z when Char.IsLower(z[0]) => z, //looks formatted already
                                    var z => Char.ToLower(z[0]) + z.Substring(1).ToLower() }
                                : y switch {
                                    var z when z.Length <= 1 => z.ToUpper(),
                                    var z when Char.IsUpper(z[0]) => z, //looks formatted already
                                    var z => Char.ToUpper(z[0]) + z.Substring(1).ToLower() } )
                        .Concatenate()
            };
}