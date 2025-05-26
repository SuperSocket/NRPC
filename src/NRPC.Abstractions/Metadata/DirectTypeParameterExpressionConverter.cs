using System.Linq.Expressions;
using System.Reflection;

namespace NRPC.Abstractions.Metadata
{
    class DirectTypeParameterExpressionConverter : IParameterExpressionConverter
    {
        public static DirectTypeParameterExpressionConverter Singleton { get; } = new DirectTypeParameterExpressionConverter();

        private DirectTypeParameterExpressionConverter()
        {
        }

        public Expression Convert(Expression parameterExpression, ParameterInfo parameterInfo)
        {
            var parameterType = parameterInfo.ParameterType;
            if (parameterType.IsByRef)
                parameterType = parameterType.GetElementType();

            // Handle null values and type conversions
            return Expression.Condition(
                Expression.Equal(parameterExpression, Expression.Constant(null)),
                Expression.Default(parameterType),
                Expression.Convert(parameterExpression, parameterType));
        }
    }
}