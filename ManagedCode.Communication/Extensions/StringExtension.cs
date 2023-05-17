using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication.Extensions;

internal static class StringExtension
{
    public static string JoinFilter(string separator, IEnumerable<string> strings)
    {
        return string.Join(separator, strings.Where(s => !string.IsNullOrEmpty(s)));
    }

    public static string JoinFilter(char separator, IEnumerable<string> strings)
    {
        return string.Join(separator, strings.Where(s => !string.IsNullOrEmpty(s)));
    }

    public static string JoinFilter(string separator, params string[] str)
    {
        return string.Join(separator, str.Where(s => !string.IsNullOrEmpty(s)));
    }

    public static string JoinFilter(char separator, params string?[] str)
    {
        return string.Join(separator, str.Where(s => !string.IsNullOrEmpty(s)));
    }
}