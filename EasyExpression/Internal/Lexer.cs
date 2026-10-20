using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EasyExpression.Internal
{
    internal enum TokenKind
    {
        End = 0,
        Number = 1,
        String = 2,
        Identifier = 3,
        Operator = 4,
        LeftParen = 5,
        RightParen = 6,
        LeftBracket = 7,
        RightBracket = 8,
        Comma = 9
    }

    internal struct Token
    {
        public TokenKind Kind;
        public string Text;
        public string Raw;
        public int Position;
        public bool Quoted;
    }

    /// <summary>
    /// 把表达式字符串切分为 Token。字符串字面量内的逗号、运算符不会再被误判。
    /// </summary>
    internal static class Lexer
    {
        internal static List<Token> Tokenize(string source, CultureInfo culture)
        {
            var tokens = new List<Token>();
            var index = 0;
            while (index < source.Length)
            {
                var current = source[index];
                if (char.IsWhiteSpace(current))
                {
                    index++;
                    continue;
                }

                switch (current)
                {
                    case '(':
                        tokens.Add(Create(TokenKind.LeftParen, "(", index));
                        index++;
                        continue;
                    case ')':
                        tokens.Add(Create(TokenKind.RightParen, ")", index));
                        index++;
                        continue;
                    case '[':
                        tokens.Add(Create(TokenKind.LeftBracket, "[", index));
                        index++;
                        continue;
                    case ']':
                        tokens.Add(Create(TokenKind.RightBracket, "]", index));
                        index++;
                        continue;
                    case ',':
                        tokens.Add(Create(TokenKind.Comma, ",", index));
                        index++;
                        continue;
                    case '\'':
                    case '"':
                        tokens.Add(ReadString(source, ref index, current));
                        continue;
                }

                if (IsOperatorChar(current))
                {
                    // 允许运算符两字符之间出现空白，例如 "< = 5"。
                    var next = index + 1;
                    while (next < source.Length && char.IsWhiteSpace(source[next]))
                    {
                        next++;
                    }

                    if (next < source.Length && IsOperatorChar(source[next]))
                    {
                        var normalized = NormalizeOperatorPair(current, source[next]);
                        if (normalized != null)
                        {
                            var pairToken = Create(TokenKind.Operator, normalized, index);
                            pairToken.Raw = source.Substring(index, next + 1 - index);
                            tokens.Add(pairToken);
                            index = next + 1;
                            continue;
                        }
                    }

                    tokens.Add(Create(TokenKind.Operator, current.ToString(), index));
                    index++;
                    continue;
                }

                var start = index;
                while (index < source.Length && !IsDelimiter(source[index]))
                {
                    index++;
                }

                if (index == start)
                {
                    throw new ExpressionException($"位置 {start}: 无法识别的字符 '{source[start]}'");
                }

                var word = source.Substring(start, index - start);
                var kind = IsNumber(word, culture) ? TokenKind.Number : TokenKind.Identifier;
                var wordToken = Create(kind, word, start);
                wordToken.Raw = word;
                tokens.Add(wordToken);
            }

            tokens.Add(Create(TokenKind.End, string.Empty, source.Length));
            return tokens;
        }

        private static Token ReadString(string source, ref int index, char quote)
        {
            var start = index;
            var builder = new StringBuilder();
            index++;
            while (index < source.Length)
            {
                var current = source[index];
                if (current == '\\')
                {
                    index++;
                    if (index >= source.Length)
                    {
                        throw new ExpressionException($"位置 {start}: 字符串未闭合");
                    }

                    // 仅转义引号与反斜杠，其余情况保留反斜杠（如 Windows 路径）。
                    var escaped = source[index];
                    if (escaped == quote || escaped == '\\')
                    {
                        builder.Append(escaped);
                    }
                    else
                    {
                        builder.Append('\\');
                        builder.Append(escaped);
                    }

                    index++;
                    continue;
                }

                if (current == quote)
                {
                    index++;
                    var token = Create(TokenKind.String, builder.ToString(), start);
                    token.Raw = source.Substring(start, index - start);
                    token.Quoted = true;
                    return token;
                }

                builder.Append(current);
                index++;
            }

            throw new ExpressionException($"位置 {start}: 字符串未闭合");
        }

        private static Token Create(TokenKind kind, string text, int position)
        {
            return new Token
            {
                Kind = kind,
                Text = text,
                Raw = text,
                Position = position,
                Quoted = false
            };
        }

        private static string NormalizeOperatorPair(char first, char second)
        {
            switch (first)
            {
                case '>':
                    return second == '=' ? ">=" : null;
                case '<':
                    return second == '=' ? "<=" : null;
                case '!':
                    return second == '=' ? "!=" : null;
                case '=':
                    return second == '=' ? "=" : second == '>' ? ">=" : second == '<' ? "<=" : null;
                case '&':
                    return second == '&' ? "&" : null;
                case '|':
                    return second == '|' ? "|" : null;
                default:
                    return null;
            }
        }

        private static bool IsNumber(string word, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            var hasDigit = false;
            for (var i = 0; i < word.Length; i++)
            {
                if (char.IsDigit(word[i]))
                {
                    hasDigit = true;
                    break;
                }
            }

            return hasDigit && double.TryParse(word, NumberStyles.Any, culture, out _);
        }

        private static bool IsDelimiter(char value)
        {
            return char.IsWhiteSpace(value)
                   || IsOperatorChar(value)
                   || value == '(' || value == ')' || value == '[' || value == ']'
                   || value == ',' || value == '\'' || value == '"';
        }

        private static bool IsOperatorChar(char value)
        {
            switch (value)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '<':
                case '>':
                case '=':
                case '!':
                case '&':
                case '|':
                    return true;
                default:
                    return false;
            }
        }
    }
}