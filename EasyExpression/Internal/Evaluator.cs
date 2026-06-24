using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyExpression.Internal
{
    internal static class Evaluator
    {
        internal static object Execute(Expression expression)
        {
            var result = ExecuteChildren(expression);
            return result.First();
        }

        private static List<object> ExecuteChildren(Expression expression)
        {
            var childrenResults = new List<object>();
            if (expression.ExpressionChildren.Count == 0)
            {
                childrenResults.Add(ExecuteNode(expression, expression));
                return childrenResults;
            }

            foreach (var childExp in expression.ExpressionChildren)
            {
                childrenResults.Add(ExecuteNode(childExp, expression));
            }

            if (expression.Operators.Count == 0)
            {
                return childrenResults;
            }

            var result = childrenResults.First();
            for (int i = 0; i < expression.Operators.Count; i++)
            {
                var value = expression.Operators[i] == Operator.Not || expression.Operators[i] == Operator.Negative
                    ? childrenResults[i]
                    : childrenResults[i + 1];

                switch (expression.Operators[i])
                {
                    case Operator.None:
                        break;
                    case Operator.And:
                        result = (double)result != 0d && (double)value != 0d ? 1d : 0d;
                        break;
                    case Operator.Or:
                        result = (double)result != 0d || (double)value != 0d ? 1d : 0d;
                        break;
                    case Operator.Not:
                        result = (double)value != 0d ? 0d : 1d;
                        break;
                    case Operator.Plus:
                        result = (double)result + (double)value;
                        break;
                    case Operator.Subtract:
                        if ((result is DateTime) && (value is DateTime))
                        {
                            result = (DateTime)result - (DateTime)value;
                        }
                        else
                        {
                            result = (double)result - (double)value;
                        }

                        break;
                    case Operator.Multiply:
                        result = (double)result * (double)value;
                        break;
                    case Operator.Divide:
                        result = (double)result / (double)value;
                        break;
                    case Operator.Mod:
                        result = (double)result % (double)value;
                        break;
                    case Operator.GreaterThan:
                        if (!(result is DateTime) && !(value is DateTime))
                        {
                            result = (double)result > (double)value ? 1d : 0d;
                        }
                        else
                        {
                            result = (DateTime)result > (DateTime)value ? 1d : 0d;
                        }

                        break;
                    case Operator.LessThan:
                        if (!(result is DateTime) && !(value is DateTime))
                        {
                            result = (double)result < (double)value ? 1d : 0d;
                        }
                        else
                        {
                            result = (DateTime)result < (DateTime)value ? 1d : 0d;
                        }

                        break;
                    case Operator.Equals:
                        if (!(result is DateTime) && !(value is DateTime))
                        {
                            if (value is double a && result is double b)
                            {
                                result = a == b ? 1d : 0d;
                            }
                            else
                            {
                                result = result == value ? 1d : 0d;
                            }
                        }
                        else
                        {
                            result = (DateTime)result == (DateTime)value ? 1d : 0d;
                        }

                        break;
                    case Operator.UnEquals:
                        if (!(result is DateTime) && !(value is DateTime))
                        {
                            if (value is double a && result is double b)
                            {
                                result = a != b ? 1d : 0d;
                            }
                            else
                            {
                                result = result != value ? 1d : 0d;
                            }
                        }
                        else
                        {
                            result = (DateTime)result == (DateTime)value ? 0d : 1d;
                        }

                        break;
                    case Operator.GreaterThanOrEquals:
                        if (!(result is DateTime) && !(value is DateTime))
                        {
                            result = (double)result >= (double)value ? 1d : 0d;
                        }
                        else
                        {
                            result = (DateTime)result >= (DateTime)value ? 1d : 0d;
                        }

                        break;
                    case Operator.LessThanOrEquals:
                        if (!(result is DateTime) && !(value is DateTime))
                        {
                            result = (double)result <= (double)value ? 1d : 0d;
                        }
                        else
                        {
                            result = (DateTime)result <= (DateTime)value ? 1d : 0d;
                        }

                        break;
                    case Operator.Negative:
                        result = (double)value * -1;
                        break;
                    default:
                        break;
                }
            }

            childrenResults.Clear();
            childrenResults.Add(result);
            return childrenResults;
        }

        private static object ExecuteNode(Expression childExp, Expression context)
        {
            switch (childExp.ElementType)
            {
                case ElementType.Expression:
                    return Execute(childExp);
                case ElementType.Data:
                    return Convert2ObjectValue(childExp.RealityString, context.SourceExpressionString);
                case ElementType.Function:
                    if (childExp.Function == null)
                    {
                        throw new ExpressionException($"at {context.SourceExpressionString}: 不存在函数实例{childExp.FunctionType}");
                    }

                    object v = 0d;
                    switch (childExp.FunctionType)
                    {
                        case FunctionType.None:
                            v = childExp.Function.Invoke();
                            break;
                        case FunctionType.Sum:
                        case FunctionType.Avg:
                        case FunctionType.Customer:
                        case FunctionType.EDate:
                        case FunctionType.EODate:
                        case FunctionType.NowTime:
                        case FunctionType.TimeToString:
                        case FunctionType.Round:
                        case FunctionType.Contains:
                        case FunctionType.ContainsExcept:
                        case FunctionType.Equals:
                        case FunctionType.StartWith:
                        case FunctionType.EndWith:
                        case FunctionType.Different:
                        case FunctionType.Days:
                        case FunctionType.Hours:
                        case FunctionType.Minutes:
                        case FunctionType.Seconds:
                        case FunctionType.MillSeconds:
                            v = BuildParams(childExp, v, context.SourceExpressionString, allowNullParam: false);
                            break;
                        case FunctionType.IsNull:
                            v = BuildParams(childExp, v, context.SourceExpressionString, allowNullParam: true);
                            break;
                    }

                    return v;
                default:
                    throw new ExpressionException($"at {context.SourceExpressionString}: 未知表达式节点");
            }
        }

        private static object BuildParams(
            Expression childExp,
            object v,
            string sourceExpressionString,
            bool allowNullParam)
        {
            var paramsList = new List<object>();
            if (childExp.ExpressionChildren.Count(x => x.ElementType != ElementType.Data) != 0)
            {
                foreach (var child in childExp.ExpressionChildren)
                {
                    ExecuteChildren(child).ForEach(x => paramsList.Add(x));
                }
            }
            else
            {
                if (allowNullParam && childExp.RealityString == null)
                {
                    paramsList.Add(null);
                }
                else
                {
                    paramsList = childExp.RealityString?.Split(',').Select(x => (object)x).ToList();
                }
            }

            try
            {
                v = childExp.Function.Invoke(paramsList?.ToArray());
                return v;
            }
            catch
            {
                throw new ExpressionException(
                    $"at {sourceExpressionString}: 函数 {childExp.FunctionType} 形参 {childExp.DataString} 映射到实参 {childExp.RealityString} 错误");
            }
        }

        private static object Convert2ObjectValue(string tag, string sourceExpressionString)
        {
            if (tag == null)
            {
                return null;
            }

            switch (tag)
            {
                case "true":
                    return 1d;
                case "false":
                    return 0d;
                case "1":
                    return 1d;
                case "0":
                    return 0d;
                default:
                    if (tag.EndsWith("%"))
                    {
                        return double.TryParse(tag.Trim('%'), out double percentResult)
                            ? percentResult * 0.01d
                            : throw new ExpressionException($"at {sourceExpressionString}: {tag} 不是数值类型");
                    }

                    if (double.TryParse(tag, out double result))
                    {
                        return result;
                    }

                    if (DateTime.TryParse(tag, out var time))
                    {
                        return time;
                    }

                    return tag;
            }
        }
    }
}
