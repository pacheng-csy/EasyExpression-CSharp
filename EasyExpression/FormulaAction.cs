using System;
using System.Linq;

namespace EasyExpression
{
    /// <summary>
    /// 函数委托：接收按顺序求值后的实参，返回函数结果。
    /// </summary>
    /// <param name="values">实参数组。</param>
    /// <returns>函数结果，支持 double、string、bool、DateTime、TimeSpan 或 null。</returns>
    public delegate object Function(params object[] values);

    public class FormulaAction
    {
        #region Math

        [FunctionRemark("SUM", "[SUM]", "数学", "数值、结果为数值的表达式、结果为数值的函数", "双精度浮点数", "求和\r\n参数以英文逗号分隔，无数量限制")]
        public static object Sum(params object[] values)
        {
            return values.Select(x => Convert.ToDouble(x)).Sum();
        }
        [FunctionRemark("AVG", "[AVG]", "数学", "数值、结果为数值的表达式、结果为数值的函数", "双精度浮点数", "求平均值\r\n参数以英文逗号分隔，无数量限制")]
        public static object Avg(params object[] values)
        {
            return values.Select(x => Convert.ToDouble(x)).Average();
        }

        [FunctionRemark("ROUND", "[ROUND]", "数学", "ROUND(a,b,c)\r\na=需要进行精度处理的数值\r\nb=保留的小数位数，\r\nc=精度处理模式(1:进位,向上处理;0:四舍五入;-1:舍位,向下处理)", "双精度浮点数", "ROUND(3.14,1,1)，返回3.2\r\nROUND(3.19,1,-1)，返回3.1\r\nROUND(18.88,1,0)，返回18.9")]
        public static object Round(params object[] values)
        {
            double value;
            if (values[0] is double v)
            {
                value = v;
            }
            else if (double.TryParse(values[0].ToString(), out var v1))
            {
                value = v1;
            }
            else
            {
                throw new Exception("ROUND execute error");
            }
            var accuracy = Convert.ToInt32(values[1]);
            var mode = Convert.ToInt32(values[2]);
            var delta = 5 / Math.Pow(10, accuracy + 1);
            switch (mode)
            {
                case -1:
                    return Math.Round(value - delta, accuracy);
                case 0:
                    return Math.Round(value, accuracy);
                case 1:
                    return Math.Round(value + delta, accuracy);
            }
            throw new Exception("ROUND execute error");
        }
        #endregion

        #region String

