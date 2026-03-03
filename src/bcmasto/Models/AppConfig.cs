namespace BcMasto.Models;

public class AppConfig
{
    public required int Port { get; set; } 
    public required string RedirectUri { get; set; }
    public required string SessionSecret { get; set; }
    public const string AppName = "BcMasto";
}
