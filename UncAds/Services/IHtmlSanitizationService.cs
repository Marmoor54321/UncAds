namespace UncAds.Services
{
    public interface IHtmlSanitizationService
    {
        string Sanitize(string html);
    }
}