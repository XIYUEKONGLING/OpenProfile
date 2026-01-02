namespace OpenProfileServer.Models.DTOs.Core;

public class ServerInfoDto
{
    public string Version { get; set; } = string.Empty;
    public string Server { get; set; } = "OpenProfileServer";
    public bool Static { get; set; }
    public bool Dynamic { get; set; }
}