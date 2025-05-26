# NRPC (No RPC)

NRPC is a .NET library that provides RPC-like programming interfaces using message queue services or other distributed communication facilities. It allows you to create distributed applications with clean service interfaces while abstracting away the underlying communication mechanisms.

## Overview

NRPC enables you to:
- Define service contracts using simple .NET interfaces with attributes
- Implement RPC-style method calls over various transport mechanisms
- Support for both synchronous and asynchronous communication patterns
- Generate proxy implementations at runtime
- High-performance service execution with pre-compiled method handlers

## Project Structure

The repository consists of several components:

- **NRPC.Abstractions**: Core interfaces, models, and metadata for RPC communication
- **NRPC.Caller**: Client-side implementation for sending requests and handling responses (formerly NRPC.Client)
- **NRPC.Proxy**: Dynamic proxy generation for RPC interfaces
- **NRPC.Executor**: Server-side implementation for handling requests with compiled service handlers

## Getting Started

### Installation

```bash
dotnet add package NRPC
```

### Usage Example

1. Define your service interface with the ServiceContract attribute:

```csharp
using NRPC.Caller;

[ServiceContractAtribute]
public interface ICalculator
{
    Task<int> Add(int x, int y);
    Task<string> Concat(string x, string y);
    Task ExecuteVoid(string command);
}
```

2. Implement the service:

```csharp
public class CalculatorService : ICalculator
{
    public Task<int> Add(int x, int y) => Task.FromResult(x + y);
    
    public Task<string> Concat(string x, string y) => Task.FromResult(x + y);
    
    public Task ExecuteVoid(string command)
    {
        Console.WriteLine($"Executing: {command}");
        return Task.CompletedTask;
    }
}
```

3. Create and use the RPC caller:

```csharp
using NRPC.Caller;
using NRPC.Abstractions;

// Create a connection factory (implement IRpcConnectionFactory)
var connectionFactory = new YourRpcConnectionFactory();

// Create the caller factory
var callerFactory = new RpcCallerFactory<ICalculator>(connectionFactory);

// Create and use the caller
var calculator = await callerFactory.CreateCaller();
var result = await calculator.Add(5, 3); // Returns 8
```

4. Handle requests on the server side:

```csharp
using NRPC.Executor;
using NRPC.Abstractions.Metadata;

// Create service handler
var serviceHandler = new CompiledServiceHandler<ICalculator>();
var service = new CalculatorService();

// Handle incoming request
var request = new RpcRequest { Method = "Add", Parameters = new object[] { 5, 3 } };
var response = await serviceHandler.HandleRequestAsync(service, request);
```

## Features

- **Interface-based**: Define clean service contracts using C# interfaces with ServiceContract attributes
- **Dynamic Proxies**: Runtime generation of proxy types for seamless method calls
- **Task-based API**: All remote operations are asynchronous by design
- **Transport Abstraction**: Independent from the underlying communication mechanism through IRpcConnection
- **High Performance**: Pre-compiled service handlers for optimal execution speed
- **Error Handling**: Built-in RPC exception handling with detailed error information
- **Metadata System**: Rich metadata support for service and method information
- **Extensible**: Support for custom serialization, transport, and calling adapters

## Key Components

### Service Contracts
Mark your interfaces with `[ServiceContractAttribute]` to define RPC service contracts:

```csharp
[ServiceContractAttribute]
public interface IMyService
{
    Task<string> GetData(int id);
}
```

### RPC Connection
Implement `IRpcConnection` to define how requests and responses are transmitted:

```csharp
public interface IRpcConnection
{
    Task SendAsync(RpcRequest request);
    Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default);
}
```

### Compiled Service Handler
Use `CompiledServiceHandler<T>` for high-performance server-side request handling with pre-compiled method invocation.

## Supported Platforms

- .NET 6.0+
- .NET Standard 2.0+

## Architecture

NRPC follows a clean architecture with clear separation of concerns:

- **Abstractions Layer**: Core interfaces and data models (`RpcRequest`, `RpcResponse`, `RpcError`)
- **Metadata System**: Service and method metadata for efficient runtime operations
- **Caller Layer**: Client-side proxy generation and request/response handling
- **Executor Layer**: Server-side request processing with compiled handlers
- **Proxy Layer**: Dynamic proxy generation for transparent method calls

## Testing

The project includes comprehensive tests covering:
- RPC connection workflows
- Client-to-server communication
- Service handler functionality
- Proxy generation and method invocation
- Error handling scenarios

## License

This project is licensed under the terms of the license included in the repository.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
