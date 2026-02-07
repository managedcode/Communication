using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Merges multiple results and returns the first failure, or success when all results succeed.
    /// </summary>
    public static Result Merge(params Result[] results)
    {
        return AdvancedRailwayExtensions.Merge(results);
    }

    /// <summary>
    ///     Merges multiple results and aggregates all failures.
    /// </summary>
    public static Result MergeAll(params Result[] results)
    {
        return AdvancedRailwayExtensions.MergeAll(results);
    }

    /// <summary>
    ///     Combines successful result values into a collection result.
    /// </summary>
    public static CollectionResult<T> Combine<T>(params Result<T>[] results)
    {
        return AdvancedRailwayExtensions.Combine(results);
    }

    /// <summary>
    ///     Combines successful values or aggregates failures into a collection result.
    /// </summary>
    public static CollectionResult<T> CombineAll<T>(params Result<T>[] results)
    {
        return AdvancedRailwayExtensions.CombineAll(results);
    }
}
