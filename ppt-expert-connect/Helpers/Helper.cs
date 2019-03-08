using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.ExpertConnect.Models;
using Microsoft.Extensions.Configuration;

namespace Microsoft.ExpertConnect.Helpers
{
    public class Helper
    {
        public static string GetValueFromConfiguration(IConfiguration config, string key)
        {
            return config.GetSection(key)?.Value;
        }

        public static string GetAssetLocationUrl(string url, string assetPath, string location)
        {
            return url + assetPath + location;
        }

        public static string GetPowerPointTemplateLink(string style, IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            if (string.IsNullOrEmpty(style)) { return string.Empty; }

            // TODO: Convert to relative links.
//            var url = GetValueFromConfiguration(configuration, AppSettingsKey.BotUrl);
            var url = hostingEnvironment.WebRootPath + "/";
            var assetPath = GetValueFromConfiguration(configuration, AppSettingsKey.AssetsPath);
            var pptLink = string.Empty;

            switch (style.Split(",", StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant())
            {
                case "dark":
                    pptLink = GetAssetLocationUrl(url, assetPath, "templates/dark.pptx");
                    break;
                case "light":
                    pptLink = GetAssetLocationUrl(url, assetPath, "templates/light.pptx");
                    break;
                case "colorful":
                    pptLink = GetAssetLocationUrl(url, assetPath, "templates/colorful.pptx");
                    break;
                default:
                    pptLink = GetAssetLocationUrl(url, assetPath, "templates/empty.pptx");
                    break;
            }

            return pptLink;
        }

        public static PromptValidator<string> CreateValidatorFromOptionsAsync(string[] options)
        {
            return (PromptValidatorContext<string> promptContext, CancellationToken cancellationToken) =>
            {
                var text = promptContext.Recognized.Value;
                return Task.FromResult(options.Contains(text));
            };
        }
    }
}
