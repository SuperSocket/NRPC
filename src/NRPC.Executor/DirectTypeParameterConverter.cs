using System;
using System.Linq.Expressions;
using System.Reflection;
using NRPC.Abstractions;

namespace NRPC.Executor
{
    public class DirectTypeParameterConverter : IParameterConverter
    {
        public Expression Convert(Expression parameterExpression, ParameterInfo parameterInfo)
        {
            Type parameterType = parameterInfo.ParameterType;
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