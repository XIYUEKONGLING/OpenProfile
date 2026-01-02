namespace OpenProfileServer.Models.DTOs.Common;

/// <summary>
/// A generic wrapper for all API responses containing data.
/// </summary>
/// <typeparam name="T">The type of the data being returned.</typeparam>
public class ApiResponse<T>
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(T data, string? message = null)
    {
        Status = true;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> Success(T data, string? message = null) => new(data, message);
    
    public static ApiResponse<T> Failure(string message) => new() 
    { 
        Status = false, 
        Message = message 
    };
}