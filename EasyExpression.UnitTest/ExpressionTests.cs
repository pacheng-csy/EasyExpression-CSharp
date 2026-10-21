using EasyExpression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public void Concat_Literals_ReturnsJoinedString()
        {
            var exp = new Expression("[CONCAT]('ab','cd','ef')");
            exp.LoadArgument();
            Assert.AreEqual("abcdef", exp.Execute());
        }

        [TestMethod]
        public void Concat_ArgumentsFromDictionary_ReturnsJoinedString()
        {
            var exp = new Expression("[CONCAT](a,b)");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "x" }, { "b", "y" } });
            Assert.AreEqual("xy", exp.Execute());
        }

        [TestMethod]
        public void Concat_NestedFunctionAndNumericValue_ReturnsJoinedString()
        {
            var exp = new Expression("[CONCAT]('d:',[TIMETOSTRING]('2024-05-27',yyyyMMdd),1)");
            exp.LoadArgument();
            Assert.AreEqual("d:202405271", exp.Execute());
        }

        [TestMethod]
        public void Concat_NullArgument_TreatedAsEmptyString()
        {
            var withLiteral = new Expression("[CONCAT]('a',b)");
            withLiteral.LoadArgument(new Dictionary<string, object> { { "b", null } });
            Assert.AreEqual("a", withLiteral.Execute());

            var allParams = new Expression("[CONCAT](a,b)");
            allParams.LoadArgument(new Dictionary<string, object> { { "a", null }, { "b", "y" } });
            Assert.AreEqual("y", allParams.Execute());
        }

        [TestMethod]
        public void Concat_FunctionName_IsCaseInsensitive()
        {
            var exp = new Expression("[concat]('a','b')");
            exp.LoadArgument();
            Assert.AreEqual("ab", exp.Execute());
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
            Ok("[CONCAT]('a','b')");
            Ok("[IF](1,'a','b')");
            Ok("[CASE](1,1,'a','b')");
            Ok("[LEN]('abc')");
            Ok("[SUBSTR]('abc',2)");
            Ok("[REPLACE]('abc','b','x')");
            Ok("[UPPER]('abc')");
            Ok("[LOWER]('abc')");
            Ok("[TRIM](' abc ')");
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

        #region 回归测试（阶段0/1/2 修复）

        private static object EvalValue(string formula)
        {
            var exp = new Expression(formula);
            exp.LoadArgument();
            return exp.Execute();
        }

        private static string EvalString(string formula)
        {
            return (string)EvalValue(formula);
        }

        [TestMethod]
        public void Regression_WhitespaceExpression_ThrowsExpressionException()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("   "));
        }

        [TestMethod]
        public void Regression_SingleValueInParentheses_Evaluates()
        {
            Assert.AreEqual(2d, Run("(2)"));
            Assert.AreEqual(-2d, Run("-(2)"));
            Assert.AreEqual(2d, Run("[SUM]((2))"));
            Assert.AreEqual(1d, Run("-(2)+3"));
            Assert.AreEqual(3d, Run("(2)+1"));
        }

        [TestMethod]
        public void Regression_IncompleteExpression_ThrowsExpressionException()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("+"));
            Assert.ThrowsException<ExpressionException>(() => new Expression("1 +"));
            Assert.ThrowsException<ExpressionException>(() => new Expression("[SUM](1,2"));
        }

        [TestMethod]
        public void Regression_StringLiterals_AreNotRewritten()
        {
            Assert.AreEqual("a==b", EvalString("'a==b'"));
            Assert.AreEqual("x||y", EvalString("'x||y'"));
            Assert.AreEqual("a&&b", EvalString("'a&&b'"));
            Assert.AreEqual("a,b", EvalString("[CONCAT]('a,b')"));
        }

        [TestMethod]
        public void Regression_DoubleCharOperators_RemainSupported()
        {
            // 兼容两种写法：&& / || / == 与 & / | / =
            Assert.AreEqual(1d, Run("true && (1 == 1)"));
            Assert.AreEqual(1d, Run("false || (1 == 1)"));
            Assert.AreEqual(1d, Run("1 & 1"));
            Assert.AreEqual(1d, Run("0 | 1"));
            Assert.AreEqual(1d, Run("2 =< 5"));
            Assert.AreEqual(1d, Run("5 => 3"));
            Assert.AreEqual(1d, Run("[EQUALS](a,'a')", new Dictionary<string, object> { { "a", "a" } }));
        }

        [TestMethod]
        public void Regression_UnterminatedString_ThrowsExpressionException()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("a'"));
        }

        [TestMethod]
        public void Regression_StringLiteral_Backslashes_ArePreserved()
        {
            Assert.AreEqual(@"C:\temp\file.txt", EvalString(@"'C:\temp\file.txt'"));
            Assert.AreEqual("It's", EvalString(@"'It\'s'"));
            Assert.AreEqual(@"a\b", EvalString(@"'a\\b'"));
        }

        [TestMethod]
        public void Regression_VariableNames_AreNotPartiallyReplaced()
        {
            var concat = new Expression("[CONCAT](a,ab)");
            concat.LoadArgument(new Dictionary<string, object> { { "a", "1" } });
            Assert.AreEqual("1ab", concat.Execute());

            var equals = new Expression("[EQUALS](a,'a')");
            equals.LoadArgument(new Dictionary<string, object> { { "a", "X" } });
            Assert.AreEqual(0d, equals.Execute());
        }

        [TestMethod]
        public void Regression_LoadArgument_IsIdempotent_ForFunctions()
        {
            var exp = new Expression("[CONCAT](a,'a')");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "X" } });
            Assert.AreEqual("Xa", exp.Execute());

            exp.LoadArgument(new Dictionary<string, object> { { "a", "Y" } });
            Assert.AreEqual("Ya", exp.Execute());
        }

        [TestMethod]
        public void Regression_MissingVariable_ThrowsFriendlyExpressionException()
        {
            var exp = new Expression("a + 1");
            exp.LoadArgument(new Dictionary<string, object>());
            var ex = Assert.ThrowsException<ExpressionException>(() => exp.Execute());
            StringAssert.Contains(ex.Message, "a");
            StringAssert.Contains(ex.Message, "未提供实参");
        }

        [TestMethod]
        public void Regression_EODate_December_EndOfMonth_DoesNotThrow()
        {
            Assert.AreEqual("20241231", EvalString("[TIMETOSTRING]([EODATE]('2024-12-15',0,E),yyyyMMdd)"));
        }

        [TestMethod]
        public void Regression_StringLiteralComparison_ComparesText()
        {
            Assert.AreEqual(1d, Run("'abc' = 'abc'"));
            Assert.AreEqual(1d, Run("'abc' != 'abd'"));
        }

        [TestMethod]
        public void Regression_StringFunctions_AreNullSafe()
        {
            var nullArgs = new Dictionary<string, object> { { "a", null } };
            Assert.AreEqual(0d, Run("[CONTAINS](a,'x')", nullArgs));
            Assert.AreEqual(1d, Run("[EXCLUDING](a,'x')", nullArgs));
            Assert.AreEqual(0d, Run("[STARTWITH](a,'x')", nullArgs));
            Assert.AreEqual(0d, Run("[ENDWITH](a,'x')", nullArgs));
            Assert.AreEqual(1d, Run("[DIFFERENT](a,'x')", nullArgs));
        }

        [TestMethod]
        public void Regression_ExecuteWithDictionary_Overload_BindsAndEvaluates()
        {
            var exp = new Expression("a * 2");
            Assert.AreEqual(8d, exp.Execute(new Dictionary<string, object> { { "a", 4 } }));
        }

        [TestMethod]
        public void Regression_ExpressionOptions_Culture_IsUsedDuringBinding()
        {
            var exp = new Expression("a + 1", new ExpressionOptions
            {
                Culture = new System.Globalization.CultureInfo("de-DE")
            });
            exp.LoadArgument(new Dictionary<string, object> { { "a", 1.5 } });
            Assert.AreEqual(2.5d, exp.Execute());
        }

        [TestMethod]
        public void Regression_CustomFunction_RegisterAndUnregister()
        {
            Assert.IsTrue(FunctionRegistry.Register("TWICE", values => Convert.ToDouble(values[0]) * 2, 1, 1));
            try
            {
                Assert.IsTrue(FunctionRegistry.Contains("twice"));
                Assert.AreEqual(6d, Run("[TWICE](3)"));
                Assert.AreEqual(6d, Run("[twice](3)"));
            }
            finally
            {
                Assert.IsTrue(FunctionRegistry.Unregister("TWICE"));
            }

            Assert.IsFalse(FunctionRegistry.Contains("TWICE"));
            Assert.ThrowsException<ExpressionException>(() => new Expression("[TWICE](3)"));
        }

        [TestMethod]
        public void Regression_CustomFunction_Arity_IsValidated()
        {
            FunctionRegistry.Register("ONEARG", values => values[0], 1, 1);
            try
            {
                var exp = new Expression("[ONEARG](1,2)");
                exp.LoadArgument();
                Assert.ThrowsException<ExpressionException>(() => exp.Execute());
            }
            finally
            {
                FunctionRegistry.Unregister("ONEARG");
            }
        }

        [TestMethod]
        public void Regression_BoundObjectTypes_ArePreserved()
        {
            var timeSpan = new Expression("[DAYS](a)");
            timeSpan.LoadArgument(new Dictionary<string, object> { { "a", TimeSpan.FromDays(3) } });
            Assert.AreEqual(3d, timeSpan.Execute());

            var boolean = new Expression("!a");
            boolean.LoadArgument(new Dictionary<string, object> { { "a", false } });
            Assert.AreEqual(1d, boolean.Execute());

            var date = new Expression("a = '2024-01-01'");
            date.LoadArgument(new Dictionary<string, object> { { "a", new DateTime(2024, 1, 1) } });
            Assert.AreEqual(1d, date.Execute());
        }

        [TestMethod]
        public void Regression_ExecuteWithoutLoadArgument_BindsEmptyArguments()
        {
            Assert.AreEqual(3d, new Expression("1+2").Execute());
            Assert.AreEqual("abc", new Expression("'abc'").Execute());
            Assert.AreEqual(6d, new Expression("[SUM](1,2,3)").Execute());
        }

        [TestMethod]
        public void NewFeature_If_IsLazy()
        {
            Assert.AreEqual(10d, Run("[IF](1,10,20)"));
            Assert.AreEqual(20d, Run("[IF](0,10,20)"));
            Assert.AreEqual(5d, Run("[IF](1,5,x+1)"));
            Assert.AreEqual(0d, Run("[IF](0,x+1,0)"));
        }

        [TestMethod]
        public void NewFeature_Case_MatchesAndFallsBackToDefault()
        {
            Assert.AreEqual("b", EvalString("[CASE](2,1,'a',2,'b','c')"));
            Assert.AreEqual("c", EvalString("[CASE](9,1,'a',2,'b','c')"));
            Assert.AreEqual("a", EvalString("[CASE](1,1,'a',x+1)"));
            Assert.IsNull(EvalValue("[CASE](9,1,'a')"));
        }

        [TestMethod]
        public void NewFeature_StringFunctions()
        {
            Assert.AreEqual(3d, Run("[LEN]('abc')"));
            Assert.AreEqual(0d, Run("[LEN](a)", new Dictionary<string, object> { { "a", null } }));
            Assert.AreEqual("bcd", EvalString("[SUBSTR]('abcdef',2,3)"));
            Assert.AreEqual("cdef", EvalString("[SUBSTR]('abcdef',3)"));
            Assert.AreEqual(string.Empty, EvalString("[SUBSTR]('abcdef',99)"));
            Assert.AreEqual("a_b", EvalString("[REPLACE]('a-b','-','_')"));
            Assert.AreEqual("ABC", EvalString("[UPPER]('abc')"));
            Assert.AreEqual("abc", EvalString("[LOWER]('ABC')"));
            Assert.AreEqual("abc", EvalString("[TRIM]('  abc  ')"));
        }

        [TestMethod]
        public void NewFeature_DivisionByZeroBehavior()
        {
            Assert.IsTrue(double.IsInfinity(Convert.ToDouble(new Expression("1/0").Execute())));

            var zero = new Expression("1/0", new ExpressionOptions
            {
                DivisionByZero = DivisionByZeroBehavior.Zero
            });
            Assert.AreEqual(0d, zero.Execute());

            var throws = new Expression("1/0", new ExpressionOptions
            {
                DivisionByZero = DivisionByZeroBehavior.Throw
            });
            Assert.ThrowsException<ExpressionException>(() => throws.Execute());
        }

        [TestMethod]
        public void NewFeature_FunctionWhitelist()
        {
            var options = new ExpressionOptions
            {
                FunctionWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SUM" }
            };

            var allowed = new Expression("[SUM](1,2)", options);
            allowed.LoadArgument();
            Assert.AreEqual(3d, allowed.Execute());

            Assert.ThrowsException<ExpressionException>(() => new Expression("[AVG](1,2)", options));
            Assert.ThrowsException<ExpressionException>(() => new Expression("[IF](1,1,2)", options));
        }

        [TestMethod]
        public void NewFeature_MultipleCultures()
        {
            foreach (var name in new[] { "zh-CN", "en-US" })
            {
                var options = new ExpressionOptions { Culture = new CultureInfo(name) };

                var date = new Expression("'2024-01-15' > '2024-01-01'", options);
                date.LoadArgument();
                Assert.AreEqual(1d, date.Execute());

                var number = new Expression("a + 1", options);
                number.LoadArgument(new Dictionary<string, object> { { "a", 1.5 } });
                Assert.AreEqual(2.5d, number.Execute());
            }

            var german = new Expression("a + 1", new ExpressionOptions { Culture = new CultureInfo("de-DE") });
            german.LoadArgument(new Dictionary<string, object> { { "a", 1.5 } });
            Assert.AreEqual(2.5d, german.Execute());
        }

        [TestMethod]
        public void Fuzz_RandomExpressions_NeverThrowUnexpectedExceptions()
        {
            var random = new Random(20261021);
            var atoms = new[] { "0", "1", "2", "3", "'a'", "'b'", "true", "false" };
            var operators = new[] { "+", "-", "*", "/", "%", "&", "|", "=", "!=", ">", "<", ">=", "<=" };
            var functions = new[]
            {
                "[SUM](1,2)", "[AVG](1,2,3)", "[CONCAT]('a','b')",
                "[IF](1,2,3)", "[CASE](1,1,'a','b')", "[LEN]('abc')"
            };

            for (var i = 0; i < 400; i++)
            {
                var text = BuildRandomExpression(random, atoms, operators, functions, 0);
                try
                {
                    var exp = new Expression(text);
                    exp.LoadArgument(new Dictionary<string, object>());
                    exp.Execute();
                }
                catch (ExpressionException)
                {
                }
                catch (Exception ex)
                {
                    Assert.Fail($"随机表达式 '{text}' 抛出非 ExpressionException: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        [TestMethod]
        public void Fuzz_MalformedCorpus_ThrowsExpressionExceptionOnly()
        {
            var corpus = new[]
            {
                "", "   ", "(", ")", "[", "]", "[SUM](", "[SUM](1,2", "1 +", "* 2",
                "a'", "'a", "1 == = 2", "[NOPE](1)", "()", "1 2", "&", "|", "!", "~",
                "1..2", "a b", "1,,2", "[CONCAT](,)", "'unterminated", "[IF](1,2)", "[CASE]()"
            };

            foreach (var text in corpus)
            {
                try
                {
                    var exp = new Expression(text);
                    exp.LoadArgument(new Dictionary<string, object>());
                    exp.Execute();
                }
                catch (ExpressionException)
                {
                }
                catch (Exception ex)
                {
                    Assert.Fail($"表达式 '{text}' 抛出非 ExpressionException: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static string BuildRandomExpression(
            Random random,
            string[] atoms,
            string[] operators,
            string[] functions,
            int depth)
        {
            if (depth >= 3 || random.Next(4) == 0)
            {
                return random.Next(3) == 0
                    ? functions[random.Next(functions.Length)]
                    : atoms[random.Next(atoms.Length)];
            }

            switch (random.Next(4))
            {
                case 0:
                    return "(" + BuildRandomExpression(random, atoms, operators, functions, depth + 1) + ")";
                case 1:
                    return "!" + BuildRandomExpression(random, atoms, operators, functions, depth + 1);
                case 2:
                    return "-" + BuildRandomExpression(random, atoms, operators, functions, depth + 1);
                default:
                    return BuildRandomExpression(random, atoms, operators, functions, depth + 1)
                           + " " + operators[random.Next(operators.Length)] + " "
                           + BuildRandomExpression(random, atoms, operators, functions, depth + 1);
            }
        }

        #endregion

        #region 场景补全（公共 API、边界输入、防御性分支）

        private static object EvalWithArgs(string formula, Dictionary<string, object> args)
        {
            var exp = new Expression(formula);
            exp.LoadArgument(args);
            return exp.Execute();
        }

        private static string EvalStringWithArgs(string formula, Dictionary<string, object> args)
        {
            return (string)EvalWithArgs(formula, args);
        }

        private static object EvalWithOptions(string formula, ExpressionOptions options)
        {
            var exp = new Expression(formula, options);
            exp.LoadArgument();
            return exp.Execute();
        }

        private static double RunWithOptions(string formula, ExpressionOptions options)
        {
            return Convert.ToDouble(EvalWithOptions(formula, options));
        }

        [TestMethod]
        public void Api_LoadArgument_ReturnsBoundParameters()
        {
            var exp = new Expression("a + b + 1 + [SUM](c)");
            var bound = exp.LoadArgument(new Dictionary<string, object>
            {
                { "a", 1 },
                { "b", "2" },
                { "c", 3 },
            });

            Assert.AreEqual(3, bound.Count);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, bound.Select(x => x.Key).ToArray());
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, bound.Select(x => x.Value).ToArray());
            Assert.AreEqual(7d, exp.Execute());
        }

        [TestMethod]
        public void Api_LoadArgument_NullDictionary_BehavesAsEmpty()
        {
            var exp = new Expression("1 + 1");
            var bound = exp.LoadArgument((Dictionary<string, object>)null);
            Assert.AreEqual(0, bound.Count);
            Assert.AreEqual(2d, exp.Execute());
        }

        [TestMethod]
        public void Api_LoadArgument_WithoutDictionary_UsesVariableNameAsText()
        {
            var exp = new Expression("a");
            exp.LoadArgument();
            Assert.AreEqual("a", exp.Execute());

            var comparison = new Expression("a = 'a'");
            comparison.LoadArgument();
            Assert.AreEqual(1d, comparison.Execute());
        }

        [TestMethod]
        public void Api_Execute_WithNullDictionary_AndRepeatedly()
        {
            var literal = new Expression("1 + 1");
            Assert.AreEqual(2d, literal.Execute((Dictionary<string, object>)null));

            var exp = new Expression("a * 2");
            Assert.AreEqual(8d, exp.Execute(new Dictionary<string, object> { { "a", 4 } }));
            Assert.AreEqual(8d, exp.Execute());
            Assert.AreEqual(10d, exp.Execute(new Dictionary<string, object> { { "a", 5 } }));
        }

        [TestMethod]
        public void Api_PublicProperties_ArePopulatedAfterParse()
        {
            var exp = new Expression("a + 1");
            Assert.IsTrue(exp.Status);
            Assert.AreEqual(ElementType.Expression, exp.ElementType);
            Assert.AreEqual("a + 1", exp.SourceExpressionString);
            Assert.AreEqual("a + 1", exp.DataString);
            Assert.AreEqual(1, exp.Operators.Count);
            Assert.AreEqual(Operator.Plus, exp.Operators[0]);
            Assert.AreEqual(2, exp.ExpressionChildren.Count);
            Assert.AreEqual("a", exp.ExpressionChildren[0].DataString);
            Assert.AreEqual(ElementType.Data, exp.ExpressionChildren[0].ElementType);

            var function = new Expression("[SUM](1,2)");
            Assert.AreEqual(ElementType.Function, function.ElementType);
            Assert.AreEqual(FunctionType.Sum, function.FunctionType);
            Assert.AreEqual("Sum", function.FunctionName);
            Assert.AreEqual(2, function.ExpressionChildren.Count);
        }

        [TestMethod]
        public void Api_ErrorMessage_Alias_RoundTrips()
        {
            var exp = new Expression("1 + 1");
            Assert.IsNull(exp.ErrorMessage);

            exp.ErrorMessage = "first";
            Assert.AreEqual("first", exp.ErrorMessgage);

            exp.ErrorMessgage = "second";
            Assert.AreEqual("second", exp.ErrorMessage);
        }

        [TestMethod]
        public void Api_GetAllParams_ReportsTypes_AndSkipsLiterals()
        {
            var exp = new Expression("[SUM](a,b) + c + 'lit' + 1");
            var parameters = exp.GetAllParams();

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, parameters.Select(x => x.Key).ToArray());
            CollectionAssert.AreEqual(
                new[] { ElementType.Function, ElementType.Function, ElementType.Data },
                parameters.Select(x => x.Value).ToArray());
        }

        [TestMethod]
        public void Api_GetAllParams_HandlesMissingChildren()
        {
            var function = new Expression("[SUM](1,2)");
            function.ExpressionChildren = null;
            Assert.AreEqual(0, function.GetAllParams().Count);
        }

        [TestMethod]
        public void Api_Check_InvalidTree_Throws()
        {
            var exp = new Expression("1 + 2");
            exp.ExpressionChildren.Clear();
            Assert.ThrowsException<ExpressionException>(() => exp.Check());

            var unary = new Expression("!1");
            unary.Check();

            var placeholder = new Expression("1 + 2");
            placeholder.ElementType = ElementType.Placeholder;
            placeholder.Operators.Clear();
            placeholder.ExpressionChildren.Clear();
            Assert.ThrowsException<ExpressionException>(() => placeholder.Check());

            var leaf = new Expression("1");
            leaf.ExpressionChildren = null;
            leaf.Check();
        }

        [TestMethod]
        public void Api_NullOptions_UsesDefaults()
        {
            Assert.AreEqual(1d, new Expression("1", (ExpressionOptions)null).Execute());

            var options = new ExpressionOptions();
            Assert.AreEqual(CultureInfo.CurrentCulture, options.Culture);
            Assert.AreEqual(DivisionByZeroBehavior.Infinity, options.DivisionByZero);
            Assert.IsNull(options.FunctionWhitelist);
        }

        [TestMethod]
        public void Lexer_DoubleQuotedStrings_AndEscapes()
        {
            Assert.AreEqual("abc", EvalString("\"abc\""));
            Assert.AreEqual("a\"b", EvalString("\"a\\\"b\""));
            Assert.AreEqual("It's", EvalString("\"It's\""));
            Assert.AreEqual("a>b,c", EvalString("'a>b,c'"));
        }

        [TestMethod]
        public void Lexer_UnterminatedEscape_Throws()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("'abc\\"));
        }

        [TestMethod]
        public void Lexer_NumbersWhitespaceAndIdentifiers()
        {
            Assert.AreEqual(1000d, Run("1e3"));
            Assert.AreEqual(3d, Run("1 +\n\t2"));
            Assert.AreEqual(2d, Run("变量 + 1", new Dictionary<string, object> { { "变量", 1 } }));
            Assert.AreEqual(2d, Run("v_1.x + 1", new Dictionary<string, object> { { "v_1.x", 1 } }));
            Assert.AreEqual(1d, Run("1 <\n= 2"));
            Assert.AreEqual(3d, Run("1--2"));
            Assert.AreEqual(1d, Run("!!1"));
        }

        [TestMethod]
        public void Lexer_UnaryPlus_IsRejected()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("+1"));
        }

        [TestMethod]
        public void Lexer_PercentText_RequiresQuotes()
        {
            Assert.AreEqual(0.5d, Run("'50%'"));
            Assert.ThrowsException<ExpressionException>(() => new Expression("50%"));
        }

        [TestMethod]
        public void Parser_DeepNesting_IsLimited()
        {
            var deep = new string('(', 300) + "1" + new string(')', 300);
            Assert.ThrowsException<ExpressionException>(() => new Expression(deep));

            var allowed = new string('(', 50) + "1" + new string(')', 50);
            Assert.AreEqual(1d, Run(allowed));
        }

        [TestMethod]
        public void Parser_LongOperatorChain_Evaluates()
        {
            var formula = string.Join("+", Enumerable.Repeat("1", 300));
            Assert.AreEqual(300d, Run(formula));
        }

        [TestMethod]
        public void Evaluator_MalformedTree_ThrowsExpressionException()
        {
            var missingChildren = new Expression("1 + 2");
            missingChildren.ExpressionChildren.Clear();
            Assert.ThrowsException<ExpressionException>(() => missingChildren.Execute());

            var missingOperators = new Expression("1 + 2");
            missingOperators.Operators.Clear();
            Assert.ThrowsException<ExpressionException>(() => missingOperators.Execute());

            var emptyNode = new Expression("1 + 2");
            emptyNode.Operators.Clear();
            emptyNode.ExpressionChildren.Clear();
            Assert.ThrowsException<ExpressionException>(() => emptyNode.Execute());

            var singleChild = new Expression("1 + 2");
            singleChild.Operators.Clear();
            singleChild.ExpressionChildren.RemoveAt(1);
            Assert.AreEqual(1d, singleChild.Execute());

            var extraOperator = new Expression("1 + 2");
            extraOperator.Operators.Add(Operator.Multiply);
            Assert.ThrowsException<ExpressionException>(() => extraOperator.Execute());

            var unknownUnary = new Expression("!1");
            unknownUnary.Operators[0] = (Operator)999;
            Assert.ThrowsException<ExpressionException>(() => unknownUnary.Execute());

            var unknownBinary = new Expression("1 + 2");
            unknownBinary.Operators[0] = (Operator)999;
            Assert.ThrowsException<ExpressionException>(() => unknownBinary.Execute());

            var unknownNode = new Expression("1 + 2");
            unknownNode.ElementType = (ElementType)999;
            Assert.ThrowsException<ExpressionException>(() => unknownNode.Execute());
        }

        [TestMethod]
        public void Evaluator_RealityString_IsUsedWhenNotBound()
        {
            var exp = new Expression("x");
            exp.LoadArgument(new Dictionary<string, object>());
            exp.RealityString = "5";
            Assert.AreEqual(5d, exp.Execute());
        }

        [TestMethod]
        public void Evaluator_TimeSpan_ComparisonAndEquality()
        {
            Assert.AreEqual(1d, Run("('2024-10-15'-'2024-10-10') > ('2024-10-14'-'2024-10-10')"));
            Assert.AreEqual(1d, Run("('2024-10-15'-'2024-10-10') = ('2024-10-15'-'2024-10-10')"));
            Assert.AreEqual(1d, Run("('2024-10-15'-'2024-10-10') != ('2024-10-14'-'2024-10-10')"));
        }

        [TestMethod]
        public void Evaluator_NullAndMixedType_Comparison()
        {
            var bothNull = new Dictionary<string, object> { { "x", null }, { "y", null } };
            Assert.AreEqual(1d, Run("x = y", bothNull));
            Assert.AreEqual(0d, Run("x != y", bothNull));
            Assert.AreEqual(0d, Run("x = 1", new Dictionary<string, object> { { "x", null } }));
            Assert.AreEqual(0d, Run("1 = 'a'"));
            Assert.AreEqual(1d, Run("1 != 'a'"));
            Assert.AreEqual(0d, Run("'A' = 'a'"));
        }

        [TestMethod]
        public void Evaluator_NumericConversions_AndTextFallback()
        {
            Assert.AreEqual(11d, Run("a + 1", new Dictionary<string, object> { { "a", 10 } }));
            Assert.AreEqual(11d, Run("a + 1", new Dictionary<string, object> { { "a", 10L } }));
            Assert.AreEqual(11d, Run("a + 1", new Dictionary<string, object> { { "a", (short)10 } }));
            Assert.AreEqual(11d, Run("a + 1", new Dictionary<string, object> { { "a", (byte)10 } }));
            Assert.AreEqual(11.5d, Run("a + 1", new Dictionary<string, object> { { "a", 10.5f } }));
            Assert.AreEqual(12.5d, Run("a + 1", new Dictionary<string, object> { { "a", 11.5m } }));

            var custom = new Expression("a + 1");
            custom.LoadArgument(new Dictionary<string, object> { { "a", new TextLike("41") } });
            Assert.AreEqual(42d, custom.Execute());
        }

        [TestMethod]
        public void Evaluator_NullAndNonNumericOperands_ThrowFriendlyErrors()
        {
            var nullValue = new Expression("a + 1");
            nullValue.LoadArgument(new Dictionary<string, object> { { "a", null } });
            var nullEx = Assert.ThrowsException<ExpressionException>(() => nullValue.Execute());
            StringAssert.Contains(nullEx.Message, "值为空");

            var date = new Expression("'2024-01-01' + 1");
            Assert.ThrowsException<ExpressionException>(() => date.Execute());

            var timeSpan = new Expression("('2024-10-15'-'2024-10-10') + 1");
            Assert.ThrowsException<ExpressionException>(() => timeSpan.Execute());
        }

        [TestMethod]
        public void Evaluator_UnboundVariable_ThrowsThenCanBeRebound()
        {
            var exp = new Expression("a + 1");
            exp.LoadArgument(new Dictionary<string, object>());
            var ex = Assert.ThrowsException<ExpressionException>(() => exp.Execute());
            StringAssert.Contains(ex.Message, "未提供实参");

            exp.LoadArgument(new Dictionary<string, object> { { "a", 1 } });
            Assert.AreEqual(2d, exp.Execute());
        }

        [TestMethod]
        public void Functions_AggregateEdgeCases()
        {
            Assert.AreEqual(0d, Run("[SUM]()"));
            Assert.AreEqual(string.Empty, EvalString("[CONCAT]()"));
            Assert.AreEqual(1d, Run("[ISNULL]()"));
            Assert.ThrowsException<ExpressionException>(() => new Expression("[AVG]()").Execute());
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("[SUM]([CONCAT]('abc'))").Execute());

            Assert.AreEqual(0d, Run("[SUM](a)", new Dictionary<string, object> { { "a", "" } }));
            Assert.AreEqual(0d, Run("[AVG](a,b)", new Dictionary<string, object> { { "a", "" }, { "b", "" } }));
        }

        [TestMethod]
        public void Functions_StringEdgeCases()
        {
            Assert.AreEqual("abc", EvalString("[SUBSTR]('abc')"));
            Assert.AreEqual("abc", EvalString("[SUBSTR]('abc',0)"));
            Assert.AreEqual("abc", EvalString("[SUBSTR]('abc',-5)"));
            Assert.AreEqual(string.Empty, EvalString("[SUBSTR]('abc',2,0)"));
            Assert.AreEqual(string.Empty, EvalString("[SUBSTR]('abc',2,-1)"));
            Assert.AreEqual("bc", EvalString("[SUBSTR]('abc',2,99)"));
            Assert.AreEqual("abc", EvalString("[REPLACE]('abc','','x')"));

            var nullArgs = new Dictionary<string, object> { { "a", null } };
            Assert.AreEqual(string.Empty, EvalStringWithArgs("[SUBSTR](a,1)", nullArgs));
            Assert.AreEqual(string.Empty, EvalStringWithArgs("[REPLACE](a,'x','y')", nullArgs));
            Assert.AreEqual(string.Empty, EvalStringWithArgs("[UPPER](a)", nullArgs));
            Assert.AreEqual(string.Empty, EvalStringWithArgs("[LOWER](a)", nullArgs));
            Assert.AreEqual(string.Empty, EvalStringWithArgs("[TRIM](a)", nullArgs));
        }

        [TestMethod]
        public void Functions_Round_StringInput_AndArity()
        {
            Assert.AreEqual(3.1d, Run("[ROUND]([CONCAT]('3.14'),1,0)"));
            Assert.ThrowsException<ExpressionException>(() => new Expression("[ROUND](1,0)").Execute());
        }

        [TestMethod]
        public void Functions_DateAndTimeSpan_InvalidArguments()
        {
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("[EDATE]([CONCAT]('2024-01-15'),1,D)").Execute());
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("[EDATE]('2024-01-15',1.5,D)").Execute());
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("[EODATE]('2024-01-15',0,X)").Execute());
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("[EODATE]([CONCAT]('2024-01-15'),0,E)").Execute());

            foreach (var function in new[] { "DAYS", "HOURS", "MINUTES", "MILLSECONDS" })
            {
                Assert.ThrowsException<ExpressionException>(
                    () => new Expression("[" + function + "](1)").Execute());
            }

            Assert.AreEqual("20240115", EvalString("[TIMETOSTRING]([CONCAT]('2024-01-15'),yyyyMMdd)"));
        }

        [TestMethod]
        public void Functions_IfAndCase_ArityAndMatchOrder()
        {
            Assert.ThrowsException<ExpressionException>(() => new Expression("[IF](1,2)").Execute());
            Assert.ThrowsException<ExpressionException>(() => new Expression("[IF]()").Execute());
            Assert.ThrowsException<ExpressionException>(() => new Expression("[CASE]()").Execute());
            Assert.ThrowsException<ExpressionException>(() => new Expression("[CASE](1)").Execute());
            Assert.ThrowsException<ExpressionException>(() => FormulaAction.If(1, 2, 3));
            Assert.ThrowsException<ExpressionException>(() => FormulaAction.Case(1));

            Assert.AreEqual("a", EvalString("[CASE](1,1,'a',1,'b')"));
            Assert.AreEqual("two", EvalStringWithArgs(
                "[CASE](a,1,'one',b,'two','other')",
                new Dictionary<string, object> { { "a", 2 }, { "b", 2 } }));

            var nullCondition = new Dictionary<string, object> { { "a", null } };
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("[IF](a,1,2)").Execute(nullCondition));
        }

        [TestMethod]
        public void Registry_ValidatesArguments()
        {
            Assert.ThrowsException<ArgumentException>(() => FunctionRegistry.Register(" ", values => 1d));
            Assert.ThrowsException<ArgumentNullException>(() => FunctionRegistry.Register("X", null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => FunctionRegistry.Register("X", values => 1d, -1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => FunctionRegistry.Register("X", values => 1d, 2, 1));
        }

        [TestMethod]
        public void Registry_QueryAndUnregister_HandleMissingNames()
        {
            Assert.IsFalse(FunctionRegistry.Contains(null));
            Assert.IsFalse(FunctionRegistry.Contains("  "));
            Assert.IsFalse(FunctionRegistry.Unregister(null));
            Assert.IsFalse(FunctionRegistry.Unregister("  "));
            Assert.IsFalse(FunctionRegistry.Unregister("NOT_REGISTERED_FN"));
            Assert.IsFalse(FunctionRegistry.Register("SUM", values => 0d));
        }

        [TestMethod]
        public void Registry_DuplicateAndOverwrite()
        {
            Assert.IsTrue(FunctionRegistry.Register("DUPFN", values => 1d, 0, 0));
            try
            {
                Assert.IsFalse(FunctionRegistry.Register("dupfn", values => 2d, 0, 0));
                Assert.AreEqual(1d, Run("[DUPFN]()"));

                Assert.IsTrue(FunctionRegistry.Register("DUPFN", values => 2d, 0, 0, true));
                Assert.AreEqual(2d, Run("[dupfn]()"));
            }
            finally
            {
                Assert.IsTrue(FunctionRegistry.Unregister("DUPFN"));
            }
        }

        [TestMethod]
        public void Registry_TrimsFunctionNames()
        {
            Assert.IsTrue(FunctionRegistry.Register("  PADDED  ", values => 1d, 0, 0));
            try
            {
                Assert.IsTrue(FunctionRegistry.Contains("PADDED"));
                Assert.AreEqual(1d, Run("[PADDED]()"));
            }
            finally
            {
                Assert.IsTrue(FunctionRegistry.Unregister(" padded "));
            }
        }

        [TestMethod]
        public void Registry_UnregisterBuiltIn_RemovesAndCanBeRestored()
        {
            Assert.IsTrue(FunctionRegistry.Unregister("TRIM"));
            try
            {
                Assert.IsFalse(FunctionRegistry.Contains("TRIM"));
                Assert.ThrowsException<ExpressionException>(() => new Expression("[TRIM](' a ')"));
            }
            finally
            {
                Assert.IsTrue(FunctionRegistry.Register("TRIM", FormulaAction.Trim));
            }

            Assert.AreEqual("a", EvalString("[TRIM](' a ')"));
        }

        [TestMethod]
        public void Registry_CustomFunction_ReturnTypesAndExceptions()
        {
            Assert.IsTrue(FunctionRegistry.Register("RETURNNULL", values => null, 0, 0));
            Assert.IsTrue(FunctionRegistry.Register("RETURNINT", values => 42, 0, 0));
            Assert.IsTrue(FunctionRegistry.Register("RETURNDATE", values => new DateTime(2024, 1, 1), 0, 0));
            Assert.IsTrue(FunctionRegistry.Register(
                "THROWEX",
                values => { throw new ExpressionException("自定义错误"); },
                0,
                0));
            Assert.IsTrue(FunctionRegistry.Register(
                "THROWIO",
                values => { throw new InvalidOperationException("boom"); },
                0,
                0));
            try
            {
                Assert.IsNull(new Expression("[RETURNNULL]()").Execute());
                Assert.AreEqual(43d, Run("[RETURNINT]() + 1"));
                Assert.AreEqual("20240101", EvalString("[TIMETOSTRING]([RETURNDATE](),yyyyMMdd)"));

                var ex = Assert.ThrowsException<ExpressionException>(
                    () => new Expression("[THROWEX]()").Execute());
                Assert.AreEqual("自定义错误", ex.Message);

                var io = Assert.ThrowsException<ExpressionException>(
                    () => new Expression("[THROWIO]()").Execute());
                StringAssert.Contains(io.Message, "boom");
            }
            finally
            {
                FunctionRegistry.Unregister("RETURNNULL");
                FunctionRegistry.Unregister("RETURNINT");
                FunctionRegistry.Unregister("RETURNDATE");
                FunctionRegistry.Unregister("THROWEX");
                FunctionRegistry.Unregister("THROWIO");
            }
        }

        [TestMethod]
        public void Registry_CustomFunction_MinimumArity_IsValidated()
        {
            Assert.IsTrue(FunctionRegistry.Register("RANGE", values => values.Length, 2, 3));
            try
            {
                Assert.AreEqual(2d, Run("[RANGE](1,2)"));
                Assert.AreEqual(3d, Run("[RANGE](1,2,3)"));
                Assert.ThrowsException<ExpressionException>(
                    () => new Expression("[RANGE](1)").Execute());
            }
            finally
            {
                Assert.IsTrue(FunctionRegistry.Unregister("RANGE"));
            }
        }

        [TestMethod]
        public void Options_Whitelist_IsCaseInsensitive_AndEmptyMeansUnrestricted()
        {
            Assert.AreEqual(3d, RunWithOptions("[SUM](1,2)", new ExpressionOptions
            {
                FunctionWhitelist = new HashSet<string>()
            }));

            Assert.AreEqual(3d, RunWithOptions("[SUM](1,2)", new ExpressionOptions
            {
                FunctionWhitelist = new HashSet<string> { "sum" }
            }));

            Assert.IsTrue(FunctionRegistry.Register("WLFN", values => 1d, 0, 0));
            try
            {
                var options = new ExpressionOptions
                {
                    FunctionWhitelist = new HashSet<string> { "WLFN" }
                };
                Assert.AreEqual(1d, RunWithOptions("[WLFN]()", options));
                Assert.ThrowsException<ExpressionException>(() => new Expression("[SUM](1,2)", options));
            }
            finally
            {
                Assert.IsTrue(FunctionRegistry.Unregister("WLFN"));
            }
        }

        [TestMethod]
        public void Options_DivisionByZero_AppliesToNestedExpressions()
        {
            var zero = new ExpressionOptions { DivisionByZero = DivisionByZeroBehavior.Zero };
            Assert.AreEqual(0d, new Expression("1/(2-2)", zero).Execute());

            var throws = new ExpressionOptions { DivisionByZero = DivisionByZeroBehavior.Throw };
            Assert.ThrowsException<ExpressionException>(
                () => new Expression("1/(2-2)", throws).Execute());

            Assert.IsTrue(double.IsInfinity(Convert.ToDouble(new Expression("-1/0").Execute())));
            Assert.IsTrue(double.IsNaN(Convert.ToDouble(new Expression("0/0").Execute())));
        }

        [TestMethod]
        public void Options_CultureSpecificNumberBinding()
        {
            var german = new Expression("a + 1", new ExpressionOptions
            {
                Culture = new CultureInfo("de-DE")
            });
            german.LoadArgument(new Dictionary<string, object> { { "a", "1,5" } });
            Assert.AreEqual(2.5d, german.Execute());

            var nullCulture = new Expression("a + 1", new ExpressionOptions { Culture = null });
            nullCulture.LoadArgument(new Dictionary<string, object> { { "a", 1.5 } });
            Assert.AreEqual(2.5d, nullCulture.Execute());
        }

        [TestMethod]
        public void OperatorMetadata_UnknownValue_Throws()
        {
            Assert.ThrowsException<InvalidOperationException>(() => ((Operator)999).GetOperatorObj());
        }

        [TestMethod]
        public void OperatorMetadata_NonOperatorEnum_UsesReflection()
        {
            Assert.AreEqual("+", DecoratedEnum.Plus.GetOperatorObj().Value);
            Assert.AreEqual(4, DecoratedEnum.Plus.GetOperatorObj().Level);
        }

        private enum DecoratedEnum
        {
            [Operator("加", 4, "+")]
            Plus = 1
        }

        private sealed class TextLike
        {
            private readonly string _text;

            public TextLike(string text)
            {
                _text = text;
            }

            public override string ToString()
            {
                return _text;
            }
        }

        #endregion

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
