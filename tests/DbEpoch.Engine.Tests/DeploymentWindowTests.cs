using DbEpoch.Core.ValueObjects;
using Xunit;

namespace DbEpoch.Engine.Tests;

public class DeploymentWindowTests
{
    private static DeploymentWindow Window(string start, string end) => new()
    {
        Enabled = true,
        StartTime = start,
        EndTime = end,
        AllowedDays = Array.Empty<string>()
    };

    [Fact]
    public void IsWithinWindow_InsideNormalWindow_ReturnsTrue()
    {
        var window = Window("09:00", "17:00");
        Assert.True(window.IsWithinWindow(new DateTime(2026, 8, 13, 12, 30, 0), out _));
    }

    [Fact]
    public void IsWithinWindow_OutsideNormalWindow_ReturnsFalse()
    {
        var window = Window("09:00", "17:00");
        Assert.False(window.IsWithinWindow(new DateTime(2026, 8, 13, 18, 0, 0), out var reason));
        Assert.Contains("outside", reason);
    }

    [Fact]
    public void IsWithinWindow_OvernightWindow_AfterMidnightIsInside()
    {
        var window = Window("22:00", "04:00");
        Assert.True(window.IsWithinWindow(new DateTime(2026, 8, 13, 2, 30, 0), out _));
    }

    [Fact]
    public void IsWithinWindow_OvernightWindow_EveningIsInside()
    {
        var window = Window("22:00", "04:00");
        Assert.True(window.IsWithinWindow(new DateTime(2026, 8, 13, 23, 0, 0), out _));
    }

    [Fact]
    public void IsWithinWindow_OvernightWindow_MiddayIsOutside()
    {
        var window = Window("22:00", "04:00");
        Assert.False(window.IsWithinWindow(new DateTime(2026, 8, 13, 12, 0, 0), out var reason));
        Assert.Contains("outside", reason);
    }

    [Fact]
    public void IsWithinWindow_AllowedDays_Enforced()
    {
        var window = Window("00:00", "23:59");
        window.AllowedDays = new[] { "Monday" };

        // 2026-08-13 is a Thursday.
        Assert.False(window.IsWithinWindow(new DateTime(2026, 8, 13, 12, 0, 0), out var reason));
        Assert.Contains("Monday", reason);
        Assert.True(window.IsWithinWindow(new DateTime(2026, 8, 10, 12, 0, 0), out _));
    }

    [Fact]
    public void IsWithinWindow_MissingAllowedDay_DefaultsToTrue()
    {
        var window = Window("00:00", "23:59");
        Assert.True(window.IsWithinWindow(new DateTime(2026, 8, 13, 12, 0, 0), out _));
    }
}