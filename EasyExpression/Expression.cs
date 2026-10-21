using System;
using System.Collections.Generic;
using System.Linq;
using EasyExpression.Internal;

namespace EasyExpression
{
    /// <summary>
    /// 表达式树。构造时完成词法分析与语法分析，<see cref="LoadArgument(Dictionary{string, object})"/>
    /// 绑定外部实参后通过 <see cref="Execute()"/> 求值。
    /// </summary>
    public class Expression
    {
        /// <summary>
        /// 使用当前区域性解析表达式。
        /// </summary>
        /// <param name="expression">表达式文本。</param>
        public Expression(string expression)
            : this(expression, null)
        {
        }

        /// <summary>
        /// 使用指定选项解析表达式。
        /// </summary>
        /// <param name="expression">表达式文本。</param>
        /// <param name="options">区域性、除零行为、函数白名单等选项；为 null 时使用默认选项。</param>
        public Expression(string expression, ExpressionOptions options)
        {
            Options = options ?? new ExpressionOptions();
            Init(expression);
        }

        private Expression(ExpressionOptions options)
        {
            Options = options ?? new ExpressionOptions();
            ExpressionChildren = new List<Expression>();
            Operators = new List<Operator>();
            DataString = string.Empty;
            SourceExpressionString = string.Empty;
            RealityString = null;
        }

        internal static Expression CreateNode()
        {
            return new Expression((ExpressionOptions)null);
        }

        internal static Expression CreateNode(ExpressionOptions options)
        {
            return new Expression(options);
        }

        private void Init(string expression)
        {
            if (expression == null)
            {
                throw new ExpressionException("表达式不能为空");
            }

            var source = expression.Trim();
            if (source.Length == 0)
            {
                throw new ExpressionException("表达式不能为空");
            }

            SourceExpressionString = source;
            ExpressionChildren = new List<Expression>();
            Operators = new List<Operator>();
            DataString = string.Empty;
            RealityString = null;
            ErrorMessgage = null;
            ArgumentsLoaded = false;

            if (!Parser.TryParse(this))
            {
                var detail = string.IsNullOrEmpty(ErrorMessgage) ? string.Empty : ErrorMessgage;
                throw new ExpressionException($"表达式: {source} 解析错误! {detail}".TrimEnd());
            }
        }

        /// <summary>
        /// 解析失败时的错误信息（保留原属性名以兼容旧版本）。
        /// </summary>
        public string ErrorMessgage { get; set; }

        /// <summary>
        /// <see cref="ErrorMessgage"/> 的别名，拼写更清晰。
        /// </summary>
        public string ErrorMessage
        {
            get { return ErrorMessgage; }
            set { ErrorMessgage = value; }
        }

        public bool Status { get; set; } = true;

        public ElementType ElementType { get; set; }

        public string SourceExpressionString { get; set; }

        public string DataString { get; set; }

        public string RealityString { get; set; }

        public List<Operator> Operators { get; set; }

        public FunctionType FunctionType { get; set; }

        internal Function Function { get; set; }

        public string FunctionName { get; set; }

        public List<Expression> ExpressionChildren { get; set; }

        internal ExpressionOptions Options { get; set; }

        internal bool IsLiteral { get; set; }

        internal bool IsStringLiteral { get; set; }

        internal bool HasBoundValue { get; set; }

        internal object BoundValue { get; set; }

        internal bool IsUnbound { get; set; }

        internal bool ArgumentsLoaded { get; set; }

        internal void CopyFrom(Expression other)
        {
            if (other == null)
            {
                return;
            }

            Status = other.Status;
            ElementType = other.ElementType;
            SourceExpressionString = other.SourceExpressionString;
            DataString = other.DataString;
            RealityString = other.RealityString;
            Operators = other.Operators ?? new List<Operator>();
            FunctionType = other.FunctionType;
            Function = other.Function;
            FunctionName = other.FunctionName;
            ExpressionChildren = other.ExpressionChildren ?? new List<Expression>();
            ErrorMessgage = other.ErrorMessgage;
            IsLiteral = other.IsLiteral;
            IsStringLiteral = other.IsStringLiteral;
            HasBoundValue = other.HasBoundValue;
            BoundValue = other.BoundValue;
            IsUnbound = other.IsUnbound;
            ArgumentsLoaded = other.ArgumentsLoaded;
        }

