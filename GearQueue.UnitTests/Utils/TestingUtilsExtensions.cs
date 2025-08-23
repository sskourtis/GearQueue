namespace GearQueue.UnitTests.Utils;

public static class TestingUtilsExtensions
{
    public static bool IsEquivalent<T>(this T[]? first, T[]? second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first is null || second is null)
        {
            return false;
        }

        if (first.Length != second.Length)
        {
            return false;
        }

        for (var i = 0; i < first.Length; i++)
        {
            if (!first[i]!.Equals(second[i]))
            {
                return false;
            }
        }

        return true;
    }
}