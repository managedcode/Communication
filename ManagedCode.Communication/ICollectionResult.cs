namespace ManagedCode.Communication;

/// <summary>
/// Defines a contract for a result that contains a collection of items.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public interface ICollectionResult<out T> : IResult
{
    /// <summary>
    /// Gets the collection of items.
    /// </summary>
    /// <value>The collection of items, or null if the result does not contain a collection.</value>
    T[]? Collection { get; }

    /// <summary>
    /// Gets a value indicating whether the collection is empty.
    /// </summary>
    /// <value>true if the collection is empty; otherwise, false.</value>
    bool IsEmpty { get; }
}