using Ganss.Xss;
using Microsoft.Extensions.Options;

namespace UncAds.Services
{
    public class HtmlSanitizationService : IHtmlSanitizationService
    {
        private readonly IHtmlSanitizer _sanitizer;

        public HtmlSanitizationService(IOptions<HtmlSanitizerSettings> settings)
        {
            _sanitizer = new HtmlSanitizer();

            _sanitizer.AllowedTags.Clear();
            foreach (var tag in settings.Value.AllowedTags)
            {
                _sanitizer.AllowedTags.Add(tag.ToLowerInvariant());
            }
        }

        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            return _sanitizer.Sanitize(html);
        }
    }
}