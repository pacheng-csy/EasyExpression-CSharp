using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace EasyExpression.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ParseTest()
        {
            var expStr = " 2 + 3* -3 > -9 || [SUM] (1,2,3) < 4";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void NullTest()
        {
            var expStr = "!a && !b";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object>()
            {
                {"a",1},
                {"b",0},
            };
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }


        [TestMethod]
        public void EmptyStringTest()
        {
            var expStr = "a==''";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object>()
            {
                {"a",""}
            };
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void NegativeTest()
        {
            var expStr = "3 * -2";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(-6d, value);
        }

        [TestMethod]
        public void LogicTest()
        {
            var expStr = "3 * (1 + 2) < = 5 || !(8 / (4 - 2) > [SUM](1,2,3))";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void MutipleFunctionTest()
        {
            var expStr = "[SUM]([SUM](1,2),[SUM](3,4),[AVG](5,6,7))";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(16d, value);
        }

        [TestMethod]
        public void MutipleExpFunctionTest()
        {
            var expStr = "3 * (1 + 2) + [SUM]([SUM](1,2),6 / 2,[AVG](5,6,7))";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(21d, value);
        }

        [TestMethod]
        public void FunctionParamsTest()
        {
            var expStr = "[EQUALS](12+3,15)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void UnEqualsTest()
        {

            var expStr = "4 != 4";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(0d, value);
        }

        [TestMethod]
        public void ArithmeticTest()
        {
            var expStr = "3 * (1 + 2) + 5 - (30 / (4 - 2) % [SUM](1,2,3))";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();

            Assert.AreEqual(11d, value);
        }

        [TestMethod]
        public void StringTest()
        {
            var expStr = "a * (b + c) > d & [Contains](srcText,text)";
            var dic = new Dictionary<string, object>
            {
                { "a","3"},
                { "b","1"},
                { "c","2"},
                { "d","4"},
                { "srcText","abc"},
                { "text","bc"},
            };
            var exp = new Expression(expStr);
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void ParamsTest()
        {
            var expStr = "a * (b + c) + 5 - (30 / (d - 2) % [SUM](1,2,3))";
            var dic = new Dictionary<string, object>
            {
                { "a","3"},
                { "b","1"},
                { "c","2"},
                { "d","4"},
            };
            var exp = new Expression(expStr);
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(11d, value);
        }

        [TestMethod]
        public void DateCompareTest()
        {
            var expStr = "'2024-05-27' == a";
            var dic = new Dictionary<string, object>
            {
                { "a","2024-05-27"},
            };
            var exp = new Expression(expStr);
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void DateMoreThenTest()
        {
            var expStr = "'2024-05-27' > a";
            var dic = new Dictionary<string, object>
            {
                { "a","2024-05-26"},
            };
            var exp = new Expression(expStr);
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void DateLessThanTest()
        {
            var expStr = "'2024-05-27' < a";
            var dic = new Dictionary<string, object>
            {
                { "a","2024-05-26"},
            };
            var exp = new Expression(expStr);
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(0d, value);
        }

        [TestMethod]
        public void EDATETest()
        {
            var expStr = "[TIMETOSTRING]([EDATE]('2024-05-27',2,D),yyyyMMdd)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual("20240529", value);
        }

        [TestMethod]
        public void EODateStartTest()
        {
            var expStr = "[TIMETOSTRING]([EODATE]('2024-05-27',2,S),yyyyMMdd)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual("20240701", value);
        }

        [TestMethod]
        public void EODateEndTest()
        {
            var expStr = "[TIMETOSTRING]([EODATE]('2024-05-27',2,E),yyyyMMdd)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual("20240731", value);
        }

        [TestMethod]
        public void NowTimeTest()
        {
            var expStr = "[TIMETOSTRING]([NOWTIME](),yyyyMMdd)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(DateTime.Now.ToString("yyyyMMdd"), value);
        }

        [TestMethod]
        public void RoundTest1()
        {
            var expStr = "[ROUND](11.34,1,-1)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(11.3, value);
        }
        [TestMethod]
        public void RoundTest2()
        {
            var expStr = "[ROUND](11.34,1,0)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(11.3, value);
        }
        [TestMethod]
        public void RoundTest3()
        {
            var expStr = "[ROUND](11.34,1,1)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(11.4, value);
        }

        [TestMethod]
        public void TimeSpanDays()
        {
            var expStr = "[DAYS]('2024-10-15'-'2024-10-10')";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(5d, value);
        }
        [TestMethod]
        public void TimeSpanHours()
        {
            var expStr = "[HOURS]('2024-10-15'-'2024-10-10')";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(120d, value);
        }
        [TestMethod]
        public void TimeSpanMinutes()
        {
            var expStr = "[MINUTES]('2024-10-15'-'2024-10-10')";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(7200d, value);
        }
        [TestMethod]
        public void TimeSpanSeconds()
        {
            var expStr = "[SECONDS]('2024-10-15'-'2024-10-10')";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(432000d, value);
        }
        [TestMethod]
        public void TimeSpanMillSeconds()
        {
            var expStr = "[MILLSECONDS]('2024-10-15'-'2024-10-10')";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(432000000d, value);
        }
        [TestMethod]
        public void RoundAndTimeSpan()
        {
            var expStr = "[ROUND]([DAYS]('2024-10-15'-'2024-10-10') / 30,1,0)";
            var exp = new Expression(expStr);
            exp.LoadArgument();
            var value = exp.Execute();
            Assert.AreEqual(0.2d, value);
        }
    }
}
