using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasyExpression
{
    /// <summary>
    /// 除数为 0 时的处理策略。
    /// </summary>
    public enum DivisionByZeroBehavior
    {
        /// <summary>
        /// 返回 ±Infinity / NaN（与历史行为一致，默认）。
        /// </summary>
        Infinity = 0,

        /// <summary>
        /// 返回 0。
        /// </summary>
        Zero = 1,

        /// <summary>
        /// 抛出 <see cref="ExpressionException"/>。
        /// </summary>
        Throw = 2
    }

    /// <summary>
    /// 表达式的解析与求值选项。
    /// </summary>
    public sealed class ExpressionOptions
    {
        internal static readonly ExpressionOptions Default = new ExpressionOptions();

        /// <summary>
        /// 数字、日期解析与格式化使用的区域性，默认 <see cref="CultureInfo.CurrentCulture"/>。
        /// </summary>
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

        /// <summary>
        /// 除数为 0 时的处理策略，默认 <see cref="DivisionByZeroBehavior.Infinity"/>。
        /// </summary>
        public DivisionByZeroBehavior DivisionByZero { get; set; } = DivisionByZeroBehavior.Infinity;

        /// <summary>
        /// 函数白名单。为 null 或空集合时不限制；否则表达式只能调用名单内的函数（不区分大小写）。
        /// </summary>
        public ISet<string> FunctionWhitelist { get; set; }

        internal bool IsFunctionAllowed(string name)
        {
            if (FunctionWhitelist == null || FunctionWhitelist.Count == 0)
            {
                return true;
            }

            foreach (var item in FunctionWhitelist)
            {
                if (string.Equals(item, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}