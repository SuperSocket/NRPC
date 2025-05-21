using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Executor
{
    /// <summary>
    /// A high-performance service handler that pre-compiles all service methods during initialization
    /// </summary>
    /// <typeparam name="TService">The type of service to handle</typeparam>
    public class CompiledServiceHandler<TService>
    {
        private readonly IParameterConverter _parameterConverter;

        private readonly Dictionary<string, Func<TService, object[], Task<object>>> _compiledMethods;

        public CompiledServiceHandler()
            : this(new DirectTypeParameterConverter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the CompiledServiceHandler and pre-compiles all methods
        /// </summary>
        public CompiledServiceHandler(IParameterConverter parameterConverter)
        {
            _parameterConverter = parameterConverter ?? throw new ArgumentNullException(nameof(parameterConverter));
            _compiledMethods = new Dictionary<string, Func<TService, object[], Task<object>>>(StringComparer.OrdinalIgnoreCase);

            // Pre-compile all public methods of the service
            CompileServiceMethods();
        }

        /// <summary>
        /// Handles an RPC request by invoking the appropriate pre-compiled method
        /// </summary>
        /// <param name="service">The service instance that will handle the request</param>
        /// <param name="request">The RPC request to handle</param>
        /// <returns>A task containing the RPC response</returns>
        public async Task<RpcResponse> HandleRequestAsync(TService service, RpcRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                if (!_compiledMethods.TryGetValue(request.MethodName, out var methodInvoker))
                {
                    return new RpcResponse
                    {
                        Id = request.Id,
                        Error = new RpcError(404, $"Method '{request.MethodName}' not found on service {typeof(TService).Name}")
                    };
                }

                // Invoke the pre-compiled method
                object result = await methodInvoker(service, request.Arguments).ConfigureAwait(false);

                return new RpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException
                if (ex is TargetInvocationException tie)
                    ex = tie.InnerException ?? tie;

                return new RpcResponse
                {
                    Id = request.Id,
                    Error = new RpcError(500, ex.Message)
                };
            }
        }

        private void CompileServiceMethods()
        {
            Type serviceType = typeof(TService);

            var compileMethod = this.GetType()
                .GetMethod(nameof(CompileMethod), BindingFlags.NonPublic | BindingFlags.Instance);

            // Get all public methods
            foreach (MethodInfo method in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName) // Exclude property accessors
                .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType))) // Only include async methods
            {
                // Create a delegate for each method
                Func<TService, object[], Task<object>> compiledMethod =
                    (Func<TService, object[], Task<object>>)compileMethod
                        .MakeGenericMethod(method.ReturnType.IsGenericType
                            ? method.ReturnType.GenericTypeArguments[0]
                            : typeof(object))
                        .Invoke(this, new object[] { method });

                _compiledMethods[method.Name] = compiledMethod;
            }
        }

        private Func<TService, object[], Task<object>> CompileMethod<TTaskResult>(MethodInfo method)
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
                argumentExpressions[i] = _parameterConverter.Convert(argument, parameters[i]);
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
