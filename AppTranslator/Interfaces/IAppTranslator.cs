using Microsoft.Extensions.Localization;

namespace AppTranslator.Interfaces;

public interface IAppTranslator : IStringLocalizer
{
    void Initialize();
    Task PreloadAsync();
    Implementations.AppTranslator SetContext(string context);
    Implementations.AppTranslator SetFileResource(string path, string name, string context = null, string culture = null);
}


