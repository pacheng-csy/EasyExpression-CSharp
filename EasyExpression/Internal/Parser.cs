using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EasyExpression.Internal
{
    internal static class Parser
    {
        internal static bool TryParse(Expression expression)
        {
            try
            {
                if (expression == null || string.IsNullOrEmpty(expression.SourceExpressionString))
                {
                    return false;
                }

                var tokens = Lexer.Tokenize(expression.SourceExpressionString, expression.Options.Culture);
                var parser = new AstParser(tokens, expression.SourceExpressionString, expression.Options);
                var root = parser.ParseFull();
                expression.CopyFrom(root);
                expression.Status = true;
                expression.ErrorMessgage = null;
                expression.Check();
                return true;
            }
            catch (Exception ex)
            {
                expression.Status = false;
                expression.ErrorMessgage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 递归下降解析器，优先级与 <see cref="OperatorAttribute"/> 中的 Level 保持一致：
        /// 逻辑(1) &lt; 关系(3) &lt; 加减(4) &lt; 乘除模(5) &lt; 一元(6)。
        /// </summary>
        private sealed class AstParser
        {
            private const int MaxDepth = 256;

            private readonly List<Token> _tokens;
            private readonly string _source;
            private readonly ExpressionOptions _options;
            private int _index;
            private int _depth;

            internal AstParser(List<Token> tokens, string source, ExpressionOptions options)
            {
                _tokens = tokens;
                _source = source;
                _options = options;
            }

            internal Expression ParseFull()
            {
                if (Current.Kind == TokenKind.End)
                {
                    throw new ExpressionException("表达式不能为空");
                }

                var root = ParseExpression();
                if (Current.Kind != TokenKind.End)
                {
                    throw new ExpressionException($"位置 {Current.Position}: 无法识别的内容 '{Current.Raw}'");
                }

                root.SourceExpressionString = _source;
                if (root.ElementType != ElementType.Data && root.ElementType != ElementType.Reference)
                {
                    root.DataString = _source;
                }

                return root;
            }

            private Expression ParseExpression()
            {
                EnterDepth();
                try
                {
                    return ParseLogical();
                }
                finally
                {
                    _depth--;
                }
            }

            private Expression ParseLogical()
            {
                var left = ParseComparison();
                while (IsOperator("&") || IsOperator("|"))
                {
                    var token = Next();
                    var right = ParseComparison();
                    left = BuildBinary(ToOperator(token.Text), left, right);
                }

                return left;
            }

            private Expression ParseComparison()
            {
                var left = ParseAdditive();
                while (IsOperator("=") || IsOperator("!=") || IsOperator(">") || IsOperator("<")
                       || IsOperator(">=") || IsOperator("<="))
                {
                    var token = Next();
                    var right = ParseAdditive();
                    left = BuildBinary(ToOperator(token.Text), left, right);
                }

                return left;
            }

            private Expression ParseAdditive()
            {
                var left = ParseMultiplicative();
                while (IsOperator("+") || IsOperator("-"))
                {
                    var token = Next();
                    var right = ParseMultiplicative();
                    left = BuildBinary(ToOperator(token.Text), left, right);
                }

                return left;
            }

            private Expression ParseMultiplicative()
            {
                var left = ParseUnary();
                while (IsOperator("*") || IsOperator("/") || IsOperator("%"))
                {
                    var token = Next();
                    var right = ParseUnary();
                    left = BuildBinary(ToOperator(token.Text), left, right);
                }

                return left;
            }

            private Expression ParseUnary()
            {
                EnterDepth();
                try
                {
                    if (IsOperator("!") || IsOperator("-"))
                    {
                        var token = Next();
                        var operand = ParseUnary();
                        var op = token.Text == "!" ? Operator.Not : Operator.Negative;
                        return BuildUnary(op, operand);
                    }

                    return ParsePrimary();
                }
                finally
                {
                    _depth--;
                }
            }

            private Expression ParsePrimary()
            {
                var token = Current;
                switch (token.Kind)
                {
                    case TokenKind.Number:
                        Next();
                        return BuildData(token, true);
                    case TokenKind.String:
                        Next();
                        return BuildData(token, true);
                    case TokenKind.Identifier:
                        Next();
                        return BuildData(token, false);
                    case TokenKind.LeftParen:
                        Next();
                        var inner = ParseExpression();
                        Expect(TokenKind.RightParen, "缺少 ')'");
                        return inner;
                    case TokenKind.LeftBracket:
                        return ParseFunction();
                    case TokenKind.End:
                        throw new ExpressionException("表达式不完整");
                    default:
                        throw new ExpressionException($"位置 {token.Position}: 无法识别的符号 '{token.Raw}'");
                }
            }

            private Expression ParseFunction()
            {
                Expect(TokenKind.LeftBracket, "缺少 '['");
                var nameToken = Expect(TokenKind.Identifier, "'[' 后缺少函数名");
                Expect(TokenKind.RightBracket, "函数名缺少 ']'");
                Expect(TokenKind.LeftParen, $"函数 {nameToken.Text} 缺少 '('");

                var args = new List<Expression>();
                if (Current.Kind != TokenKind.RightParen)
                {
                    args.Add(ParseExpression());
                    while (Current.Kind == TokenKind.Comma)
                    {
                        Next();
                        args.Add(ParseExpression());
                    }
                }

                Expect(TokenKind.RightParen, $"函数 {nameToken.Text} 缺少 ')'");

                var definition = FunctionRegistry.Get(nameToken.Text);
                if (_options != null && !_options.IsFunctionAllowed(nameToken.Text))
                {
                    throw new ExpressionException($"位置 {nameToken.Position}: 函数 {nameToken.Text} 不在允许列表中");
                }

                var function = Expression.CreateNode(_options);
                function.ElementType = ElementType.Function;
                function.FunctionType = definition.Type;
                function.Function = definition.Function;
                function.FunctionName = definition.Type == FunctionType.Customer
                    ? definition.Name
                    : definition.Type.ToString();
                function.ExpressionChildren = args;
                function.DataString = string.Join(",", args.Select(x => x.SourceExpressionString ?? string.Empty));
                function.SourceExpressionString = $"[{nameToken.Text}]({function.DataString})";
                function.Status = true;
                return function;
            }

            private Expression BuildData(Token token, bool isLiteral)
            {
                var data = Expression.CreateNode(_options);
                data.ElementType = ElementType.Data;
                data.DataString = token.Text;
                data.SourceExpressionString = token.Quoted ? token.Raw : token.Text;
                data.RealityString = null;
                data.IsLiteral = isLiteral;
                data.IsStringLiteral = token.Quoted;
                data.Status = true;
                return data;
            }

            private Expression BuildBinary(Operator op, Expression left, Expression right)
            {
                var node = Expression.CreateNode(_options);
                node.ElementType = ElementType.Expression;
                node.Operators = new List<Operator> { op };
                node.ExpressionChildren = new List<Expression> { left, right };
                node.SourceExpressionString = $"{left.SourceExpressionString}{OperatorInfoCache.GetValue(op)}{right.SourceExpressionString}";
                node.DataString = node.SourceExpressionString;
                node.Status = true;
                return node;
            }

            private Expression BuildUnary(Operator op, Expression operand)
            {
                var node = Expression.CreateNode(_options);
                node.ElementType = ElementType.Expression;
                node.Operators = new List<Operator> { op };
                node.ExpressionChildren = new List<Expression> { operand };
                node.SourceExpressionString = $"{OperatorInfoCache.GetValue(op)}{operand.SourceExpressionString}";
                node.DataString = node.SourceExpressionString;
                node.Status = true;
                return node;
            }

            private Token Current
            {
                get { return _tokens[_index]; }
            }

            private Token Next()
            {
                var token = _tokens[_index];
                if (_index < _tokens.Count - 1)
                {
                    _index++;
                }

                return token;
            }

            private Token Expect(TokenKind kind, string message)
            {
                if (Current.Kind != kind)
                {
                    throw new ExpressionException($"位置 {Current.Position}: {message}");
                }

                return Next();
            }

            private bool IsOperator(string text)
            {
                return Current.Kind == TokenKind.Operator && Current.Text == text;
            }

            private void EnterDepth()
            {
                _depth++;
                if (_depth > MaxDepth)
                {
                    throw new ExpressionException("表达式嵌套层级过深");
                }
            }

            private static Operator ToOperator(string text)
            {
                switch (text)
                {
                    case "&":
                        return Operator.And;
                    case "|":
                        return Operator.Or;
                    case "!":
                        return Operator.Not;
                    case "+":
                        return Operator.Plus;
                    case "-":
                        return Operator.Subtract;
                    case "*":
                        return Operator.Multiply;
                    case "/":
                        return Operator.Divide;
                    case "%":
                        return Operator.Mod;
                    case ">":
                        return Operator.GreaterThan;
                    case "<":
                        return Operator.LessThan;
                    case "=":
                        return Operator.Equals;
                    case "!=":
                        return Operator.UnEquals;
                    case "<=":
                        return Operator.LessThanOrEquals;
                    case ">=":
                        return Operator.GreaterThanOrEquals;
                    default:
                        throw new ExpressionException($"未知运算符 '{text}'");
                }
            }
        }
    }
}