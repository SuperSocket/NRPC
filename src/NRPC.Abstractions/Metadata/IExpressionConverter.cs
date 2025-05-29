using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NRPC.Abstractions.Metadata
{
    public interface IExpressionConverter
    {
        Expression Convert(Expression parameterExpression, Type dataType);
    }
}