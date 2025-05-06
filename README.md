# NRPC (No RPC)

NRPC is a .NET library that provides RPC-like programming interfaces using message queue services or other distributed communication facilities. It allows you to create distributed applications with clean service interfaces while abstracting away the underlying communication mechanisms.

## Overview

NRPC enables you to:
- Define service contracts using simple .NET interfaces
- Implement RPC-style method calls over various transport mechanisms
- Support for both synchronous and asynchronous communication patterns
- Generate proxy implementations at runtime

## Project Structure

The repository consists of several components:

- **NRPC.Abstractions**: Core interfaces and models for RPC communication
- **NRPC.Client**: Client-side implementation for sending requests and handling responses
- **NRPC.Proxy**: Dynamic proxy generation for RPC interfaces
- **NRPC.Server**: Server-side implementation for handling requests

## Getting Started

### Installation

```bash
dotnet add package NRPC
```

### Usage Example

1. Define your service interface:

```csharp
public interface ICaculator
{
    Task<int> Add(int x, int y);
    Task<string> Concact(string x, string y);
    Task Execute(string command);
}
```

2. Create and configure the client:

```csharp
// Configure services
services.AddNRPCClient<ICaculator>(options => {
    // Configure connection options
});

// Use the client
var calculator = serviceProvider.GetRequiredService<ICaculator>();
var result = await calculator.Add(5, 3);
```

## Features

- **Interface-based**: Define clean service contracts using C# interfaces
- **Dynamic Proxies**: Runtime generation of proxy types
- **Task-based API**: All remote operations are asynchronous
- **Transport Abstraction**: Independent from the underlying communication mechanism
- **Extensible**: Support for custom serialization and transport

## Supported Platforms

- .NET 6.0+
- .NET Standard 2.0+

## License

This project is licensed under the terms of the license included in the repository.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
