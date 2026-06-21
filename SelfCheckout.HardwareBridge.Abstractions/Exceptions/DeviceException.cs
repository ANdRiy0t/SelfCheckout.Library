using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Abstractions.Exceptions;

public class DeviceException : Exception
{

    public string DeviceId { get; }

    public DeviceType DeviceType { get; }

    public ErrorCode ErrorCode { get; }

    public DeviceException(
        string message,
        string deviceId,
        DeviceType deviceType,
        ErrorCode errorCode,
        Exception? innerException = null)
        : base(message, innerException)
    {
        DeviceId = deviceId;
        DeviceType = deviceType;
        ErrorCode = errorCode;
    }
}
