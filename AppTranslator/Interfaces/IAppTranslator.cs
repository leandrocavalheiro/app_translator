using Microsoft.Extensions.Localization;

namespace AppTranslator.Interfaces;

public interface IAppTranslator : IStringLocalizer
{
    void Initialize();

    Task PreloadAsync();

    Task SetLanguageAsync(string culture);

    void SetLanguage(string culture);

    IAppTranslator SetContext(string context);

    IAppTranslator SetFileResource(
        string path,
        string name,
        string context = null,
        string culture = null);

    string CurrentCulture { get; }
}


