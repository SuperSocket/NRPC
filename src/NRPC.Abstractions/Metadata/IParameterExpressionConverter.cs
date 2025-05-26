using System.Linq.Expressions;
using System.Reflection;

namespace NRPC.Abstractions.Metadata
{
    public interface IParameterExpressionConverter
    {
        Expression Convert(Expression parameterExpression, ParameterInfo parameterInfo);
    }
}