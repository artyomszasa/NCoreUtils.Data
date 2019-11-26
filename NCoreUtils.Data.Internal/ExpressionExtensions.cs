using System.Linq.Expressions;

namespace NCoreUtils.Data.Internal
{
    static class ExpressionExtensions
    {
        public static int ExtractInt32(this Expression expression)
        {
            return (int)expression.MaybeExtractConstant().Value;
        }
    }
}