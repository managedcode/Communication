# Communication

[![.NET](https://github.com/managedcode/Communication/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/Communication/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/Communication/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/Communication?branch=main)
[![nuget](https://github.com/managedcode/Communication/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/Communication/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managedcode/Communication/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Communication/actions/workflows/codeql-analysis.yml)
[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication) 


The Communication library is a convenient wrapper for handling the results of functions that do not return exceptions. 
Instead of throwing exceptions, these functions return an object that contains the result of the operation. 
This makes it easy to handle and process the results of these operations in a consistent, reliable way.

## Motivation

Many functions in software development can fail and throw exceptions when something goes wrong. 
This can make it difficult to handle errors and to read and understand code that uses these functions.

Exceptions are a powerful tool for handling error conditions in your code, but they can also be difficult to manage and can make your code harder to read and understand. 
Instead of throwing exceptions, the Communication library allows you to return a Result object that contains the result of an operation. 
This makes it easy to handle and process the results of these operations in a consistent, reliable way.

## Features
- Wraps the result of a function in an object, eliminating the need to handle exceptions.
- Makes it easy to check whether the function was successful or not. 
- Provides access to the function's output via simple property accessors.

## Getting Started

To use the Communication library in your project, you will need to add a reference to the Communication assembly. 
You can do this by downloading the library from GitHub and adding it to your project, or by installing the Communication NuGet package.

Once you have added a reference to the Communication assembly, you can start using the library in your code. 
Here is a simple example of how to use the Communication library to handle the result of an operation:

```csharp
var succeed = Result.Succeed();
if(succeed.IsSuccess)
{
    // do some
}

var fail = Result.Fail();
if(fail.IsFailed)
{
    // do some
}
```

Generic Result
```csharp
var succeed = Result<MyObject>.Succeed(new MyObject());
if(succeed.IsSuccess)
{
    succeed.Value // <-- this is the result
    // do some
}

var fail = Result<MyObject>.Fail("Oops!");
if(fail.IsFailed)
{
    // do some
}
```

From
```csharp
var succeed = await Result<MyObject>.From(() => GetMyResult());
if(succeed.IsSuccess)
{
    succeed.Value // <-- this is the result
    // do some
}
```

## Conclusion
In summary, our library provides a convenient and easy-to-use solution for handling the result of a function that may throw exceptions. 
It eliminates the need to handle exceptions and makes it easy to check whether the function was successful and to access its output. 
We hope you find it useful in your own projects!
