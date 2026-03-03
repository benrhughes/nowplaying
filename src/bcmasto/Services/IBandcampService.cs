using BcMasto.Models;

namespace BcMasto.Services;

public interface IBandcampService
{
    Task<ScrapeResponse> ScrapeAsync(string url);
}
