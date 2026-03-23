using EasyExpression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace EasyExpression.UnitTest
{
    /// <summary>
    /// 补充场景：异常、字面量、关系运算、未覆盖的字符串/日期函数、GetAllParams、Check、扩展方法等。
    /// </summary>
    [TestClass]
    public class ExpressionAdditionalTests
    {
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
            // 百分号需作为完整数据标记解析；通过变量代入非法百分比字面量
            var exp = new Expression("1 + a");
            exp.LoadArgument(new Dictionary<string, object> { { "a", "bad%" } });
            Assert.ThrowsException<ExpressionException>(() => exp.Execute());
        }

        [TestMethod]
        public void Execute_PercentLiteral_AsFraction_ViaVariable()
        {
            // 表达式中的「%」会被解析为取模运算符，百分比需以单个数据标记出现（例如变量替换后）
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

        /// <summary>
        /// 业务场景：带点号的实参名；两侧均为「非空」判断，用 & 组合（与空字符串组合校验常见写法一致）。
        /// </summary>
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
            // 空字符串不是 null，ISNULL 为 0，取非后为 1
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
            // 格式串中含「-」会被解析为减法，故使用无连字符格式
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

        [XmlRoot("Sample")]
        public class DeepCopySample
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
