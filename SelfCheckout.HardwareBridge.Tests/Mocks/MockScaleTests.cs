using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Mocks;

public class MockScaleTests
{
    private static DeviceDescriptor ScaleDescriptor() =>
        new("test-scale", DeviceType.Scale, "Mock", "mock://scale");

    private static MockScaleOptions FastOptions() =>
        new() { ReadDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) };

    [Fact]
    public async Task GetWeightAsync_ReturnsConfiguredWeight()
    {
        var scale = new MockScale(ScaleDescriptor(), FastOptions());
        await scale.ConnectAsync();

        var result = await scale.GetWeightAsync();

        Assert.Equal(1.5m, result.Value);
        Assert.Equal("kg", result.Unit);
        Assert.True(result.IsStable);
    }

    [Fact]
    public async Task GetWeightAsync_WithCustomOptions_ReturnsCustomValues()
    {
        var options = new MockScaleOptions
        {
            DefaultWeight = 2.5m,
            Unit = "lb",
            IsStable = false,
            ReadDelay = TimeSpan.FromMilliseconds(1),
            ConnectDelay = TimeSpan.FromMilliseconds(1)
        };
        var scale = new MockScale(ScaleDescriptor(), options);
        await scale.ConnectAsync();

        var result = await scale.GetWeightAsync();

        Assert.Equal(2.5m, result.Value);
        Assert.Equal("lb", result.Unit);
        Assert.False(result.IsStable);
    }

    [Fact]
    public async Task TareAsync_ZerosWeight()
    {
        var scale = new MockScale(ScaleDescriptor(), FastOptions());
        await scale.ConnectAsync();

        await scale.TareAsync();
        var result = await scale.GetWeightAsync();

        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public async Task GetWeightAsync_WhenNotConnected_ThrowsDeviceException()
    {
        var scale = new MockScale(ScaleDescriptor(), FastOptions());

        var ex = await Assert.ThrowsAsync<DeviceException>(() => scale.GetWeightAsync());

        Assert.Equal(ErrorCode.ConnectionFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task GetWeightAsync_WithShouldFailOnRead_ThrowsDeviceException()
    {
        var options = FastOptions();
        options.ShouldFailOnRead = true;
        var scale = new MockScale(ScaleDescriptor(), options);
        await scale.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => scale.GetWeightAsync());

        Assert.Equal(ErrorCode.HardwareFailure, ex.ErrorCode);
    }
}
