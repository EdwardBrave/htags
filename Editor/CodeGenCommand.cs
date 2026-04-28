using System;
using System.Text.RegularExpressions;

namespace HTags.Editor
{
    internal struct CodeGenCommand
    {
        public string Name;
        public string[] Args;
        public string Body;
        private string _insertionLine;

        public bool IsValid => !string.IsNullOrEmpty(Name);

        public string ReplaceWith(string line, string replacement)
        {
            int insertionLineStartIndex = line.IndexOf(_insertionLine, StringComparison.Ordinal);
            int insertionLineEndIndex = insertionLineStartIndex + _insertionLine.Length;
            
            return line.Substring(0, insertionLineStartIndex) + replacement + line.Substring(insertionLineEndIndex);
        }
        
        public static CodeGenCommand TryRead(Capture match)
        {
            string line = match.Value.Substring(1, match.Value.Length - 2);
            
            if (Regex.IsMatch(line, @"^[a-zA-Z0-9]+$"))
            {
                return new CodeGenCommand
                {
                    Name = line,
                    Args = Array.Empty<string>(),
                    Body = string.Empty,
                    _insertionLine = match.Value
                };
            }
            
            if (Regex.IsMatch(line, @"^[a-zA-Z0-9]+?\(.+?\)$"))
            {
                return new CodeGenCommand
                {
                    Name = line,
                    Args = line.Substring(line.IndexOf('(') + 1, line.IndexOf(')') - line.IndexOf('(') - 1).Split('$'),
                    Body = string.Empty,
                    _insertionLine = match.Value
                };
            }
            
            if (Regex.IsMatch(line, @"^[a-zA-Z0-9]+?:.+$"))
            {
                return new CodeGenCommand
                {
                    Name = line.Substring(0, line.IndexOf(':')),
                    Args = Array.Empty<string>(),
                    Body = line.Substring(line.IndexOf(':') + 1),
                    _insertionLine = match.Value
                };
            }

            if (Regex.IsMatch(line, @"^[a-zA-Z0-9]+?\(.+?\):.+$"))
            {
                return new CodeGenCommand
                {
                    Name = line.Substring(0, line.IndexOf('(')),
                    Args = line.Substring(line.IndexOf('(') + 1, line.IndexOf(')') - line.IndexOf('(') - 1).Split('$'),
                    Body = line.Substring(line.IndexOf(':') + 1),
                    _insertionLine = match.Value
                };
            }

            return default;
        }
    }
}