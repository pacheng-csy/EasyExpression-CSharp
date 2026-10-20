using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasyExpression.Internal
{
    internal static class ArgumentBinder
    {
        internal static List<KeyValuePair<string, string>> LoadArgument(
            Expression expression,
            Dictionary<string, object> keyValues)
        {
            var result = new List<KeyValuePair<string, string>>();
            Bind(expression, keyValues ?? new Dictionary<string, object>(), GetCulture(expression), result, false);
            expression.ArgumentsLoaded = true;
            return result;
        }

        internal static void LoadArgumentWithoutDictionary(Expression expression)
        {
            Reset(expression);
            expression.ArgumentsLoaded = true;
        }

        /// <summary>
        /// 把表达式树还原为未绑定实参的状态，便于重复调用 LoadArgument。
        /// </summary>
        private static void Reset(Expression expression)
        {
            if (expression == null)
            {
                return;
            }

            expression.RealityString = expression.DataString;
            expression.HasBoundValue = false;
            expression.BoundValue = null;
            expression.IsUnbound = false;

            if (expression.ExpressionChildren == null)
            {
                return;
            }

            foreach (var child in expression.ExpressionChildren)
            {
                Reset(child);
            }
        }

        private static void Bind(
            Expression expression,
            Dictionary<string, object> keyValues,
            CultureInfo culture,
            List<KeyValuePair<string, string>> result,
            bool zeroInit)
        {
            if (expression == null)
            {
                return;
            }

            if (expression.ElementType == ElementType.Data || expression.ElementType == ElementType.Reference)
            {
                BindData(expression, keyValues, culture, result, zeroInit);
            }
            else
            {
                expression.RealityString = expression.DataString;
            }

            if (expression.ExpressionChildren == null)
            {
                return;
            }

            var childrenZeroInit = zeroInit
                || (expression.ElementType == ElementType.Function
                    && (expression.FunctionType == FunctionType.Avg || expression.FunctionType == FunctionType.Sum));

            foreach (var child in expression.ExpressionChildren)
            {
                Bind(child, keyValues, culture, result, childrenZeroInit);
            }
        }

        private static void BindData(
            Expression expression,
            Dictionary<string, object> keyValues,
            CultureInfo culture,
            List<KeyValuePair<string, string>> result,
            bool zeroInit)
        {
            expression.HasBoundValue = false;
            expression.BoundValue = null;
            expression.IsUnbound = false;

            if (expression.IsLiteral)
            {
                expression.RealityString = expression.DataString;
                return;
            }

            object value;
            if (!string.IsNullOrEmpty(expression.DataString)
                && keyValues.TryGetValue(expression.DataString, out value))
            {
                if (zeroInit && value is string text && text.Length == 0)
                {
                    value = 0d;
                }

                expression.HasBoundValue = true;
                expression.BoundValue = value;
                expression.RealityString = value == null ? null : Convert.ToString(value, culture);
                result.Add(new KeyValuePair<string, string>(expression.DataString, expression.RealityString));
            }
            else
            {
                expression.IsUnbound = true;
                expression.RealityString = expression.DataString;
            }
        }

        private static CultureInfo GetCulture(Expression expression)
        {
            return expression.Options != null && expression.Options.Culture != null
                ? expression.Options.Culture
                : ExpressionOptions.Default.Culture;
        }
    }
}