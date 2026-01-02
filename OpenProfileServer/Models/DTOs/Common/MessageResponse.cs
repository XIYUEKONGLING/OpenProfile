namespace OpenProfileServer.Models.DTOs.Common;

/// <summary>
/// A simple response used when only a message is required.
/// </summary>
public class MessageResponse
{
    public string? Message { get; set; }

    public MessageResponse() { }

    public MessageResponse(string? message)
    {
        Message = message;
    }

    public static MessageResponse Create(string? message) => new(message);
}