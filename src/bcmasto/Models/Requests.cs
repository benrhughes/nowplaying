namespace BcMasto.Models;

public record RegisterRequest(string Instance);

public record ScrapeRequest(string Url);

public record PostRequest(string Text, string ImageUrl, string? AltText = null);
