using System;
using System.Linq;

namespace EasyExpression
{
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
            return (values[0].ToString()).Contains(values[1].ToString()) ? 1d : 0d;
        }
        [FunctionRemark("EXCLUDING", "[EXCLUDING]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "不包含\r\nsource.excluding(target)")]
        public static object Excluding(params object[] values)
        {
            return ((string)values[0]).Contains((string)values[1]) ? 0d : 1d;
        }
        [FunctionRemark("STARTWITH", "[STARTWITH]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "开头是\r\nsource.startwith(target)")]
        public static object StartWith(params object[] values)
        {
            return ((string)values[0]).StartsWith((string)values[1]) ? 1d : 0d;
        }
        [FunctionRemark("ENDWITH", "[ENDWITH]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "结尾是\r\nsource.endwith(target)")]
        public static object EndWith(params object[] values)
        {
            return (values[0].ToString()).EndsWith(values[1].ToString()) ? 1d : 0d;
        }
        [FunctionRemark("EQUALS", "[EQUALS]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "完全匹配\r\nsource.equals(target)")]
        public static object Equals(params object[] values)
        {
            return (values[0]).Equals(values[1]) ? 1d : 0d;
        }
        [FunctionRemark("DIFFERENT", "[DIFFERENT]", "字符串", "1. source \r\n 2. target", "0（false）或者1（true）", "不匹配\r\nsource.different(target)")]
        public static object Different(params object[] values)
        {
            return ((string)values[0]).Equals((string)values[1]) ? 0d : 1d;
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
                            return new DateTime(startTime.Year, startTime.Month + 1, 1).AddDays(-1);
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
