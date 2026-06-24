using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyExpression.Internal
{
    internal static class Parser
    {
        internal static bool TryParse(Expression expression)
        {
            bool result;
            try
            {
                Parse(expression, expression);
                result = ParseStatus(expression);
                if (!result)
                {
                    result = false;
                }

                RebuildExpression(expression);
            }
            catch (Exception ex)
            {
                result = false;
                expression.ErrorMessgage = ex.Message;
            }

            return result;
        }

        private static void AbsorbTrailingNotForLastOperand(Expression expression)
        {
            while (expression.Operators.Count > 0
                   && expression.Operators[expression.Operators.Count - 1] == Operator.Not
                   && expression.ExpressionChildren.Count > 0)
            {
                expression.Operators.RemoveAt(expression.Operators.Count - 1);
                var i = expression.ExpressionChildren.Count - 1;
                var operand = expression.ExpressionChildren[i];
                var operandText = !string.IsNullOrEmpty(operand.SourceExpressionString)
                    ? operand.SourceExpressionString
                    : operand.DataString ?? string.Empty;
                expression.ExpressionChildren[i] = Expression.CreateNode();
                var notExpression = expression.ExpressionChildren[i];
                notExpression.ElementType = ElementType.Expression;
                notExpression.ExpressionChildren = new List<Expression> { operand };
                notExpression.Operators = new List<Operator> { Operator.Not };
                notExpression.SourceExpressionString = "!" + operandText;
                notExpression.DataString = "!" + operandText;
                notExpression.Status = true;
                notExpression.FunctionType = FunctionType.None;
            }
        }

        private static void Parse(Expression root, Expression expression)
        {
            var lastBlock = MatchMode.None;
            for (int index = 0; index < expression.SourceExpressionString.Length; index++)
            {
                (bool Status, int EndIndex, string ChildrenExpressionString) matchScope = default;
                var currentChar = expression.SourceExpressionString[index];
                var (mode, endTag) = SetMatchMode(currentChar, lastBlock);
                switch (mode)
                {
                    case MatchMode.Scope:
                        if (currentChar == endTag)
                        {
                            matchScope = FindEnd(currentChar, expression.SourceExpressionString, index);
                            var dataExp = Expression.CreateNode();
                            dataExp.ElementType = ElementType.Data;
                            dataExp.SourceExpressionString = $"{currentChar}{matchScope.ChildrenExpressionString}{endTag}";
                            dataExp.DataString = matchScope.ChildrenExpressionString;
                            expression.ExpressionChildren.Add(dataExp);
                            AbsorbTrailingNotForLastOperand(expression);
                            lastBlock = MatchMode.Data;
                            index = matchScope.EndIndex;
                            continue;
                        }

                        matchScope = FindEnd(currentChar, endTag, expression.SourceExpressionString, index);
                        expression.Status = matchScope.Status;
                        break;
                    case MatchMode.RelationSymbol:
                        var relationSymbolStr = GetFullSymbol(expression.SourceExpressionString, index, mode);
                        var relationSymbol = ConvertOperator(relationSymbolStr.Replace(" ", ""));
                        expression.Operators.Add(relationSymbol);
                        expression.ElementType = ElementType.Expression;
                        index += relationSymbolStr.Length - 1;
                        lastBlock = mode;
                        continue;
                    case MatchMode.LogicSymbol:
                        var logicSymbolStr = GetFullSymbol(expression.SourceExpressionString, index, mode);
                        var logicSymbol = ConvertOperator(logicSymbolStr.Replace(" ", ""));
                        expression.Operators.Add(logicSymbol);
                        expression.ElementType = ElementType.Expression;
                        index += logicSymbolStr.Length - 1;
                        lastBlock = mode;
                        continue;
                    case MatchMode.ArithmeticSymbol:
                        var operatorSymbol = ConvertOperator(currentChar.ToString());
                        expression.Operators.Add(operatorSymbol);
                        expression.ElementType = ElementType.Expression;
                        lastBlock = mode;
                        continue;
                    case MatchMode.Function:
                        matchScope = FindEnd('[', endTag, expression.SourceExpressionString, index);
                        var (executeType, function) = GetFunctionType(root.SourceExpressionString, matchScope.ChildrenExpressionString);
                        var functionStr = $"[{matchScope.ChildrenExpressionString}]";
                        matchScope = FindEnd('(', ')', expression.SourceExpressionString, matchScope.EndIndex + 1);
                        functionStr += $"({matchScope.ChildrenExpressionString})";
                        var functionExp = Expression.CreateNode();
                        functionExp.ElementType = ElementType.Function;
                        functionExp.FunctionType = executeType;
                        functionExp.Function = function;
                        functionExp.FunctionName = executeType.ToString();
                        functionExp.SourceExpressionString = functionStr;
                        functionExp.DataString = matchScope.ChildrenExpressionString;
                        expression.ExpressionChildren.Add(functionExp);
                        AbsorbTrailingNotForLastOperand(expression);
                        var paramList = SplitParamObject(matchScope.ChildrenExpressionString);
                        paramList.ForEach(x =>
                        {
                            var paramExp = new Expression(x);
                            functionExp.ExpressionChildren.Add(paramExp);
                        });
                        index = matchScope.EndIndex;
                        lastBlock = mode;
                        continue;
                    case MatchMode.Data:
                        if (string.IsNullOrWhiteSpace(currentChar.ToString())) continue;
                        lastBlock = mode;
                        var (str, dataMtachMode) = GetFullData(expression.SourceExpressionString, index, lastBlock);
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            if (str.Equals(expression.SourceExpressionString))
                            {
                                expression.ElementType = ElementType.Data;
                                expression.DataString = str;
                                return;
                            }

                            var dataExp = new Expression(str);
                            if (dataMtachMode == MatchMode.Scope && currentChar == '-')
                            {
                                expression.Operators.Add(Operator.Negative);
                                continue;
                            }

                            expression.ExpressionChildren.Add(dataExp);
                            AbsorbTrailingNotForLastOperand(expression);
                        }

                        index += str.Length - 1;
                        continue;
                    case MatchMode.EscapeCharacter:
                        index++;
                        lastBlock = mode;
                        continue;
                    default:
                        break;
                }

                if (!expression.Status)
                {
                    break;
                }

                var isOver = root.ElementType == ElementType.Data || IsOver(matchScope.ChildrenExpressionString);
                if (!isOver)
                {
                    var expressionChildren = new Expression(matchScope.ChildrenExpressionString);
                    expression.ExpressionChildren.Add(expressionChildren);
                    AbsorbTrailingNotForLastOperand(expression);
                }

                index = matchScope.EndIndex;
                lastBlock = mode;
            }
        }

        private static bool ParseStatus(Expression expression)
        {
            var result = true;
            try
            {
                if (expression == null || !expression.Status)
                {
                    result = false;
                    return result;
                }

                var status = new List<bool>();
                GetExpressionStatus(expression, ref status);
                if (status.Contains(false))
                {
                    result = false;
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        private static string GetFullSymbol(string exp, int startIndex, MatchMode matchMode)
        {
            if (startIndex == exp.Length) return exp.Last() + "";
            var result = "" + exp[startIndex];
            for (int i = startIndex + 1; i < exp.Length; i++)
            {
                if (exp[i] == ' ' && i - startIndex == result.Length)
                {
                    result += exp[i];
                    continue;
                }

                var (mode, _) = SetMatchMode(exp[i], matchMode);
                if (mode == MatchMode.RelationSymbol && matchMode == MatchMode.RelationSymbol)
                {
                    result += exp[i];
                    break;
                }

                if (mode == MatchMode.LogicSymbol && exp[startIndex] == '!' && matchMode == MatchMode.LogicSymbol)
                {
                    result += exp[i];
                    break;
                }

                if (mode == MatchMode.Data) break;
                matchMode = mode;
            }

            return result;
        }

        private static (string value, MatchMode mode) GetFullData(string exp, int startIndex, MatchMode matchMode)
        {
            if (startIndex == exp.Length) return (exp.Last() + "", MatchMode.Data);
            var result = "" + exp[startIndex];
            for (int i = startIndex + 1; i < exp.Length; i++)
            {
                var (mode, _) = SetMatchMode(exp[i], matchMode);
                switch (mode)
                {
                    case MatchMode.Data:
                        result += exp[i];
                        matchMode = mode;
                        continue;
                    case MatchMode.LogicSymbol:
                        return (result, MatchMode.LogicSymbol);
                    case MatchMode.ArithmeticSymbol:
                        return (result, MatchMode.ArithmeticSymbol);
                    case MatchMode.RelationSymbol:
                        return (result, MatchMode.RelationSymbol);
                    case MatchMode.Scope:
                        var matchScope = FindEnd('(', ')', exp, i);
                        return (matchScope.ChildrenExpressionString, MatchMode.Scope);
                    case MatchMode.EscapeCharacter:
                        result += exp[i];
                        result += exp[i + 1];
                        i++;
                        matchMode = mode;
                        continue;
                    default:
                        return (result, MatchMode.Data);
                }
            }

            return (result, MatchMode.Data);
        }

        private static (FunctionType executeType, Function function) GetFunctionType(string sourceExpressionString, string key)
        {
            switch (key.ToLower())
            {
                case "sum":
                    return (FunctionType.Sum, FormulaAction.Sum);
                case "avg":
                    return (FunctionType.Avg, FormulaAction.Avg);
                case "contains":
                    return (FunctionType.Contains, FormulaAction.Contains);
                case "excluding":
                    return (FunctionType.ContainsExcept, FormulaAction.Excluding);
                case "equals":
                    return (FunctionType.Equals, FormulaAction.Equals);
                case "startwith":
                    return (FunctionType.StartWith, FormulaAction.StartWith);
                case "endwith":
                    return (FunctionType.EndWith, FormulaAction.EndWith);
                case "different":
                    return (FunctionType.Different, FormulaAction.Different);
                case "round":
                    return (FunctionType.Round, FormulaAction.Round);
                case "edate":
                    return (FunctionType.EDate, FormulaAction.EDate);
                case "eodate":
                    return (FunctionType.EODate, FormulaAction.EODate);
                case "nowtime":
                    return (FunctionType.NowTime, FormulaAction.NowTime);
                case "timetostring":
                    return (FunctionType.TimeToString, FormulaAction.TimeToString);
                case "days":
                    return (FunctionType.Days, FormulaAction.Days);
                case "hours":
                    return (FunctionType.Hours, FormulaAction.Hours);
                case "minutes":
                    return (FunctionType.Minutes, FormulaAction.Minutes);
                case "seconds":
                    return (FunctionType.Seconds, FormulaAction.Seconds);
                case "millseconds":
                    return (FunctionType.MillSeconds, FormulaAction.MillSeconds);
                case "isnull":
                    return (FunctionType.IsNull, FormulaAction.IsNull);
                default:
                    throw new ExpressionException($"at {sourceExpressionString}: {key} 函数未定义");
            }
        }

        private static bool IsOver(string expressionString)
        {
            if (string.IsNullOrEmpty(expressionString))
            {
                return true;
            }

            return !(Contains(expressionString, '(')
                || Contains(expressionString, '[')
                || Contains(expressionString, '&')
                || Contains(expressionString, '|')
                || Contains(expressionString, '!')
                || Contains(expressionString, '>')
                || Contains(expressionString, '<')
                || Contains(expressionString, '=')
                || Contains(expressionString, '+')
                || Contains(expressionString, '-')
                || Contains(expressionString, '*')
                || Contains(expressionString, '/')
                || Contains(expressionString, '%'));
        }

        private static bool Contains(string text, char contains)
        {
            var lastChar = char.MinValue;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == contains)
                {
                    if (lastChar != '\\') return true;
                }

                lastChar = text[i];
            }

            return false;
        }

        private static (bool Status, int EndIndex, string ChildrenExpressionString) FindEnd(
            char startTag,
            char? endTag,
            string expression,
            int index)
        {
            var result = (Status: true, EndIndex: 0, ChildrenExpressionString: "");
            try
            {
                int currentLevel = 0;
                int? endIndex = null;
                for (; index < expression.Length; index++)
                {
                    var currentChar = expression[index];
                    if (currentChar == '\\')
                    {
                        result.ChildrenExpressionString += expression[index++];
                        result.ChildrenExpressionString += expression[index];
                        continue;
                    }

                    if (currentChar == startTag)
                    {
                        currentLevel++;
                        if (currentLevel == 1)
                        {
                            continue;
                        }
                    }
                    else if (currentChar == endTag)
                    {
                        currentLevel--;
                    }

                    if (currentLevel == 0 && currentChar == endTag)
                    {
                        endIndex = index;
                        break;
                    }

                    result.ChildrenExpressionString += currentChar;
                }

                if (endIndex.HasValue)
                {
                    result.EndIndex = endIndex.Value;
                }
                else
                {
                    result.Status = false;
                }
            }
            catch
            {
                result.Status = false;
            }

            return result;
        }

        private static (bool Status, int EndIndex, string ChildrenExpressionString) FindEnd(char tag, string expression, int index)
        {
            var result = (Status: true, EndIndex: 0, ChildrenExpressionString: "");
            try
            {
                for (var i = index + 1; i < expression.Length; i++)
                {
                    if (expression[i] == tag)
                    {
                        result.EndIndex = i;
                        result.Status = true;
                        break;
                    }

                    result.ChildrenExpressionString += expression[i];
                }
            }
            catch
            {
                result.Status = false;
            }

            return result;
        }

        private static List<string> SplitParamObject(string srcString)
        {
            var result = new List<string>();
            var paramString = string.Empty;
            var areaLevel = 0;
            for (int i = 0; i < srcString.Length; i++)
            {
                var currentChar = srcString[i];
                switch (currentChar)
                {
                    case ',':
                        if (!string.IsNullOrEmpty(paramString) && areaLevel == 0)
                        {
                            result.Add(paramString);
                            paramString = string.Empty;
                            continue;
                        }

                        break;
                    case '(':
                    case '[':
                        areaLevel++;
                        break;
                    case ')':
                    case ']':
                        areaLevel--;
                        break;
                }

                paramString += currentChar;
            }

            if (!string.IsNullOrEmpty(paramString))
            {
                result.Add(paramString);
            }

            return result;
        }

        private static (MatchMode Mode, char? EndTag) SetMatchMode(char currentChar, MatchMode lastMode)
        {
            switch (currentChar)
            {
                case '(':
                    return (MatchMode.Scope, ')');
                case '"':
                    return (MatchMode.Scope, '"');
                case '\'':
                    return (MatchMode.Scope, '\'');
                case '[':
                    return (MatchMode.Function, ']');
                case '&':
                    return (MatchMode.LogicSymbol, null);
                case '|':
                    return (MatchMode.LogicSymbol, null);
                case '!':
                    return (MatchMode.LogicSymbol, null);
                case '+':
                    return (MatchMode.ArithmeticSymbol, null);
                case '-':
                    if (lastMode == MatchMode.None || lastMode == MatchMode.ArithmeticSymbol
                        || lastMode == MatchMode.LogicSymbol || lastMode == MatchMode.RelationSymbol)
                    {
                        return (MatchMode.Data, null);
                    }

                    return (MatchMode.ArithmeticSymbol, null);
                case '*':
                    return (MatchMode.ArithmeticSymbol, null);
                case '/':
                    return (MatchMode.ArithmeticSymbol, null);
                case '%':
                    return (MatchMode.ArithmeticSymbol, null);
                case '<':
                    return (MatchMode.RelationSymbol, null);
                case '>':
                    return (MatchMode.RelationSymbol, null);
                case '=':
                    if (lastMode == MatchMode.LogicSymbol)
                    {
                        return (MatchMode.LogicSymbol, null);
                    }

                    return (MatchMode.RelationSymbol, null);
                case '\\':
                    return (MatchMode.EscapeCharacter, null);
                default:
                    return (MatchMode.Data, null);
            }
        }

        private static Operator ConvertOperator(string currentChar)
        {
            switch (currentChar)
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
                case "=<":
                    return Operator.LessThanOrEquals;
                case ">=":
                case "=>":
                    return Operator.GreaterThanOrEquals;
                default:
                    return Operator.None;
            }
        }

        private static void GetExpressionStatus(Expression expression, ref List<bool> status)
        {
            if (!status.Contains(expression.Status))
            {
                status.Add(expression.Status);
            }

            if (!expression.Status)
            {
                return;
            }

            if (expression.ExpressionChildren.Any())
            {
                foreach (var item in expression.ExpressionChildren)
                {
                    if (!status.Contains(item.Status))
                    {
                        status.Add(item.Status);
                    }

                    if (item.ExpressionChildren.Any())
                    {
                        GetExpressionStatus(item, ref status);
                    }
                }
            }
        }

        private static void RebuildExpression(Expression expression)
        {
            if (!expression.Operators.Any()) return;
            while (true)
            {
                var count = expression.Operators.Select(x => OperatorInfoCache.GetLevel(x)).Distinct();
                if (count.Count() == 1)
                {
                    return;
                }

                var level = count.Max();
                var operators = GetTargetLevelOperators(expression, expression.Operators, level);
                foreach (var list in operators)
                {
                    var startIndex = list.Last();
                    var endIndex = list.First();
                    var children = expression.ExpressionChildren.Skip(startIndex).Take(endIndex - startIndex + 2).ToList();
                    var childrenOperators = new List<Operator>();
                    list.ForEach(x => childrenOperators.Add(expression.Operators[x]));
                    var newExp = BuildChildren(children, childrenOperators);
                    expression.ExpressionChildren.Insert(startIndex, newExp);
                    children.ForEach(x => expression.ExpressionChildren.Remove(x));
                    list.ForEach(expression.Operators.RemoveAt);
                }
            }
        }

        private static Expression BuildChildren(List<Expression> expressions, List<Operator> operators)
        {
            var dataString = expressions[0].DataString;
            if (expressions[0].ElementType == ElementType.Function)
            {
                dataString = $"{expressions[0].SourceExpressionString}";
            }

            for (int i = 1; i < expressions.Count; i++)
            {
                var childStr = expressions[i].DataString;
                if (expressions[i].ElementType == ElementType.Expression)
                {
                    childStr = $"{expressions[i].SourceExpressionString}";
                }

                dataString += $"{OperatorInfoCache.GetValue(operators[i - 1])}{childStr}";
            }

            var exp = Expression.CreateNode();
            exp.ExpressionChildren = expressions;
            exp.Operators = operators;
            exp.DataString = dataString;
            exp.ElementType = ElementType.Expression;
            exp.SourceExpressionString = dataString;
            exp.Status = true;
            exp.FunctionType = FunctionType.None;
            return exp;
        }

        private static List<List<int>> GetTargetLevelOperators(
            Expression expression,
            List<Operator> oldOperators,
            int level)
        {
            var result = new List<List<int>>();
            var operators = new List<int>();
            for (int i = oldOperators.Count - 1; i >= 0; i--)
            {
                if (OperatorInfoCache.GetLevel(expression.Operators[i]) == level)
                {
                    operators.Add(i);
                }
                else
                {
                    if (operators.Any())
                    {
                        result.Add(new List<int>(operators));
                        operators.Clear();
                    }
                }
            }

            if (operators.Count != 0)
            {
                result.Add(operators);
            }

            return result;
        }
    }
}