        [FunctionRemark("CONTAINS", "[CONTAINS]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "包含\r\nsource.contains(target)")]
        public static object Contains(params object[] values)
        {
            return ToText(values, 0).Contains(ToText(values, 1)) ? 1d : 0d;
        }
        [FunctionRemark("EXCLUDING", "[EXCLUDING]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "不包含\r\nsource.excluding(target)")]
        public static object Excluding(params object[] values)
        {
            return ToText(values, 0).Contains(ToText(values, 1)) ? 0d : 1d;
        }
        [FunctionRemark("STARTWITH", "[STARTWITH]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "开头是\r\nsource.startwith(target)")]
        public static object StartWith(params object[] values)
        {
            return ToText(values, 0).StartsWith(ToText(values, 1)) ? 1d : 0d;
        }
        [FunctionRemark("ENDWITH", "[ENDWITH]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "结尾是\r\nsource.endwith(target)")]
        public static object EndWith(params object[] values)
        {
            return ToText(values, 0).EndsWith(ToText(values, 1)) ? 1d : 0d;
        }
        [FunctionRemark("EQUALS", "[EQUALS]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "完全匹配\r\nsource.equals(target)")]
        public static object Equals(params object[] values)
        {
            return object.Equals(GetValue(values, 0), GetValue(values, 1)) ? 1d : 0d;
        }
        [FunctionRemark("DIFFERENT", "[DIFFERENT]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "不匹配\r\nsource.different(target)")]
        public static object Different(params object[] values)
        {
            return object.Equals(GetValue(values, 0), GetValue(values, 1)) ? 0d : 1d;
        }

        private static object GetValue(object[] values, int index)
        {
            return values != null && index < values.Length ? values[index] : null;
        }

        private static string ToText(object[] values, int index)
        {
            var value = GetValue(values, index);
            return value == null ? string.Empty : value.ToString();
        }
        [FunctionRemark("CONCAT", "[CONCAT]", "字符串", "1. value \r\n 2. value ...", "字符串", "拼接\r\n[CONCAT]('a','b')，返回ab\r\n参数以英文逗号分隔，无数量限制，null参数按空字符串处理")]
        public static object Concat(params object[] values)
        {
            return values == null
                ? string.Empty
                : string.Concat(values.Select(x => x?.ToString() ?? string.Empty));
        }

        [FunctionRemark("LEN", "[LEN]", "字符串", "1. source", "数值", "字符串长度\r\n[LEN]('abc')，返回3\r\nnull 按空字符串处理")]
        public static object Len(params object[] values)
        {
            return ToText(values, 0).Length;
        }

        [FunctionRemark("SUBSTR", "[SUBSTR]", "字符串", "1. source \r\n 2. start（从1开始）\r\n 3. length（可选）", "字符串", "截取子串\r\n[SUBSTR]('abcdef',2,3)，返回bcd\r\n[SUBSTR]('abcdef',3)，返回cdef\r\nnull 按空字符串处理")]
        public static object Substr(params object[] values)
        {
            var source = ToText(values, 0);
            var start = values != null && values.Length > 1 ? Convert.ToInt32(GetValue(values, 1)) : 1;
            if (start < 1)
            {
                start = 1;
            }

            var offset = start - 1;
            if (offset >= source.Length)
            {
                return string.Empty;
            }

            if (values != null && values.Length > 2)
            {
                var length = Convert.ToInt32(GetValue(values, 2));
                if (length <= 0)
                {
                    return string.Empty;
                }

                if (offset + length > source.Length)
                {
                    length = source.Length - offset;
                }

                return source.Substring(offset, length);
            }

            return source.Substring(offset);
        }

        [FunctionRemark("REPLACE", "[REPLACE]", "字符串", "1. source \r\n 2. oldValue \r\n 3. newValue", "字符串", "替换子串\r\n[REPLACE]('a-b','-','_')，返回a_b\r\noldValue 为空时原样返回")]
        public static object Replace(params object[] values)
        {
            var source = ToText(values, 0);
            var oldValue = ToText(values, 1);
            return oldValue.Length == 0 ? source : source.Replace(oldValue, ToText(values, 2));
        }

        [FunctionRemark("UPPER", "[UPPER]", "字符串", "1. source", "字符串", "转大写\r\n[UPPER]('abc')，返回ABC")]
        public static object Upper(params object[] values)
        {
            return ToText(values, 0).ToUpperInvariant();
        }

        [FunctionRemark("LOWER", "[LOWER]", "字符串", "1. source", "字符串", "转小写\r\n[LOWER]('ABC')，返回abc")]
        public static object Lower(params object[] values)
        {
            return ToText(values, 0).ToLowerInvariant();
        }

        [FunctionRemark("TRIM", "[TRIM]", "字符串", "1. source", "字符串", "去除首尾空白\r\n[TRIM]('  abc  ')，返回abc")]
        public static object Trim(params object[] values)
        {
            return ToText(values, 0).Trim();
        }

        #endregion

        #region Logic

        [FunctionRemark("IF", "[IF]", "逻辑", "1. condition \r\n 2. trueValue \r\n 3. falseValue", "任意", "条件分支，仅计算命中的分支\r\n[IF](a > 0,'正','非正')")]
        public static object If(params object[] values)
        {
            throw new ExpressionException("IF 由求值器惰性执行，不能直接调用");
        }

        [FunctionRemark("CASE", "[CASE]", "逻辑", "1. value \r\n 2. pattern,result（可多组）\r\n 最后可选的默认值", "任意", "多分支匹配，仅计算命中的分支\r\n[CASE](2,1,'a',2,'b','c')，返回b\r\n[CASE](9,1,'a',2,'b','c')，返回c")]
        public static object Case(params object[] values)
        {
            throw new ExpressionException("CASE 由求值器惰性执行，不能直接调用");
        }

        #endregion

        #region Time
        [FunctionRemark("EDATE", "[EDATE]", "时间", "EDATE(a,b,c)\r\na=\"StartDate\"\r\nb=Unit", "日期值", "EDATE(\"2019-10-10\",10,\"D\")，返回值代表指定日期之前或之后n天/月/年的日期\r\n参数按照顺序以英文逗号分隔")]
        public static object EDate(params object[] values)
        {
            if (values[0] is DateTime dateTime)
            {
                if (int.TryParse(values[1].ToString(), out var delta))
                {
                    var mode = values[2].ToString();
                    switch (mode)
                    {
                        case "y":
                        case "Y":
                            dateTime = dateTime.AddYears(delta);
                            break;
                        case "M":
                            dateTime = dateTime.AddMonths(delta);
                            break;
                        case "d":
                        case "D":
                            dateTime = dateTime.AddDays(delta);
                            break;
                        case "h":
                        case "H":
                            dateTime = dateTime.AddHours(delta);
                            break;
                        case "m":
                            dateTime = dateTime.AddMinutes(delta);
                            break;
                        case "s":
                        case "S":
                            dateTime = dateTime.AddSeconds(delta);
                            break;
                        case "f":
                        case "F":
                            dateTime = dateTime.AddMilliseconds(delta);
                            break;
                    }
                    return dateTime;
                }
            }

            throw new Exception("EDATE execute error");
        }

        [FunctionRemark("EODATE", "[EODATE]", "时间", "EODATE(a,b,c)\r\na=\"StartDate\"\r\nb=Months", "日期值", "EODATE(\"2019-10-10\",10,\"S\")，返回StartDate前后n个月最后一天或者第一天的日期\r\n参数按照顺序以英文逗号分隔\r\nC={\"S\" \"E”}")]
        public static object EODate(params object[] values)
        {
            if (values[0] is DateTime startTime)
            {
                if (int.TryParse(values[1].ToString(), out var delta))
                {
                    var mode = values[2].ToString();
                    startTime = startTime.AddMonths(delta);
                    switch (mode)
                    {
                        case "s":
                        case "S":
                            return new DateTime(startTime.Year, startTime.Month, 1);
                        case "e":
                        case "E":
                            return new DateTime(startTime.Year, startTime.Month, 1).AddMonths(1).AddDays(-1);
                    }
                }
            }
            throw new Exception("EODate execute error");
        }

        [FunctionRemark("NOWTIME", "[NOWTIME]", "时间", "无参数", "日期值", "NOWTIME()，获取当前时间，默认返回今天的日期")]
        public static object NowTime(params object[] values)
        {
            return DateTime.Now;
        }

        [FunctionRemark("TIMETOSTRING", "[TIMETOSTRING]", "时间", "TOSTRING(a,b)\r\na=日期、结果为日期的表达式、结果为日期的函数\r\nb=日期格式", "日期值", "TOSTRING(\"2019-10-10\"，\"YYYY.DD.MM\")，返回特定格式")]
        public static object TimeToString(params object[] values)
        {
            if (values[0] is DateTime dateTime)
            {
                return dateTime.ToString(values[1].ToString());
            }
            if (DateTime.TryParse(values[0].ToString(), out var time))
            {
                return time.ToString(values[1].ToString());
            }
            throw new Exception("TIMETOSTRING execute error");
        }

        #endregion

        #region TimeSpan
        [FunctionRemark("DAYS", "[DAYS]", "时间段", "DAYS(time1-time2)", "天数", "DAYS('2019-10-10' - '2019-10-05')，返回值代表时间段对应的天数\r\n参数为两个时间相减的表达式")]
        public static object Days(params object[] values)
        {
            if (values[0] is TimeSpan timeSpan)
            {
                return timeSpan.TotalDays;
            }
            throw new Exception("Days execute error");
        }
        [FunctionRemark("HOURS", "[HOURS]", "时间段", "HOURS(time1-time2", "小时数", "HOURS('2019-10-10' - '2019-10-05')，返回值代表时间段对应的小时数\r\n参数为两个时间相减的表达式")]
        public static object Hours(params object[] values)
        {
            if (values[0] is TimeSpan timeSpan)
            {
                return timeSpan.TotalHours;
            }
            throw new Exception("Hours execute error");
        }
        [FunctionRemark("MINUTES", "[MINUTES]", "时间段", "MINUTES(time1-time2)", "分钟数", "MINUTES('2019-10-10' - '2019-10-05')，返回值代表时间段对应的分钟数\r\n参数为两个时间相减的表达式")]
        public static object Minutes(params object[] values)
        {
            if (values[0] is TimeSpan timeSpan)
            {
                return timeSpan.TotalMinutes;
            }
            throw new Exception("Minutes execute error");
        }
        [FunctionRemark("SECONDS", "[SECONDS]", "时间段", "SECONDS(time1-time2)", "秒数", "SECONDS('2019-10-10' - '2019-10-05')，返回值代表时间段对应的秒数\r\n参数为两个时间相减的表达式")]
        public static object Seconds(params object[] values)
        {
            if (values[0] is TimeSpan timeSpan)
            {
                return timeSpan.TotalSeconds;
            }
            throw new Exception("Seconds execute error");
        }
        [FunctionRemark("MILLSECONDS", "[MILLSECONDS]", "时间段", "MILLSECONDS(time1-time2)", "毫秒数", "MILLSECONDS('2019-10-10' - '2019-10-05')，返回值代表时间段对应的毫秒数\r\n参数为两个时间相减的表达式")]
        public static object MillSeconds(params object[] values)
        {
            if (values[0] is TimeSpan timeSpan)
            {
                return timeSpan.TotalMilliseconds;
            }
            throw new Exception("MillSeconds execute error");
        }
        #endregion

        #region object

        [FunctionRemark("ISNULL", "[ISNULL]", "对象", "1. obj", "0（false）或者1（true）", "是否为空\r\n isnull(obj)")]
        public static object IsNull(params object[] values)
        {
            return values.FirstOrDefault() == null ? 1d : 0d;
        }

        #endregion
    }
}
