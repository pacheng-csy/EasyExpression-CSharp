using System;
using System.Globalization;

namespace EasyExpression.Internal
{
    internal enum ValueKind
    {
        Null = 0,
        Number = 1,
        Text = 2,
        Date = 3,
        TimeSpan = 4,
        /// <summary>
        /// 未提供实参的变量，按变量名文本保留，参与数值运算时给出明确错误。
        /// </summary>
        Unbound = 5
    }

    /// <summary>
    /// 求值过程中的类型化值，替代原有的 object + 强制转换。
    /// </summary>
    internal struct ExpressionValue
    {
        public ValueKind Kind;
        public double Number;
        public string Text;
        public DateTime Date;
        public TimeSpan TimeSpan;

        internal static ExpressionValue NullValue()
        {
            return new ExpressionValue { Kind = ValueKind.Null };
        }

        internal static ExpressionValue FromNumber(double value)
        {
            return new ExpressionValue { Kind = ValueKind.Number, Number = value };
        }

        internal static ExpressionValue FromText(string value)
        {
            return new ExpressionValue { Kind = ValueKind.Text, Text = value ?? string.Empty };
        }

        internal static ExpressionValue FromDate(DateTime value)
        {
            return new ExpressionValue { Kind = ValueKind.Date, Date = value };
        }

        internal static ExpressionValue FromTimeSpan(TimeSpan value)
        {
            return new ExpressionValue { Kind = ValueKind.TimeSpan, TimeSpan = value };
        }

        internal static ExpressionValue FromUnbound(string name)
        {
            return new ExpressionValue { Kind = ValueKind.Unbound, Text = name ?? string.Empty };
        }

        internal static ExpressionValue FromObject(object value, CultureInfo culture)
        {
            if (value == null)
            {
                return NullValue();
            }

            if (value is double d)
            {
                return FromNumber(d);
            }

            if (value is bool b)
            {
                return FromNumber(b ? 1d : 0d);
            }

            if (value is DateTime dateTime)
            {
                return FromDate(dateTime);
            }

            if (value is TimeSpan timeSpan)
            {
                return FromTimeSpan(timeSpan);
            }

            if (value is string text)
            {
                return FromText(text);
            }

            if (value is int || value is long || value is short || value is byte
                || value is float || value is decimal)
            {
                return FromNumber(Convert.ToDouble(value, culture));
            }

            return FromTextValue(Convert.ToString(value, culture), culture);
        }

        /// <summary>
        /// 按原有 Convert2ObjectValue 的规则把文本转换为数值 / 日期 / 字符串。
        /// </summary>
        internal static ExpressionValue FromTextValue(string text, CultureInfo culture)
        {
            if (text == null)
            {
                return NullValue();
            }

            switch (text)
            {
                case "true":
                    return FromNumber(1d);
                case "false":
                    return FromNumber(0d);
                case "1":
                    return FromNumber(1d);
                case "0":
                    return FromNumber(0d);
            }

            if (text.EndsWith("%", StringComparison.Ordinal))
            {
                if (double.TryParse(text.Trim('%'), NumberStyles.Any, culture, out var percent))
                {
                    return FromNumber(percent * 0.01d);
                }

                throw new ExpressionException($"{text} 不是数值类型");
            }

            if (double.TryParse(text, NumberStyles.Any, culture, out var number))
            {
                return FromNumber(number);
            }

            if (DateTime.TryParse(text, culture, DateTimeStyles.None, out var time))
            {
                return FromDate(time);
            }

            return FromText(text);
        }

        internal double ToNumber(string context)
        {
            switch (Kind)
            {
                case ValueKind.Number:
                    return Number;
                case ValueKind.Null:
                    throw new ExpressionException($"{context}: 值为空，无法作为数值参与运算");
                case ValueKind.Unbound:
                    throw new ExpressionException($"参数 '{Text}' 未提供实参");
                case ValueKind.Text:
                    throw new ExpressionException($"{context}: '{Text}' 不是数值类型");
                default:
                    throw new ExpressionException($"{context}: {Kind} 类型的值不能作为数值参与运算");
            }
        }

        internal object ToObject()
        {
            switch (Kind)
            {
                case ValueKind.Number:
                    return Number;
                case ValueKind.Text:
                    return Text;
                case ValueKind.Unbound:
                    return Text;
                case ValueKind.Date:
                    return Date;
                case ValueKind.TimeSpan:
                    return TimeSpan;
                default:
                    return null;
            }
        }
    }
}