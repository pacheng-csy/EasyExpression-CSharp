using System;
using System.Collections.Generic;
using System.Linq;
using EasyExpression.Internal;

namespace EasyExpression
{
    public class Expression
    {
        public Expression(string expression)
            : this(expression, null)
        {
        }

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

        public List<KeyValuePair<string, string>> LoadArgument(Dictionary<string, object> keyValues)
        {
            return ArgumentBinder.LoadArgument(this, keyValues);
        }

        [Obsolete("推荐使用 List<KeyValuePair<string, string>> LoadArgument(Dictionary<string, object> keyValues)")]
        public List<KeyValuePair<string, string>> LoadArgument(Dictionary<string, string> keyValues)
        {
            return LoadArgument(keyValues.ToDictionary(x => x.Key, x => (object)x.Value));
        }

        public void LoadArgument()
        {
            ArgumentBinder.LoadArgumentWithoutDictionary(this);
        }

        public void Check()
        {
            CheckExpression(this);
        }

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
        public object Execute(Dictionary<string, object> keyValues)
        {
            LoadArgument(keyValues);
            return Execute();
        }

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