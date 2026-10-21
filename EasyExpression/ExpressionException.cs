using System;

namespace EasyExpression
{
    /// <summary>
    /// 表达式解析、绑定或求值失败时抛出的异常。
    /// </summary>
    public class ExpressionException : Exception
    {
        /// <summary>
        /// 使用错误信息创建异常。
        /// </summary>
        public ExpressionException(string message) : base(message)
        {
        }

        /// <summary>
        /// 使用错误信息和内部异常创建异常。
        /// </summary>
        public ExpressionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
