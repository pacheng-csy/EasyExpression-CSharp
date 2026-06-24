using System;
using System.Collections.Generic;
using System.Linq;
using EasyExpression.Internal;

namespace EasyExpression
{
    public class Expression
    {
        public Expression(string expression)
        {
            Init(expression);
        }

        private Expression()
        {
            ExpressionChildren = new List<Expression>();
            Operators = new List<Operator>();
            DataString = string.Empty;
            SourceExpressionString = string.Empty;
            RealityString = null;
        }

        internal static Expression CreateNode() => new Expression();

        private void Init(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ExpressionException("表达式不能为空");
            }

            SourceExpressionString = expression.Trim()
                .Replace("||", "|")
                .Replace("\\\\", "\\")
                .Replace("&&", "&")
                .Replace("==", "=");
            ExpressionChildren = new List<Expression>();
            Operators = new List<Operator>();
            DataString = string.Empty;
            RealityString = null;
            if (!Parser.TryParse(this))
            {
                throw new ExpressionException($"表达式: {SourceExpressionString} 解析错误!");
            }
        }

        public string ErrorMessgage { get; set; }

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
            return Evaluator.Execute(this);
        }

        public List<KeyValuePair<string, ElementType>> GetAllParams()
        {
            var results = new List<KeyValuePair<string, ElementType>>();
            if (ElementType == ElementType.Data && ExpressionChildren.Count == 0)
            {
                results.Add(new KeyValuePair<string, ElementType>(DataString.Replace("\\", ""), ElementType));
            }
            else
            {
                results.AddRange(GetChildrenAllParams(this));
            }

            return results;
        }

        private List<KeyValuePair<string, ElementType>> GetChildrenAllParams(Expression parent = null)
        {
            var childrenResults = new List<KeyValuePair<string, ElementType>>();
            foreach (var childExp in ExpressionChildren)
            {
                switch (childExp.ElementType)
                {
                    case ElementType.Expression:
                        childrenResults.AddRange(childExp.GetChildrenAllParams(childExp));
                        break;
                    case ElementType.Function:
                        if (childExp.ExpressionChildren.Any())
                        {
                            childrenResults.AddRange(childExp.GetChildrenAllParams(childExp));
                        }
                        else
                        {
                            var paramList = childExp.DataString.Split(',').ToList();
                            paramList.ForEach(x =>
                            {
                                childrenResults.Add(new KeyValuePair<string, ElementType>(
                                    x.Replace("\\", ""),
                                    ElementType.Function));
                            });
                        }

                        break;
                    case ElementType.Data:
                    case ElementType.Reference:
                        var type = ElementType.Data;
                        if (parent != null && parent.ElementType != ElementType.Expression)
                        {
                            type = parent.ElementType = parent.ElementType;
                        }

                        childrenResults.Add(new KeyValuePair<string, ElementType>(
                            childExp.DataString.Replace("\\", ""),
                            type));
                        break;
                }
            }

            return childrenResults;
        }

        private void CheckExpression(Expression expression)
        {
            if (expression.Operators.Any())
            {
                var notOperatorCount = expression.Operators.Count(x => x == Operator.Not);
                if (expression.ExpressionChildren.Count - (expression.Operators.Count - notOperatorCount) != 1)
                {
                    throw new ExpressionException("expression check error: data not match operator");
                }
            }

            foreach (var child in expression.ExpressionChildren)
            {
                CheckExpression(child);
            }
        }
    }
}
