using System.Text;

namespace Zero.Editor.CodePolicy
{
    internal static class CodePolicySource
    {
        private enum State { Code, LineComment, BlockComment, String, VerbatimString, Character }

        public static string Sanitize(string source)
        {
            var result = new StringBuilder(source.Length);
            State state = State.Code;
            bool escaped = false;
            for (int i = 0; i < source.Length; i++)
            {
                char current = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';
                if (current == '\r' || current == '\n')
                {
                    result.Append(current);
                    if (state == State.LineComment) state = State.Code;
                    continue;
                }

                switch (state)
                {
                    case State.Code:
                        if (current == '/' && next == '/') { result.Append("  "); i++; state = State.LineComment; }
                        else if (current == '/' && next == '*') { result.Append("  "); i++; state = State.BlockComment; }
                        else if (current == '@' && next == '"') { result.Append("  "); i++; state = State.VerbatimString; }
                        else if (current == '$' && next == '"') { result.Append("  "); i++; state = State.String; escaped = false; }
                        else if (current == '"') { result.Append(' '); state = State.String; escaped = false; }
                        else if (current == '\'') { result.Append(' '); state = State.Character; escaped = false; }
                        else result.Append(current);
                        break;
                    case State.LineComment:
                        result.Append(' ');
                        break;
                    case State.BlockComment:
                        if (current == '*' && next == '/') { result.Append("  "); i++; state = State.Code; }
                        else result.Append(' ');
                        break;
                    case State.String:
                    case State.Character:
                        result.Append(' ');
                        if (escaped) escaped = false;
                        else if (current == '\\') escaped = true;
                        else if ((state == State.String && current == '"') ||
                                 (state == State.Character && current == '\'')) state = State.Code;
                        break;
                    case State.VerbatimString:
                        result.Append(' ');
                        if (current != '"') break;
                        if (next == '"') { result.Append(' '); i++; }
                        else state = State.Code;
                        break;
                }
            }
            return result.ToString();
        }

        public static int FindClosingBrace(string source, int openingBrace)
        {
            int depth = 0;
            for (int i = openingBrace; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}' && --depth == 0) return i;
            }
            return source.Length - 1;
        }

        public static int CountLogicalLines(string source, int start = 0, int end = -1)
        {
            if (end < 0 || end > source.Length) end = source.Length;
            int count = 0;
            int lineStart = start;
            for (int i = start; i <= end; i++)
            {
                if (i < end && source[i] != '\n') continue;
                string line = source.Substring(lineStart, i - lineStart).Trim();
                if (line.Length > 0 && line != "{" && line != "}" && !line.StartsWith("#")) count++;
                lineStart = i + 1;
            }
            return count;
        }
    }
}
