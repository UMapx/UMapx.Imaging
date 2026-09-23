using Xunit;

namespace UMapx.Tests;

internal static class NumericAssert
{
    internal static void Close(double expected, double actual, double absolute = 2e-6, double relative = 2e-5)
    {
        double tolerance = absolute + relative * Math.Abs(expected);
        Assert.True(double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance,
            FormattableString.Invariant($"Expected {expected:G17}; actual {actual:G17}; tolerance {tolerance:G6}."));
    }

    internal static void Close(float[] expected, float[] actual, double tolerance = 1e-4)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Close(expected[i], actual[i], tolerance, 0);
    }
}