        /// <summary>
        /// 把外部实参绑定到表达式树中的数据节点。
        /// </summary>
        /// <param name="keyValues">参数名与参数值；字典中不存在的变量在求值时会被视为未提供实参。</param>
        /// <returns>本次实际绑定成功的参数名与字符串值。</returns>
        public List<KeyValuePair<string, string>> LoadArgument(Dictionary<string, object> keyValues)
        {
            return ArgumentBinder.LoadArgument(this, keyValues);
        }

        /// <summary>
        /// 字符串字典版本的参数绑定，已过时。
        /// </summary>
        [Obsolete("推荐使用 List<KeyValuePair<string, string>> LoadArgument(Dictionary<string, object> keyValues)")]
        public List<KeyValuePair<string, string>> LoadArgument(Dictionary<string, string> keyValues)
        {
            return LoadArgument(keyValues.ToDictionary(x => x.Key, x => (object)x.Value));
        }

        /// <summary>
        /// 不绑定外部实参，直接把表达式中的变量名作为文本值使用。
        /// </summary>
        public void LoadArgument()
        {
            ArgumentBinder.LoadArgumentWithoutDictionary(this);
        }

        /// <summary>
        /// 校验表达式树中运算符与操作数的数量是否匹配，不匹配时抛出 <see cref="ExpressionException"/>。
        /// </summary>
        public void Check()
        {
            CheckExpression(this);
        }

        /// <summary>
        /// 计算表达式结果。若尚未调用 <c>LoadArgument</c>，会先按空参数绑定一次。
        /// </summary>
        /// <returns>double、string、DateTime、TimeSpan 或 null。</returns>
        /// <exception cref="ExpressionException">参数缺失、类型不匹配或函数执行失败。</exception>
        public object Execute()
        {
            if (!ArgumentsLoaded)
            {
                LoadArgument(new Dictionary<string, object>());
            }

            try
            {
                return Evaluator.Execute(this);
            }
            catch (ExpressionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExpressionException(ex.Message, ex);
            }
        }

        /// <summary>
        /// 绑定参数并立即计算。
        /// </summary>
        /// <param name="keyValues">参数名与参数值。</param>
        /// <returns>表达式求值结果。</returns>
        public object Execute(Dictionary<string, object> keyValues)
        {
            LoadArgument(keyValues);
            return Execute();
        }

        /// <summary>
        /// 获取表达式中引用的全部外部参数（不包含数字与字符串字面量）。
        /// </summary>
        /// <returns>参数名及其出现位置所属的节点类型；同名参数会按出现次数重复返回。</returns>
        public List<KeyValuePair<string, ElementType>> GetAllParams()
        {
            var results = new List<KeyValuePair<string, ElementType>>();
            CollectParams(this, null, results);
            return results;
        }

        private static void CollectParams(
            Expression expression,
            Expression parent,
            List<KeyValuePair<string, ElementType>> results)
        {
            if (expression == null)
            {
                return;
            }

            if (expression.ElementType == ElementType.Data || expression.ElementType == ElementType.Reference)
            {
                if (!expression.IsLiteral && !string.IsNullOrEmpty(expression.DataString))
                {
                    var type = parent != null && parent.ElementType == ElementType.Function
                        ? ElementType.Function
                        : ElementType.Data;
                    results.Add(new KeyValuePair<string, ElementType>(
                        expression.DataString.Replace("\\", ""),
                        type));
                }

                return;
            }

            if (expression.ExpressionChildren == null)
            {
                return;
            }

            foreach (var child in expression.ExpressionChildren)
            {
                CollectParams(child, expression, results);
            }
        }

        private static void CheckExpression(Expression expression)
        {
            if (expression == null)
            {
                return;
            }

            if (expression.Operators != null && expression.Operators.Count > 0)
            {
                var unaryCount = expression.Operators.Count(x => x == Operator.Not || x == Operator.Negative);
                var binaryCount = expression.Operators.Count - unaryCount;
                var childCount = expression.ExpressionChildren == null ? 0 : expression.ExpressionChildren.Count;
                if (childCount != binaryCount + 1)
                {
                    throw new ExpressionException("expression check error: data not match operator");
                }
            }
            else if (expression.ElementType == ElementType.Expression
                     || expression.ElementType == ElementType.Placeholder)
            {
                if (expression.ExpressionChildren == null || expression.ExpressionChildren.Count != 1)
                {
                    throw new ExpressionException("expression check error: empty expression");
                }
            }

            if (expression.ExpressionChildren == null)
            {
                return;
            }

            foreach (var child in expression.ExpressionChildren)
            {
                CheckExpression(child);
            }
        }
    }
}