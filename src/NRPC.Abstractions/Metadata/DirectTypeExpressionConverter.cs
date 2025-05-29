using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NRPC.Abstractions.Metadata
{
    public class DirectTypeExpressionConverter : IExpressionConverter
    {
        public static DirectTypeExpressionConverter Singleton { get; } = new DirectTypeExpressionConverter();

        private DirectTypeExpressionConverter()
        {
        }

        public Expression Convert(Expression parameterExpression, Type dataType)
        {
            // Handle null values and type conversions
            return Expression.Condition(
                Expression.Equal(parameterExpression, Expression.Constant(null)),
                Expression.Default(dataType),
                Expression.Convert(parameterExpression, dataType));
        }
    }
}