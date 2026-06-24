using System;
using System.Collections.Generic;
using System.Reflection;

namespace EasyExpression.Internal
{
    internal static class OperatorInfoCache
    {
        private static readonly Dictionary<Operator, OperatorAttribute> Cache = BuildCache();

        internal static int GetLevel(Operator op) => GetAttribute(op).Level;

        internal static string GetValue(Operator op) => GetAttribute(op).Value;

        internal static OperatorAttribute GetAttribute(Operator op)
        {
            if (Cache.TryGetValue(op, out var attribute))
            {
                return attribute;
            }

            throw new InvalidOperationException($"Operator {op} has no metadata.");
        }

        private static Dictionary<Operator, OperatorAttribute> BuildCache()
        {
            var cache = new Dictionary<Operator, OperatorAttribute>();
            foreach (Operator op in Enum.GetValues(typeof(Operator)))
            {
                var field = typeof(Operator).GetField(op.ToString());
                if (field == null)
                {
                    continue;
                }

                var attributes = (OperatorAttribute[])field.GetCustomAttributes(typeof(OperatorAttribute), false);
                if (attributes.Length > 0)
                {
                    cache[op] = attributes[0];
                }
            }

            return cache;
        }
    }
}
