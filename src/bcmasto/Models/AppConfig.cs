namespace BcMasto.Models;

public class AppConfig
{
    public const string AppName = "BcMasto";

    public required int Port { get; set; }

    public required string RedirectUri { get; set; }

    public required string SessionSecret { get; set; }
}
