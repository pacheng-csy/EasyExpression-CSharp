using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasyExpression.Internal
{
    internal static class Evaluator
    {
        internal static object Execute(Expression expression)
        {
            return Evaluate(expression).ToObject();
        }

        private static ExpressionValue Evaluate(Expression node)
        {
            if (node == null)
            {
                throw new ExpressionException("表达式节点为空");
            }

            switch (node.ElementType)
            {
                case ElementType.Data:
                case ElementType.Reference:
                    return EvaluateData(node);
                case ElementType.Function:
                    return EvaluateFunction(node);
                case ElementType.Expression:
                case ElementType.Placeholder:
                    return EvaluateOperator(node);
                default:
                    throw new ExpressionException($"at {node.SourceExpressionString}: 未知表达式节点");
            }
        }

        private static ExpressionValue EvaluateData(Expression node)
        {
            var culture = GetCulture(node);
            if (node.HasBoundValue)
            {
                if (node.BoundValue == null)
                {
                    return ExpressionValue.NullValue();
                }

                // 外部传入的字符串沿用原有规则转换，其余类型保留其真实类型。
                if (node.BoundValue is string text)
                {
                    return ExpressionValue.FromTextValue(text, culture);
                }

                return ExpressionValue.FromObject(node.BoundValue, culture);
            }

            if (node.IsLiteral)
            {
                return ExpressionValue.FromTextValue(node.DataString, culture);
            }

            if (!string.IsNullOrEmpty(node.RealityString)
                && !string.Equals(node.RealityString, node.DataString, StringComparison.Ordinal))
            {
                return ExpressionValue.FromTextValue(node.RealityString, culture);
            }

            var value = ExpressionValue.FromTextValue(node.DataString, culture);
            if (node.IsUnbound && value.Kind == ValueKind.Text)
            {
                return ExpressionValue.FromUnbound(value.Text);
            }

            return value;
        }

        private static ExpressionValue EvaluateOperator(Expression node)
        {
            if (node.Operators == null || node.Operators.Count == 0)
            {
                if (node.ExpressionChildren == null || node.ExpressionChildren.Count == 0)
                {
                    throw new ExpressionException($"at {node.SourceExpressionString}: 表达式不完整");
                }

                if (node.ExpressionChildren.Count == 1)
                {
                    return Evaluate(node.ExpressionChildren[0]);
                }

                throw new ExpressionException($"at {node.SourceExpressionString}: 数据与运算符不匹配");
            }

            if (node.ExpressionChildren == null || node.ExpressionChildren.Count == 0)
            {
                throw new ExpressionException($"at {node.SourceExpressionString}: 表达式缺少操作数");
            }

            var current = Evaluate(node.ExpressionChildren[0]);
            for (var i = 0; i < node.Operators.Count; i++)
            {
                var op = node.Operators[i];
                if (op == Operator.Not || op == Operator.Negative)
                {
                    current = ApplyUnary(op, current, node.SourceExpressionString);
                    continue;
                }

                if (i + 1 >= node.ExpressionChildren.Count)
                {
                    throw new ExpressionException($"at {node.SourceExpressionString}: 运算符 {op} 缺少操作数");
                }

                var right = Evaluate(node.ExpressionChildren[i + 1]);
                current = ApplyBinary(op, current, right, node);
            }

            return current;
        }

        private static ExpressionValue ApplyUnary(Operator op, ExpressionValue value, string context)
        {
            switch (op)
            {
                case Operator.Not:
                    return ExpressionValue.FromNumber(value.ToNumber(context) == 0d ? 1d : 0d);
                case Operator.Negative:
                    return ExpressionValue.FromNumber(-value.ToNumber(context));
                default:
                    throw new ExpressionException($"at {context}: 未知一元运算符 {op}");
            }
        }

        private static ExpressionValue ApplyBinary(
            Operator op,
            ExpressionValue left,
            ExpressionValue right,
            Expression node)
        {
            var context = node.SourceExpressionString;
            switch (op)
            {
                case Operator.And:
                    return ExpressionValue.FromNumber(
                        left.ToNumber(context) != 0d && right.ToNumber(context) != 0d ? 1d : 0d);
                case Operator.Or:
                    return ExpressionValue.FromNumber(
                        left.ToNumber(context) != 0d || right.ToNumber(context) != 0d ? 1d : 0d);
                case Operator.Plus:
                    return ExpressionValue.FromNumber(left.ToNumber(context) + right.ToNumber(context));
                case Operator.Subtract:
                    if (left.Kind == ValueKind.Date && right.Kind == ValueKind.Date)
                    {
                        return ExpressionValue.FromTimeSpan(left.Date - right.Date);
                    }

                    return ExpressionValue.FromNumber(left.ToNumber(context) - right.ToNumber(context));
                case Operator.Multiply:
                    return ExpressionValue.FromNumber(left.ToNumber(context) * right.ToNumber(context));
                case Operator.Divide:
                    return Divide(left, right, node, context);
                case Operator.Mod:
                    return ExpressionValue.FromNumber(left.ToNumber(context) % right.ToNumber(context));
                case Operator.GreaterThan:
                    return ExpressionValue.FromNumber(Compare(left, right, context) > 0 ? 1d : 0d);
                case Operator.LessThan:
                    return ExpressionValue.FromNumber(Compare(left, right, context) < 0 ? 1d : 0d);
                case Operator.GreaterThanOrEquals:
                    return ExpressionValue.FromNumber(Compare(left, right, context) >= 0 ? 1d : 0d);
                case Operator.LessThanOrEquals:
                    return ExpressionValue.FromNumber(Compare(left, right, context) <= 0 ? 1d : 0d);
                case Operator.Equals:
                    return ExpressionValue.FromNumber(AreEqual(left, right) ? 1d : 0d);
                case Operator.UnEquals:
                    return ExpressionValue.FromNumber(AreEqual(left, right) ? 0d : 1d);
                default:
                    throw new ExpressionException($"at {context}: 未知二元运算符 {op}");
            }
        }

        private static ExpressionValue Divide(
            ExpressionValue left,
            ExpressionValue right,
            Expression node,
            string context)
        {
            var dividend = left.ToNumber(context);
            var divisor = right.ToNumber(context);
            if (divisor != 0d)
            {
                return ExpressionValue.FromNumber(dividend / divisor);
            }

            switch (GetOptions(node).DivisionByZero)
            {
                case DivisionByZeroBehavior.Zero:
                    return ExpressionValue.FromNumber(0d);
                case DivisionByZeroBehavior.Throw:
                    throw new ExpressionException($"at {context}: 除数不能为 0");
                default:
                    return ExpressionValue.FromNumber(dividend / divisor);
            }
        }

        private static int Compare(ExpressionValue left, ExpressionValue right, string context)
        {
            if (left.Kind == ValueKind.Date && right.Kind == ValueKind.Date)
            {
                return left.Date.CompareTo(right.Date);
            }

            if (left.Kind == ValueKind.TimeSpan && right.Kind == ValueKind.TimeSpan)
            {
                return left.TimeSpan.CompareTo(right.TimeSpan);
            }

            return left.ToNumber(context).CompareTo(right.ToNumber(context));
        }

        private static bool AreEqual(ExpressionValue left, ExpressionValue right)
        {
            if (left.Kind == ValueKind.Null || right.Kind == ValueKind.Null)
            {
                return left.Kind == right.Kind;
            }

            if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            {
                return left.Number == right.Number;
            }

            if (left.Kind == ValueKind.Date && right.Kind == ValueKind.Date)
            {
                return left.Date == right.Date;
            }

            if (left.Kind == ValueKind.TimeSpan && right.Kind == ValueKind.TimeSpan)
            {
                return left.TimeSpan == right.TimeSpan;
            }

            if (IsTextual(left) && IsTextual(right))
            {
                return string.Equals(left.Text ?? string.Empty, right.Text ?? string.Empty, StringComparison.Ordinal);
            }

            return false;
        }

        private static bool IsTextual(ExpressionValue value)
        {
            return value.Kind == ValueKind.Text || value.Kind == ValueKind.Unbound;
        }

        private static ExpressionValue EvaluateFunction(Expression node)
        {
            var definition = ResolveDefinition(node);
            if (definition != null)
            {
                if (definition.Type == FunctionType.If)
                {
                    return EvaluateIf(node);
                }

                if (definition.Type == FunctionType.Case)
                {
                    return EvaluateCase(node);
                }
            }

            var function = node.Function ?? (definition == null ? null : definition.Function);
            if (function == null)
            {
                throw new ExpressionException($"at {node.SourceExpressionString}: 不存在函数实例{node.FunctionType}");
            }

            var parameters = new List<object>();
            if (node.ExpressionChildren != null)
            {
                foreach (var child in node.ExpressionChildren)
                {
                    parameters.Add(Evaluate(child).ToObject());
                }
            }

            if (definition != null
                && (parameters.Count < definition.MinArgs || parameters.Count > definition.MaxArgs))
            {
                throw new ExpressionException(
                    $"at {node.SourceExpressionString}: 函数 {definition.Name} 参数数量不正确，期望 {definition.MinArgs}-{definition.MaxArgs} 个，实际 {parameters.Count} 个");
            }

            try
            {
                return ExpressionValue.FromObject(function(parameters.ToArray()), GetCulture(node));
            }
            catch (ExpressionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExpressionException(
                    $"at {node.SourceExpressionString}: 函数 {node.FunctionName ?? node.FunctionType.ToString()} 形参 {node.DataString} 执行失败: {ex.Message}",
                    ex);
            }
        }

        private static ExpressionValue EvaluateIf(Expression node)
        {
            var children = node.ExpressionChildren;
            if (children == null || children.Count != 3)
            {
                throw new ExpressionException($"at {node.SourceExpressionString}: IF 需要 3 个参数");
            }

            var condition = Evaluate(children[0]).ToNumber(node.SourceExpressionString);
            return Evaluate(condition != 0d ? children[1] : children[2]);
        }

        private static ExpressionValue EvaluateCase(Expression node)
        {
            var children = node.ExpressionChildren;
            if (children == null || children.Count < 2)
            {
                throw new ExpressionException($"at {node.SourceExpressionString}: CASE 至少需要 2 个参数");
            }

            var value = Evaluate(children[0]);
            var pairCount = (children.Count - 1) / 2;
            var hasDefault = (children.Count - 1) % 2 == 1;
            for (var i = 0; i < pairCount; i++)
            {
                if (AreEqual(value, Evaluate(children[1 + i * 2])))
                {
                    return Evaluate(children[2 + i * 2]);
                }
            }

            return hasDefault
                ? Evaluate(children[children.Count - 1])
                : ExpressionValue.NullValue();
        }

        private static FunctionDefinition ResolveDefinition(Expression node)
        {
            FunctionDefinition definition = null;
            if (!string.IsNullOrEmpty(node.FunctionName))
            {
                FunctionRegistry.TryGet(node.FunctionName, out definition);
            }

            if (definition == null)
            {
                FunctionRegistry.TryGetByType(node.FunctionType, out definition);
            }

            return definition;
        }

        private static CultureInfo GetCulture(Expression node)
        {
            var options = GetOptions(node);
            return options.Culture ?? ExpressionOptions.Default.Culture;
        }

        private static ExpressionOptions GetOptions(Expression node)
        {
            return node.Options ?? ExpressionOptions.Default;
        }
    }
}