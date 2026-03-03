namespace BcMasto.Models;

public class AppConfig
{
    public int Port { get; set; } = 5000;
    public string RedirectUri { get; set; } = "http://localhost:5000/auth/callback";
    public string SessionSecret { get; set; } = "dev-secret-change-in-production";
    public const string AppName = "BcMasto";
}
