using EasyExpression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace EasyExpression.UnitTest
{
    /// <summary>
    /// EasyExpression 单元测试（合并原 UnitTest1 与 ExpressionAdditionalTests）。
    /// </summary>
    [TestClass]
    public class ExpressionTests
    {
        #region 基础用例（原 UnitTest1）

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
            var expStr = "[ISNULL](a)";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object>()
            {
                {"a",null},
            };
            exp.LoadArgument(dic);
            var value = exp.Execute();
            Assert.AreEqual(1d, value);
        }

        [TestMethod]
        public void NotTest()
        {
            var expStr = "!((![ISNULL](a)) && a != '' && (([ISNULL](b)) || b == '') || ((![ISNULL](b)) && b != '' && (([ISNULL](a)) || a == '')))";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object>()
            {
                {"a",""},
                {"b",""},
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

        #endregion

        #region 扩展用例（原 ExpressionAdditionalTests）

        [TestMethod]
        public void Constructor_NullOrEmpty_ThrowsExpressionException()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression(null));
            Assert.ThrowsException<ExpressionException>(() => new Expression(""));
        }

        [TestMethod]
        public void Constructor_UndefinedFunction_ThrowsExpressionException()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("[NotARealFunction](1)"));
        }

        [TestMethod]
        public void Constructor_UnbalancedParenthesis_ThrowsExpressionException()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("(1 + 2"));
        }

        [TestMethod]
        public void Execute_InvalidPercentLiteral_ThrowsExpressionException()
        {
            var exp = new Expression("1 + a");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "bad%" } });
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void Execute_PercentLiteral_AsFraction_ViaVariable()
        {
            var exp = new Expression("2 * a");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "50%" } });
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void Execute_BooleanLiterals_AndOrNot()
        {
            var and = new Expression("true & false");
            and.LoadArgument();
            Assert.AreEqual(0d, and.Execute());

            var or = new Expression("true | false");
            or.LoadArgument();
            Assert.AreEqual(1d, or.Execute());

            var notTrue = new Expression("!true");
            notTrue.LoadArgument();
            Assert.AreEqual(0d, notTrue.Execute());

            var notFalse = new Expression("!false");
            notFalse.LoadArgument();
            Assert.AreEqual(1d, notFalse.Execute());
        }

        [TestMethod]
        public void Execute_Modulo_Operator()
        {
            var exp = new Expression("7 % 4");
            exp.LoadArgument();
            Assert.AreEqual(3d, exp.Execute());
        }

        [TestMethod]
        public void Execute_Relational_GreaterEqual_LessEqual()
        {
            var ge = new Expression("5 >= 5");
            ge.LoadArgument();
            Assert.AreEqual(1d, ge.Execute());

            var le = new Expression("3 <= 5");
            le.LoadArgument();
            Assert.AreEqual(1d, le.Execute());

            var geFalse = new Expression("2 >= 9");
            geFalse.LoadArgument();
            Assert.AreEqual(0d, geFalse.Execute());
        }

        [TestMethod]
        public void Execute_StringVariable_Equality()
        {
            var exp = new Expression("x == y");
            exp.LoadArgument(new Dictionary<string, object> { { "x", "a" }, { "y", "a" } });
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void Execute_StringVariable_UnEquals()
        {
            var exp = new Expression("x != y");
            exp.LoadArgument(new Dictionary<string, object> { { "x", "a" }, { "y", "b" } });
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void Execute_Date_UnEquals_And_GreaterEqual()
        {
            var ne = new Expression("'2024-01-01' != '2024-01-02'");
            ne.LoadArgument();
            Assert.AreEqual(1d, ne.Execute());

            var ge = new Expression("'2024-06-01' >= '2024-06-01'");
            ge.LoadArgument();
            Assert.AreEqual(1d, ge.Execute());
        }

        [TestMethod]
        public void Contains_WhenNotPresent_ReturnsZero()
        {
            var exp = new Expression("[CONTAINS]('hello','z')");
            exp.LoadArgument();
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void Excluding_WhenContains_ReturnsZero_WhenNotContains_ReturnsOne()
        {
            var contained = new Expression("[EXCLUDING]('hello','ell')");
            contained.LoadArgument();
            Assert.AreEqual(0d, contained.Execute());

            var notContained = new Expression("[EXCLUDING]('hello','xyz')");
            notContained.LoadArgument();
            Assert.AreEqual(1d, notContained.Execute());
        }

        [TestMethod]
        public void StartWith_EndWith_PositiveAndNegative()
        {
            var swOk = new Expression("[STARTWITH]('abc','ab')");
            swOk.LoadArgument();
            Assert.AreEqual(1d, swOk.Execute());

            var swNo = new Expression("[STARTWITH]('abc','bc')");
            swNo.LoadArgument();
            Assert.AreEqual(0d, swNo.Execute());

            var ewOk = new Expression("[ENDWITH]('abc','bc')");
            ewOk.LoadArgument();
            Assert.AreEqual(1d, ewOk.Execute());

            var ewNo = new Expression("[ENDWITH]('abc','ab')");
            ewNo.LoadArgument();
            Assert.AreEqual(0d, ewNo.Execute());
        }

        [TestMethod]
        public void Different_WhenSame_ReturnsZero_WhenDifferent_ReturnsOne()
        {
            var same = new Expression("[DIFFERENT]('x','x')");
            same.LoadArgument();
            Assert.AreEqual(0d, same.Execute());

            var diff = new Expression("[DIFFERENT]('x','y')");
            diff.LoadArgument();
            Assert.AreEqual(1d, diff.Execute());
        }

        [TestMethod]
        public void Equals_Function_WhenFalse_ReturnsZero()
        {
            var exp = new Expression("[EQUALS](1,2)");
            exp.LoadArgument();
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void IsNull_WhenNotNull_ReturnsZero()
        {
            var exp = new Expression("[ISNULL](a)");
            var dic = new Dictionary<string, object> { { "a", 0 } };
            exp.LoadArgument(dic);
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void NotIsNull_SimpleIdentifier_AndSelf_ParsesAndRuns()
        {
            var exp = new Expression("![ISNULL](a) & ![ISNULL](a)");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "x" } });
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void IsNull_DottedParameter_Parses()
        {
            var exp = new Expression("[ISNULL](data.VIRTUAL_PARTNER_NAME)");
            exp.LoadArgument(new Dictionary<string, object> { { "data.VIRTUAL_PARTNER_NAME", "p" } });
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void NotIsNull_DottedIdentifier_AndSelf_WhenHasValue_ReturnsOne()
        {
            const string key = "data.VIRTUAL_PARTNER_NAME";
            var expStr = "![ISNULL](" + key + ") & ![ISNULL](" + key + ")";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object> { { key, "PartnerA" } };
            exp.LoadArgument(dic);
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void NotIsNull_DottedIdentifier_AndSelf_WhenNull_ReturnsZero()
        {
            const string key = "data.VIRTUAL_PARTNER_NAME";
            var expStr = "![ISNULL](" + key + ") & ![ISNULL](" + key + ")";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object> { { key, null } };
            exp.LoadArgument(dic);
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void NotIsNull_DottedIdentifier_AndSelf_WhenEmptyString_ReturnsOne()
        {
            const string key = "data.VIRTUAL_PARTNER_NAME";
            var expStr = "![ISNULL](" + key + ") & ![ISNULL](" + key + ")";
            var exp = new Expression(expStr);
            var dic = new Dictionary<string, object> { { key, "" } };
            exp.LoadArgument(dic);
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void GetAllParams_IncludesDottedIdentifier_ForVirtualPartnerExpression()
        {
            const string key = "data.VIRTUAL_PARTNER_NAME";
            var expStr = "![ISNULL](" + key + ") & ![ISNULL](" + key + ")";
            var exp = new Expression(expStr);
            var keys = exp.GetAllParams().Select(p => p.Key).ToList();
            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(key, keys[0]);
            Assert.AreEqual(key, keys[1]);
        }

        private const string VirtualPartnerNoEdgeSpacesExpr =
            "![STARTWITH](data.VIRTUAL_PARTNER_NAME,' ') & ![ENDWITH](data.VIRTUAL_PARTNER_NAME,' ')";

        [TestMethod]
        public void VirtualPartnerName_NoLeadingOrTrailingSpace_WhenTrimmedOk_ReturnsOne()
        {
            var exp = new Expression(VirtualPartnerNoEdgeSpacesExpr);
            exp.LoadArgument(new Dictionary<string, object> { { "data.VIRTUAL_PARTNER_NAME", "PartnerA" } });
            Assert.AreEqual(1d, exp.Execute());

            exp = new Expression(VirtualPartnerNoEdgeSpacesExpr);
            exp.LoadArgument(new Dictionary<string, object> { { "data.VIRTUAL_PARTNER_NAME", "A B" } });
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void VirtualPartnerName_NoLeadingOrTrailingSpace_WhenLeadingSpace_ReturnsZero()
        {
            var exp = new Expression(VirtualPartnerNoEdgeSpacesExpr);
            exp.LoadArgument(new Dictionary<string, object> { { "data.VIRTUAL_PARTNER_NAME", " PartnerA" } });
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void VirtualPartnerName_NoLeadingOrTrailingSpace_WhenTrailingSpace_ReturnsZero()
        {
            var exp = new Expression(VirtualPartnerNoEdgeSpacesExpr);
            exp.LoadArgument(new Dictionary<string, object> { { "data.VIRTUAL_PARTNER_NAME", "PartnerA " } });
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void VirtualPartnerName_NoLeadingOrTrailingSpace_WhenBothEdgesSpace_ReturnsZero()
        {
            var exp = new Expression(VirtualPartnerNoEdgeSpacesExpr);
            exp.LoadArgument(new Dictionary<string, object> { { "data.VIRTUAL_PARTNER_NAME", " x " } });
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void Avg_OfThreeIntegers()
        {
            var exp = new Expression("[AVG](2,4,6)");
            exp.LoadArgument();
            Assert.AreEqual(4d, exp.Execute());
        }

        [TestMethod]
        public void Sum_WithVariableArguments()
        {
            var exp = new Expression("[SUM](x,y)");
            var dic = new Dictionary<string, object> { { "x", "2" }, { "y", "3" } };
            exp.LoadArgument(dic);
            Assert.AreEqual(5d, exp.Execute());
        }

        [TestMethod]
        public void EDate_AddMonth_And_AddYear()
        {
            var addMonth = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15',1,M),yyyyMMdd)");
            addMonth.LoadArgument();
            Assert.AreEqual("20240215", addMonth.Execute());

            var addYear = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15',1,Y),yyyyMMdd)");
            addYear.LoadArgument();
            Assert.AreEqual("20250115", addYear.Execute());
        }

        [TestMethod]
        public void TimeToString_FirstArgAsStringDate()
        {
            var exp = new Expression("[TIMETOSTRING]('2024-05-27',yyyyMMdd)");
            exp.LoadArgument();
            Assert.AreEqual("20240527", exp.Execute());
        }

        [TestMethod]
        public void FunctionName_IsCaseInsensitive()
        {
            var exp = new Expression("[sum](1,2,3)");
            exp.LoadArgument();
            Assert.AreEqual(6d, exp.Execute());
        }

        [TestMethod]
        public void GetAllParams_CollectsIdentifiers()
        {
            var exp = new Expression("a * b + [SUM](c,d)");
            var keys = exp.GetAllParams().Select(p => p.Key).ToList();
            CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "d" }, keys);
        }

        [TestMethod]
        public void Check_OnValidExpression_DoesNotThrow()
        {
            var exp = new Expression("1 + 2 * 3");
            exp.Check();
        }

        [TestMethod]
        public void Execute_ReturnType_Number_Date_String_And_Null()
        {
            var numeric = new Expression("1+2");
            numeric.LoadArgument();
            Assert.IsInstanceOfType(numeric.Execute(), typeof(double));

            var date = new Expression("'2024-01-01'");
            date.LoadArgument();
            Assert.IsInstanceOfType(date.Execute(), typeof(DateTime));

            var str = new Expression("'abc'");
            str.LoadArgument();
            Assert.IsInstanceOfType(str.Execute(), typeof(string));

            var nullValue = new Expression("x");
            nullValue.LoadArgument(new Dictionary<string, object> { { "x", null } });
            Assert.IsNull(nullValue.Execute());
        }

        [TestMethod]
        public void LoadArgument_StringDictionary_Overload_ReplacesValues()
        {
            var exp = new Expression("a + 1");
            var dic = new Dictionary<string, string> { { "a", "4" } };
#pragma warning disable CS0618
            exp.LoadArgument(dic);
#pragma warning restore CS0618
            Assert.AreEqual(5d, exp.Execute());
        }

        [TestMethod]
        public void Round_InvalidMode_WrappedAsExpressionException()
        {
            var exp = new Expression("[ROUND](1,0,99)");
            exp.LoadArgument();
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void Extensions_GetOperatorObj_ReturnsSymbol()
        {
            var attr = Operator.Multiply.GetOperatorObj();
            Assert.AreEqual("*", attr.Value);
        }

        [TestMethod]
        public void Extensions_DeepCopy_CopiesPublicProperties()
        {
            var original = new DeepCopySample { Id = 7, Name = "t" };
            var copy = original.DeepCopy();
            Assert.AreNotSame(original, copy);
            Assert.AreEqual(7, copy.Id);
            Assert.AreEqual("t", copy.Name);
        }

        [TestMethod]
        public void Expression_TrimsLeadingWhitespace()
        {
            var exp = new Expression("  1 + 1  ");
            exp.LoadArgument();
            Assert.AreEqual(2d, exp.Execute());
        }

        [TestMethod]
        public void Execute_DoubleDivision()
        {
            var exp = new Expression("10 / 4");
            exp.LoadArgument();
            Assert.AreEqual(2.5d, exp.Execute());
        }

        [TestMethod]
        public void Execute_LogicAnd_OnNumericOperands()
        {
            var exp = new Expression("1 & 0");
            exp.LoadArgument();
            Assert.AreEqual(0d, exp.Execute());
        }

        [TestMethod]
        public void Execute_Date_LessThanOrEqual()
        {
            var exp = new Expression("'2024-01-01' <= '2024-12-31'");
            exp.LoadArgument();
            Assert.AreEqual(1d, exp.Execute());
        }

        #region 解析与执行全覆盖补充（运算符、字面量、各函数与异常路径）

        private static double Run(string formula, Dictionary<string, object> args = null)
        {
            var exp = new Expression(formula);
            if (args != null)
                exp.LoadArgument(args);
            else
                exp.LoadArgument();
            return Convert.ToDouble(exp.Execute());
        }

        [TestMethod]
        public void Init_ReplacesDoublePipe_ToSingleOr()
        {
            Assert.AreEqual(1d, Run("false || true"));
            Assert.AreEqual(1d, Run("0 || 1"));
        }

        [TestMethod]
        public void Init_ReplacesDoubleAmpersand_ToSingleAnd()
        {
            Assert.AreEqual(0d, Run("true && false"));
        }

        [TestMethod]
        public void Relational_AlternateSpacing_SmallerEqual_GreaterEqual()
        {
            Assert.AreEqual(1d, Run("2 =< 5"));
            Assert.AreEqual(1d, Run("5 => 3"));
            Assert.AreEqual(0d, Run("2 => 9"));
        }

        [TestMethod]
        public void Numeric_GreaterThan_LessThan()
        {
            Assert.AreEqual(1d, Run("7 > 3"));
            Assert.AreEqual(0d, Run("1 > 2"));
            Assert.AreEqual(1d, Run("2 < 9"));
            Assert.AreEqual(0d, Run("5 < 1"));
        }

        [TestMethod]
        public void Date_GreaterThan_LessThan_And_UnEqualsSameDay()
        {
            Assert.AreEqual(1d, Run("'2024-06-10' > '2024-06-01'"));
            Assert.AreEqual(1d, Run("'2024-01-01' < '2024-12-31'"));
            Assert.AreEqual(0d, Run("'2024-01-01' != '2024-01-01'"));
        }

        [TestMethod]
        public void Date_GreaterEqual_LessEqual_FalseCases()
        {
            Assert.AreEqual(0d, Run("'2024-01-01' >= '2024-06-01'"));
            Assert.AreEqual(0d, Run("'2024-12-31' <= '2024-01-01'"));
        }

        [TestMethod]
        public void DoubleNot_OnBooleanLiteral_ViaParentheses()
        {
            Assert.AreEqual(1d, Run("(!(!true))"));
            Assert.AreEqual(0d, Run("(!(!false))"));
        }

        [TestMethod]
        public void Convert2ObjectValue_Literal_0_And_1_Keywords()
        {
            Assert.AreEqual(1d, Run("1==1"));
            Assert.AreEqual(1d, Run("0==0"));
        }

        [TestMethod]
        public void Sum_And_Avg_WithExpressionParameters()
        {
            Assert.AreEqual(6d, Run("[SUM](1+2,3)"));
            Assert.AreEqual(3.5d, Run("[AVG]([SUM](1,2),4)"));
        }

        [TestMethod]
        public void Equals_Function_WhenTrue_ReturnsOne()
        {
            Assert.AreEqual(1d, Run("[EQUALS]('x','x')"));
        }

        [TestMethod]
        public void Contains_FunctionName_Lowercase_Parses()
        {
            Assert.AreEqual(1d, Run("[contains]('abc','b')"));
        }

        [TestMethod]
        public void EDate_Units_Hours_Minutes_Seconds_Milliseconds_LowercaseY()
        {
            var h = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15 08:00:00',5,H),HH)");
            h.LoadArgument();
            Assert.AreEqual("13", h.Execute());

            var m = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15 10:00:00',45,m),mm)");
            m.LoadArgument();
            Assert.AreEqual("45", m.Execute());

            var s = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15 10:00:00',90,s),ss)");
            s.LoadArgument();
            Assert.AreEqual("30", s.Execute());

            var f = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15 10:00:00.000',500,F),fff)");
            f.LoadArgument();
            Assert.AreEqual("500", f.Execute());

            var y = new Expression("[TIMETOSTRING]([EDATE]('2024-01-15',-1,y),yyyyMMdd)");
            y.LoadArgument();
            Assert.AreEqual("20230115", y.Execute());
        }

        [TestMethod]
        public void EODate_Mode_Lowercase_E_EndOfMonth()
        {
            var exp = new Expression("[TIMETOSTRING]([EODATE]('2024-01-15',0,e),yyyyMMdd)");
            exp.LoadArgument();
            Assert.AreEqual("20240131", exp.Execute());
        }

        [TestMethod]
        public void TimeSpan_Subtract_WithTime_UsedBySeconds()
        {
            var exp = new Expression("[SECONDS]('2024-01-15 10:00:10'-'2024-01-15 10:00:00')");
            exp.LoadArgument();
            Assert.AreEqual(10d, exp.Execute());
        }

        [TestMethod]
        public void TimeToString_InvalidDate_ThrowsExpressionException()
        {
            var exp = new Expression("[TIMETOSTRING]('not-a-date',yyyyMMdd)");
            exp.LoadArgument();
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void EDate_InvalidFirstArgument_ThrowsExpressionException()
        {
            var exp = new Expression("[TIMETOSTRING]([EDATE]('bad',1,D),yyyyMMdd)");
            exp.LoadArgument();
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void Round_NonNumericFirstArg_ThrowsExpressionException()
        {
            var exp = new Expression("[ROUND](x,1,0)");
            exp.LoadArgument(new Dictionary<string, object> { { "x", "abc" } });
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void Division_ByZero_ReturnsInfinity()
        {
            var v = Run("1/0");
            Assert.IsTrue(double.IsInfinity(v));
        }

        [TestMethod]
        public void Seconds_WithNonTimeSpan_ThrowsExpressionException()
        {
            var exp = new Expression("[SECONDS](1)");
            exp.LoadArgument();
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void AllRegisteredFunctionNames_SmokeParse()
        {
            void Ok(string s) => new Expression(s);

            Ok("[SUM](0)");
            Ok("[AVG](1,3)");
            Ok("[CONTAINS]('a','a')");
            Ok("[EXCLUDING]('a','b')");
            Ok("[EQUALS](1,1)");
            Ok("[STARTWITH]('a','a')");
            Ok("[ENDWITH]('a','a')");
            Ok("[DIFFERENT]('a','b')");
            Ok("[ROUND](1,0,0)");
            Ok("[EDATE]('2024-01-01',0,D)");
            Ok("[EODATE]('2024-01-01',0,S)");
            Ok("[NOWTIME]()");
            Ok("[TIMETOSTRING]('2024-01-01',yyyy)");
            Ok("[DAYS]('2024-01-02'-'2024-01-01')");
            Ok("[HOURS]('2024-01-02'-'2024-01-01')");
            Ok("[MINUTES]('2024-01-02'-'2024-01-01')");
            Ok("[SECONDS]('2024-01-02'-'2024-01-01')");
            Ok("[MILLSECONDS]('2024-01-02'-'2024-01-01')");
            Ok("[ISNULL](a)");
        }

        [TestMethod]
        public void OperatorAttributes_CoverMajorSymbols()
        {
            Assert.AreEqual("&", Operator.And.GetOperatorObj().Value);
            Assert.AreEqual("|", Operator.Or.GetOperatorObj().Value);
            Assert.AreEqual("!", Operator.Not.GetOperatorObj().Value);
            Assert.AreEqual("+", Operator.Plus.GetOperatorObj().Value);
            Assert.AreEqual("-", Operator.Subtract.GetOperatorObj().Value);
            Assert.AreEqual("/", Operator.Divide.GetOperatorObj().Value);
            Assert.AreEqual("%", Operator.Mod.GetOperatorObj().Value);
            Assert.AreEqual(">", Operator.GreaterThan.GetOperatorObj().Value);
            Assert.AreEqual("<", Operator.LessThan.GetOperatorObj().Value);
            Assert.AreEqual("=", Operator.Equals.GetOperatorObj().Value);
            Assert.AreEqual("!=", Operator.UnEquals.GetOperatorObj().Value);
            Assert.AreEqual(">=", Operator.GreaterThanOrEquals.GetOperatorObj().Value);
            Assert.AreEqual("<=", Operator.LessThanOrEquals.GetOperatorObj().Value);
            Assert.AreEqual("-", Operator.Negative.GetOperatorObj().Value);
        }

        [TestMethod]
        public void Parenthesized_Subexpression_WithNotAndIsNull()
        {
            var exp = new Expression("(!([ISNULL](a)))");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "v" } });
            Assert.AreEqual(1d, exp.Execute());
        }

        [TestMethod]
        public void StringComparison_UnEquals_DifferentLiterals_ReturnsOne()
        {
            Assert.AreEqual(1d, Run("'x' != 'y'"));
        }

        #endregion

        [XmlRoot("Sample")]
        public class DeepCopySample
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}
