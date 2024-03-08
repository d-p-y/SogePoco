using SogePoco.Impl.Extensions;
using Xunit;

namespace SogePoco.Impl.Tests; 

public class TestsStringExtensions {

    [Theory]
    [InlineData("Foo", "Foo")]
    [InlineData("abc", "Abc")]
    [InlineData("some_name", "SomeName")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("XYZ", "XYZ")]
    [InlineData("abc def", "AbcDef")]
    [InlineData("abc\tdef", "AbcDef")]
    [InlineData("abc-def", "AbcDef")]
    [InlineData(@"abcéÈèÊêëąęбшÜß!@$#$%^&*()+=*/|'"":;<>,.?[]{}`⟩~def", "Abcéèèêêëąęбшüßdef")] //preserves unicode yet strips symbols
    public void VerifyToUpperCamelCase(string input, string expectedOutput) => 
        Assert.Equal(expectedOutput, input.ToUpperCamelCaseOrNull());

    [Theory]
    [InlineData(" ")]
    [InlineData("\t\\")]
    [InlineData("_")]
    [InlineData("-")]
    public void VerifyToUpperCamelCaseFailsOnNonConvertable(string input) =>
        Assert.Null(input.ToUpperCamelCaseOrNull());
        
    [Theory]
    [InlineData("Foo", "foo")]
    [InlineData("abc", "abc")]
    [InlineData("some_name", "someName")]
    [InlineData("a", "a")]
    [InlineData("A", "a")]
    [InlineData("XYZ", "xyz")]
    [InlineData("abc def", "abcDef")]
    [InlineData("abc\tdef", "abcDef")]
    [InlineData("abc-def", "abcDef")]
    [InlineData(@"abcéÈèÊêëąęбшÜß!@$#$%^&*()+=*/|'"":;<>,.?[]{}`⟩~def", "abcéÈèÊêëąęбшÜßdef")] //preserves unicode yet strips symbols
    public void ToLowerCamelCaseOrNull(string input, string expectedOutput) => 
        Assert.Equal(expectedOutput, input.ToLowerCamelCaseOrNull());

    [Theory]
    [InlineData(" ")]
    [InlineData("\t\\")]
    [InlineData("_")]
    [InlineData("-")]
    public void VerifyToLowerCamelCaseFailsOnNonConvertable(string input) =>
        Assert.Null(input.ToLowerCamelCaseOrNull());

    [Theory]
    [InlineData("", "@\"\"")]
    [InlineData("a", "@\"a\"")]
    [InlineData("\\a\\", @"@""\a\""")]
    [InlineData("\"", "@\"\"\"\"")]
    public void Test_StringAsCsCodeStringValue(string input, string expected) =>
        Assert.Equal(expected, input.StringAsCsCodeStringValue());
}