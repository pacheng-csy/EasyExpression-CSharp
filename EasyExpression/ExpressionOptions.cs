using System.Globalization;

namespace EasyExpression
{
    /// <summary>
    /// 表达式解析与求值选项。
    /// </summary>
    public sealed class ExpressionOptions
    {
        internal static readonly ExpressionOptions Default = new ExpressionOptions();

        /// <summary>
        /// 数值、日期转换使用的区域文化。默认使用当前区域文化以保持兼容。
        /// </summary>
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    }
}