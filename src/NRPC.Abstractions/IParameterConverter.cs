using System.Linq.Expressions;
using System.Reflection;

namespace NRPC.Abstractions
{
    public interface IParameterConverter
    {
        Expression Convert(Expression parameterExpression, ParameterInfo parameterInfo);
    }
}