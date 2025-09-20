# Generic Problem Type Analysis

## Current Situation

Currently all Result types use the concrete `Problem` class:
- `Result` has `Problem? Problem`
- `Result<T>` has `Problem? Problem`  
- `CollectionResult<T>` has `Problem? Problem`
- Interface `IResultProblem` defines `Problem? Problem`

## Proposed Generic Approach

### Option 1: Fully Generic Result Types
```csharp
public interface IResult<TProblem> 
{
    bool IsSuccess { get; }
    TProblem? Problem { get; set; }
    bool HasProblem { get; }
}

public interface IResult<T, TProblem> : IResult<TProblem>
{
    T? Value { get; set; }
}

public interface IResultCollection<T, TProblem> : IResult<T[], TProblem>
{
    T[] Collection { get; }
    // pagination properties...
}
```

### Option 2: Constraint-Based Approach
```csharp
public interface IProblem 
{
    string? Title { get; }
    string? Detail { get; }
    int StatusCode { get; }
}

public interface IResult<TProblem> where TProblem : IProblem
{
    bool IsSuccess { get; }
    TProblem? Problem { get; set; }
    bool HasProblem { get; }
}
```

### Option 3: Hybrid Approach (Backward Compatible)
```csharp
// New generic interfaces
public interface IResultProblem<TProblem>
{
    TProblem? Problem { get; set; }
    bool HasProblem { get; }
}

public interface IResult<TProblem> : IResultProblem<TProblem>
{
    bool IsSuccess { get; }
}

// Existing interfaces inherit from generic ones
public interface IResult : IResult<Problem> { }
public interface IResult<T> : IResult, IResultValue<T> { }
```

## Pros and Cons Analysis

### ✅ Pros of Generic Problem Types

1. **Type Safety**: Compile-time checking for problem types
2. **Flexibility**: Custom error types for different domains
3. **Performance**: No boxing/unboxing for value-type problems
4. **Domain Modeling**: Better alignment with domain-specific error types

Example use cases:
```csharp
// Domain-specific error types
public class ValidationProblem 
{
    public Dictionary<string, string[]> Errors { get; set; }
}

public class BusinessRuleProblem
{
    public string RuleId { get; set; }
    public string Message { get; set; }
}

// Usage
IResult<ValidationProblem> ValidateUser(User user);
IResult<BusinessRuleProblem> ApplyBusinessRule(Order order);
```

### ❌ Cons of Generic Problem Types

1. **Complexity**: More complex API surface
2. **Breaking Changes**: Potential breaking changes for existing code
3. **JSON Serialization**: Need custom converters for each problem type
4. **Interoperability**: Different Result<T, TProblem> types can't be easily combined
5. **Learning Curve**: More difficult for developers to understand and use

### 🤔 Specific Issues

1. **Method Signatures Explosion**:
```csharp
// Before
Result<User> GetUser(int id);
Result<Order> GetOrder(int id);

// After - not interoperable
Result<User, ValidationProblem> GetUser(int id);  
Result<Order, BusinessProblem> GetOrder(int id);
```

2. **Generic Constraint Propagation**:
```csharp
// Every method needs to be generic
public async Task<Result<T, TProblem>> ProcessAsync<T, TProblem>(Result<T, TProblem> input) 
    where TProblem : IProblem
```

3. **Collection Complexity**:
```csharp
// Which is correct?
List<IResult<User, ValidationProblem>>
List<IResult<User, BusinessProblem>>
List<IResult<User, Problem>> // Mixed types?
```

## Alternative Approaches

### Approach A: Extension Properties
Keep current Problem but add extensions:
```csharp
public static class ResultExtensions 
{
    public static T? GetProblemAs<T>(this IResult result) where T : class
        => result.Problem?.Extensions.GetValueOrDefault("custom") as T;
        
    public static IResult WithCustomProblem<T>(this IResult result, T customProblem)
    {
        if (result.Problem != null)
            result.Problem.Extensions["custom"] = customProblem;
        return result;
    }
}
```

### Approach B: Problem Inheritance
```csharp
public class ValidationProblem : Problem
{
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();
}

public class BusinessRuleProblem : Problem  
{
    public string RuleId { get; set; }
    public BusinessRuleContext Context { get; set; }
}
```

### Approach C: Union Types (when available in C#)
```csharp
// Future C# version
public interface IResult<T, TProblem> where TProblem : Problem or ValidationError or BusinessError
```

## Recommendation

Based on the analysis, I recommend **Approach B: Problem Inheritance** because:

1. **✅ Maintains Backward Compatibility**: All existing code works unchanged
2. **✅ Type Safety**: Custom problem types with compile-time checking  
3. **✅ JSON Serialization**: Works out of the box with existing converters
4. **✅ Simple**: Easy to understand and adopt gradually
5. **✅ Extensible**: Can add new problem types without changing Result signatures

## Implementation Example

```csharp
// Custom problem types inherit from Problem
public class ValidationProblem : Problem
{
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();
    
    public ValidationProblem(string title = "Validation failed")
    {
        Title = title;
        Type = "validation-error";
        StatusCode = 400;
    }
}

// Usage remains the same
public Result<User> CreateUser(CreateUserRequest request)
{
    var validation = ValidateUser(request);
    if (!validation.IsValid)
    {
        return Result.Factory.Fail(new ValidationProblem 
        { 
            ValidationErrors = validation.Errors 
        });
    }
    
    // Business logic...
}

// Type-safe access to custom problem
if (result.Problem is ValidationProblem validationProblem)
{
    foreach (var error in validationProblem.ValidationErrors)
    {
        Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
    }
}
```

This approach gives you the benefits of custom problem types without the complexity and breaking changes of fully generic Result types.