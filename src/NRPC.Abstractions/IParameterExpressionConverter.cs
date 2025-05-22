using System.Linq.Expressions;
using System.Reflection;

namespace NRPC.Abstractions
{
    public interface IParameterExpressionConverter
    {
        Expression Convert(Expression parameterExpression, ParameterInfo parameterInfo);
    }
}