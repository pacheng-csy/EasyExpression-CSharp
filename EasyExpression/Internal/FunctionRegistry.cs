using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace EasyExpression.Internal
{
    internal sealed class FunctionDefinition
    {
        internal FunctionDefinition(string name, FunctionType type, Function function, int minArgs, int maxArgs)
        {
            Name = name;
            Type = type;
            Function = function;
            MinArgs = minArgs;
            MaxArgs = maxArgs;
        }

        internal string Name { get; }

        internal FunctionType Type { get; }

        internal Function Function { get; }

        internal int MinArgs { get; }

        internal int MaxArgs { get; }
    }
}

namespace EasyExpression
{
    /// <summary>
    /// 函数注册表。内置函数通过 <see cref="FormulaAction"/> 上的 <see cref="FunctionRemarkAttribute"/> 自动注册。
    /// </summary>
    public static class FunctionRegistry
    {
        private static readonly ConcurrentDictionary<string, Internal.FunctionDefinition> Definitions =
            new ConcurrentDictionary<string, Internal.FunctionDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<FunctionType, Internal.FunctionDefinition> DefinitionsByType =
            new ConcurrentDictionary<FunctionType, Internal.FunctionDefinition>();

        static FunctionRegistry()
        {
            RegisterBuiltIns();
        }

        /// <summary>
        /// 注册自定义函数。同名内置函数默认不允许覆盖。
        /// </summary>
        public static bool Register(
            string name,
            Function function,
            int minArgs = 0,
            int maxArgs = int.MaxValue,
            bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("函数名不能为空", nameof(name));
            }

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (minArgs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minArgs));
            }

            if (maxArgs < minArgs)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArgs));
            }

            return RegisterCore(
                new Internal.FunctionDefinition(name.Trim(), FunctionType.Customer, function, minArgs, maxArgs),
                overwrite);
        }

        /// <summary>
        /// 移除已注册的函数。
        /// </summary>
        public static bool Unregister(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            Internal.FunctionDefinition removed;
            if (!Definitions.TryRemove(name.Trim(), out removed))
            {
                return false;
            }

            if (removed.Type != FunctionType.Customer)
            {
                Internal.FunctionDefinition byType;
                DefinitionsByType.TryRemove(removed.Type, out byType);
            }

            return true;
        }

        /// <summary>
        /// 判断函数是否已注册。
        /// </summary>
        public static bool Contains(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && Definitions.ContainsKey(name.Trim());
        }

        internal static Internal.FunctionDefinition Get(string name)
        {
            Internal.FunctionDefinition definition;
            if (!string.IsNullOrWhiteSpace(name) && Definitions.TryGetValue(name.Trim(), out definition))
            {
                return definition;
            }

            throw new ExpressionException($"{name} 函数未定义");
        }

        internal static bool TryGet(string name, out Internal.FunctionDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(name) && Definitions.TryGetValue(name.Trim(), out definition);
        }

        internal static bool TryGetByType(FunctionType type, out Internal.FunctionDefinition definition)
        {
            return DefinitionsByType.TryGetValue(type, out definition);
        }

        private static bool RegisterCore(Internal.FunctionDefinition definition, bool overwrite)
        {
            if (!overwrite && Definitions.ContainsKey(definition.Name))
            {
                return false;
            }

            Definitions[definition.Name] = definition;
            if (definition.Type != FunctionType.Customer)
            {
                DefinitionsByType[definition.Type] = definition;
            }

            return true;
        }

        private static void RegisterBuiltIns()
        {
            var methods = typeof(FormulaAction)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .OrderBy(x => x.Name, StringComparer.Ordinal);

            foreach (var method in methods)
            {
                var remark = (FunctionRemarkAttribute)method
                    .GetCustomAttributes(typeof(FunctionRemarkAttribute), false)
                    .FirstOrDefault();

                if (remark == null || string.IsNullOrWhiteSpace(remark.Name))
                {
                    continue;
                }

                Function function;
                try
                {
                    function = (Function)Delegate.CreateDelegate(typeof(Function), method);
                }
                catch (ArgumentException)
                {
                    // 非 params object[] 签名的静态方法不参与自动注册。
                    continue;
                }

                var type = ResolveFunctionType(method);
                RegisterCore(
                    new Internal.FunctionDefinition(remark.Name, type, function, 0, int.MaxValue),
                    true);
            }
        }

        private static FunctionType ResolveFunctionType(MethodInfo method)
        {
            if (string.Equals(method.Name, "Excluding", StringComparison.OrdinalIgnoreCase))
            {
                return FunctionType.ContainsExcept;
            }

            FunctionType type;
            if (Enum.TryParse(method.Name, true, out type) && type != FunctionType.None)
            {
                return type;
            }

            return FunctionType.Customer;
        }
    }
}