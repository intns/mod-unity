using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public struct Token
{
    public string Type { get; set; }
    public string Value { get; set; }
}

public class ScriptTokenizer
{
    private static readonly Regex NumberPattern = new(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);
    private static readonly Regex WordPattern = new(@"\b[a-zA-Z_]\w*\b", RegexOptions.Compiled);
    private static readonly Regex CommentPattern = new(@"//.*$", RegexOptions.Multiline);
    private static readonly Regex BlockStartPattern = new(@"\{");
    private static readonly Regex BlockEndPattern = new(@"\}");
    private static readonly Regex StringPattern = new(@"""[^""]*""");

    public IEnumerable<Token> Tokenize(string script)
    {
        // Remove comments
        script = CommentPattern.Replace(script, "");

        List<Token> tokens = new List<Token>();
        string[] lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.Length == 0 || trimmedLine.StartsWith("#"))
                continue; // Skip empty lines and preprocessor directives

            Match match = NumberPattern.Match(trimmedLine);
            if (match.Success)
            {
                tokens.Add(new Token { Type = "Number", Value = match.Value });
                continue;
            }

            match = WordPattern.Match(trimmedLine);
            if (match.Success)
            {
                tokens.Add(new Token { Type = "Word", Value = match.Value });
                continue;
            }

            match = BlockStartPattern.Match(trimmedLine);
            if (match.Success)
            {
                tokens.Add(new Token { Type = "BlockStart", Value = match.Value });
                continue;
            }

            match = BlockEndPattern.Match(trimmedLine);
            if (match.Success)
            {
                tokens.Add(new Token { Type = "BlockEnd", Value = match.Value });
                continue;
            }

            match = StringPattern.Match(trimmedLine);
            if (match.Success)
            {
                tokens.Add(new Token { Type = "String", Value = match.Value });
                continue;
            }
        }

        return tokens;
    }
}
