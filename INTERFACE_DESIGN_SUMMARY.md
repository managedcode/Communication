# Result Interface Design Summary

## Overview

This document outlines the comprehensive interfaces designed to standardize Result classes in the ManagedCode.Communication library. The interfaces provide consistent validation properties, JSON serialization attributes, and factory methods across all Result types.

## New Interfaces Created

### 1. IResultBase
**Location**: `/ManagedCode.Communication/IResultBase.cs`

The foundational interface that provides comprehensive validation properties and JSON serialization attributes:

**Key Features:**
- Standardized JSON property naming and ordering
- Complete validation property set (IsSuccess, IsFailed, IsValid, IsInvalid, IsNotInvalid, HasProblem)
- JsonIgnore attributes for computed properties
- InvalidObject property for JSON serialization of validation errors
- Field-specific validation methods (InvalidField, InvalidFieldError)

**Properties:**
- `IsSuccess` - JSON serialized as "isSuccess" (order: 1)
- `IsFailed` - Computed property (JsonIgnore)
- `IsValid` - Computed property: IsSuccess && !HasProblem (JsonIgnore)
- `IsNotInvalid` - Computed property: !IsInvalid (JsonIgnore)
- `InvalidObject` - Validation errors dictionary (conditionally ignored when null)
- `InvalidField(string)` - Method to check field-specific validation errors
- `InvalidFieldError(string)` - Method to get field-specific error messages

### 2. IResultValue<T>
**Location**: `/ManagedCode.Communication/IResultValue.cs`

Interface for results containing a value of type T:

**Key Features:**
- Extends IResultBase for all validation properties
- Standardized Value property with JSON attributes
- IsEmpty/HasValue properties with proper null checking attributes

**Properties:**
- `Value` - JSON serialized as "value" (order: 2), ignored when default
- `IsEmpty` - Computed property with MemberNotNullWhen attribute
- `HasValue` - Computed property: !IsEmpty

### 3. IResultCollection<T>
**Location**: `/ManagedCode.Communication/IResultCollection.cs`

Interface for results containing collections with pagination support:

**Key Features:**
- Extends IResultBase for all validation properties
- Comprehensive pagination properties with JSON serialization
- Collection-specific properties (HasItems, Count, etc.)
- Navigation properties (HasPreviousPage, HasNextPage, IsFirstPage, IsLastPage)

**Properties:**
- `Collection` - JSON serialized as "collection" (order: 2)
- `HasItems` - Computed property (JsonIgnore)
- `IsEmpty` - Computed property (JsonIgnore)
- `PageNumber` - JSON serialized as "pageNumber" (order: 3)
- `PageSize` - JSON serialized as "pageSize" (order: 4)
- `TotalItems` - JSON serialized as "totalItems" (order: 5)
- `TotalPages` - JSON serialized as "totalPages" (order: 6)
- Navigation properties (all JsonIgnore): HasPreviousPage, HasNextPage, Count, IsFirstPage, IsLastPage

### 4. IResultFactory
**Location**: `/ManagedCode.Communication/IResultFactory.cs`

Comprehensive interface for standardized factory methods:

**Method Categories:**
- **Basic Success Methods**: Succeed(), Succeed<T>(T), Succeed<T>(Action<T>)
- **Basic Failure Methods**: Fail(), Fail(Problem), Fail(string), Fail(string, string), Fail(Exception)
- **Generic Failure Methods**: Fail<T>() variants for typed results
- **Validation Failure Methods**: FailValidation() for both Result and Result<T>
- **HTTP Status Specific Methods**: FailBadRequest(), FailUnauthorized(), FailForbidden(), FailNotFound()
- **Enum-based Failure Methods**: Fail<TEnum>() variants with custom error codes
- **From Methods**: From(bool), From(IResultBase), From(Task<Result>) for converting various inputs to results

## Updated Existing Interfaces

### Updated IResult
- Now inherits from IResultBase for backward compatibility
- Maintains existing interface name while providing comprehensive functionality

### Updated IResult<T>
- Now inherits from both IResult and IResultValue<T>
- Provides full functionality while maintaining backward compatibility


## Design Principles

1. **Backward Compatibility**: All existing interfaces remain unchanged in their public API
2. **Comprehensive Validation**: All interfaces include complete validation property sets
3. **JSON Standardization**: Consistent property naming, ordering, and ignore conditions
4. **Null Safety**: Proper use of MemberNotNullWhen and nullable reference types
5. **Factory Standardization**: Complete coverage of all factory method patterns used in existing code
6. **Documentation**: Comprehensive XML documentation for all properties and methods

## Benefits

1. **Consistency**: Standardized validation properties across all Result types
2. **Type Safety**: Proper null checking and member validation attributes
3. **JSON Compatibility**: Consistent serialization behavior across all result types
4. **Developer Experience**: Comprehensive IntelliSense support and clear documentation
5. **Testing**: Factory interface enables easy mocking and testing scenarios
6. **Maintainability**: Single source of truth for Result interface contracts

## Integration Notes

- All existing Result classes continue to work without modifications
- New interfaces provide enhanced functionality through inheritance
- Build and all tests pass, confirming no breaking changes
- Interfaces can be implemented by custom result types for consistency
- Factory interface can be used for dependency injection scenarios

## JSON Serialization Schema

The interfaces ensure consistent JSON output:

```json
{
  "isSuccess": true|false,
  "value": <any> | "collection": [<items>],
  "pageNumber": <number>,     // Collection results only
  "pageSize": <number>,       // Collection results only  
  "totalItems": <number>,     // Collection results only
  "totalPages": <number>,     // Collection results only
  "problem": { ... }          // When present
}
```

All computed properties (IsFailed, IsValid, HasItems, etc.) are excluded from JSON serialization via JsonIgnore attributes.