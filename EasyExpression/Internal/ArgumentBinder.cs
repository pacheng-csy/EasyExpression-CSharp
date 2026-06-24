using System.Collections.Generic;

namespace EasyExpression.Internal
{
    internal static class ArgumentBinder
    {
        internal static List<KeyValuePair<string, string>> LoadArgument(
            Expression expression,
            Dictionary<string, object> keyValues)
        {
            var result = new List<KeyValuePair<string, string>>();
            LoadArgument(expression, keyValues, result, expression.ElementType == ElementType.Function);
            return result;
        }

        internal static void LoadArgumentWithoutDictionary(Expression expression)
        {
            if (expression.ElementType == ElementType.Data
                || expression.ElementType == ElementType.Function && !string.IsNullOrEmpty(expression.DataString))
            {
                expression.RealityString = expression.DataString;
            }

            foreach (var childExp in expression.ExpressionChildren)
            {
                LoadArgumentWithoutDictionary(childExp);
            }
        }

        private static void LoadArgument(
            Expression expression,
            Dictionary<string, object> keyValues,
            List<KeyValuePair<string, string>> result,
            bool zeroInit = false)
        {
            if (!string.IsNullOrEmpty(expression.DataString))
            {
                if (expression.ElementType == ElementType.Function)
                {
                    var allParams = expression.GetAllParams();
                    foreach (var param in allParams)
                    {
                        if (keyValues.TryGetValue(param.Key, out var v))
                        {
                            expression.DataString = v is null
                                ? null
                                : expression.DataString?.Replace(param.Key, v?.ToString());
                        }
                    }

                    expression.RealityString = expression.DataString;
                    zeroInit = expression.FunctionType == FunctionType.Avg
                        || expression.FunctionType == FunctionType.Sum;
                }
                else
                {
                    if (keyValues.TryGetValue(expression.DataString, out var v))
                    {
                        if (zeroInit && v is string v1 && v1 == "")
                        {
                            expression.RealityString = "0";
                        }
                        else if (v is null)
                        {
                            expression.RealityString = null;
                        }
                        else
                        {
                            expression.RealityString = v.ToString();
                        }

                        result.Add(new KeyValuePair<string, string>(expression.DataString, expression.RealityString));
                    }
                    else
                    {
                        expression.RealityString = expression.DataString;
                    }
                }
            }
            else
            {
                expression.RealityString = expression.DataString;
            }

            foreach (var childExp in expression.ExpressionChildren)
            {
                LoadArgument(childExp, keyValues, result, zeroInit);
            }
        }
    }
}
