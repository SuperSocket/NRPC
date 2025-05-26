using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NRPC.Abstractions.Metadata
{
    public abstract class MethodMetadata
    {
        public string Name { get; }

        public Type ReturnType { get; }

        public IReadOnlyList<ParameterMetadata> Parameters { get; }

        public MethodInfo MethodInfo { get; }

        public MethodMetadata(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            Name = methodInfo.Name;
            ReturnType = methodInfo.ReturnType;
            Parameters = methodInfo.GetParameters().Select(p => new ParameterMetadata(p)).ToArray();
        }
    }

    public class MethodMetadata<TService> : MethodMetadata
    {
        public Func<TService, object[], Task<object>> Caller { get; }

        private readonly IParameterExpressionConverter _parameterExpressionConverter;

        public MethodMetadata(MethodInfo methodInfo)
            : this(methodInfo, DirectTypeParameterExpressionConverter.Singleton)
        {
        }

        public MethodMetadata(MethodInfo methodInfo, IParameterExpressionConverter parameterConverter)
            : base(methodInfo)
        {
            _parameterExpressionConverter = parameterConverter ?? throw new ArgumentNullException(nameof(parameterConverter));

            var compiledCallerMethod = this.GetType()
                .GetMethod(nameof(CompileCaller), BindingFlags.NonPublic | BindingFlags.Instance);

            var finalCompiledMethod = compiledCallerMethod.MakeGenericMethod(ReturnType.IsGenericType
                ? ReturnType.GenericTypeArguments[0]
                : typeof(object));

            Caller = (Func<TService, object[], Task<object>>)finalCompiledMethod.Invoke(this, [ methodInfo ]);
        }

        private Func<TService, object[], Task<object>> CompileCaller<TTaskResult>(MethodInfo method)
        {
            // Create parameters for the lambda expression
            ParameterExpression serviceParam = Expression.Parameter(typeof(TService), "service");
            ParameterExpression argumentsParam = Expression.Parameter(typeof(object[]), "arguments");

            // Get the parameter info
            ParameterInfo[] parameters = method.GetParameters();

            // Create an array of argument expressions
            Expression[] argumentExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                // Get the argument from the arguments array
                Expression argument = Expression.ArrayIndex(argumentsParam, Expression.Constant(i));
                argumentExpressions[i] = _parameterExpressionConverter.Convert(argument, parameters[i]);
            }

            // Create the method call expression with the service instance
            Expression methodCall = Expression.Call(serviceParam, method, argumentExpressions);

            if (method.ReturnType.IsGenericType)
            {
                // Create a lambda expression that calls the method
                var lambda = Expression.Lambda<Func<TService, object[], Task<TTaskResult>>>(
                    Expression.Convert(methodCall, typeof(Task<TTaskResult>)),
                    serviceParam,
                    argumentsParam
                );

                var compiledLambda = lambda.Compile();

                // Create a wrapper that gets the result from the Task
                return async (service, args) =>
                {
                    return await compiledLambda(service, args);
                };
            }
            else
            {
                // For regular Task, return null when completed
                var lambda = Expression.Lambda<Func<TService, object[], Task>>(
                    Expression.Convert(methodCall, typeof(Task)),
                    serviceParam,
                    argumentsParam
                );
                var compiledLambda = lambda.Compile();

                return async (service, args) =>
                {
                    await compiledLambda(service, args);
                    return null;
                };
            }
        }

        
    }
}