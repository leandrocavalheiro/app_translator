using System.Text.Json;
using AppTranslator.Dtos;
using AppTranslator.Interfaces;
using AppTranslator.Utils;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using AppTranslator.Extensions;

namespace AppTranslator.Implementations;

public class AppTranslator : IAppTranslator
{
    private Dictionary<string, string> _localizations;
    private string _context;
    private readonly TranslatorOptions _options;

    private string _resourcePath;
    private string _resourceName;
    private string _resourceFullPath;
    private string _culture;

    public AppTranslator(IOptions<TranslatorOptions> options)
    {
        _context = options.Value.DefaultContext;
        _options = options.Value;
        _localizations = LoadLocalizations();
        SetFullPath(
            _options.ResourcesPath ?? AppTranslatorConstants.DefaultResourcesPath, 
            _options.ResourceName ?? AppTranslatorConstants.DefaultResourcesName, 
            _options.DefaultLanguage ?? AppTranslatorConstants.DefaultLanguage);
    }

    private void SetFullPath(string resourcePath, string resourceName, string culture = null)
    {
        _resourcePath = resourcePath;
        _resourceName = resourceName;
        if (!string.IsNullOrWhiteSpace(culture))
            _culture = culture;

        _resourceFullPath = Path.Combine(_resourcePath, $"{_resourceName}.{_culture}.json");
    }

    private Dictionary<string, string> LoadLocalizations()
    {
        if (_options is null || _resourceName is null)
            return [];

        var resourceFile = _resourceFullPath;
        if (!File.Exists(resourceFile))
            resourceFile = Path.Combine(_resourcePath, $"{_resourceName}.{"pt-BR"}.json");
        if (!File.Exists(resourceFile))
            resourceFile = Path.Combine(_resourcePath, $"{_resourceName}.{"en"}.json");
        if (!File.Exists(resourceFile))
            throw new FileNotFoundException($"Localization file not found: {_resourceFullPath}");

        _resourceFullPath = resourceFile;

        if (string.IsNullOrWhiteSpace(_context))
            return LoadWithoutContext();

        return LoadWithContext();
    }

    private Dictionary<string, string> LoadWithContext()
    {
        var jsonDocument = JsonDocument.Parse(File.ReadAllText(_resourceFullPath));
        if (!jsonDocument.RootElement.TryGetProperty(_context, out JsonElement contextResult))
            return [];

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(contextResult.GetRawText());
        _localizations = result;
        return result;
    }
    private Dictionary<string, string> LoadWithoutContext()
    {
        var json = File.ReadAllText(_resourceFullPath);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var result = json.Deserialize<Dictionary<string, string>>();
        _localizations = result;
        return result;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = _localizations.ContainsKey(name) ? _localizations[name] : name;
            return new (name, value);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = _localizations.ContainsKey(name) ? string.Format(_localizations[name], arguments) : name;
            return new (name, value);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _localizations.Select(l => new LocalizedString(l.Key, l.Value)).ToList();

    public AppTranslator SetFileResource(string path, string name, string context = null, string culture = null)
    {
        SetFullPath(path, name, culture);
        return SetContext(context);
    }

    public AppTranslator SetContext(string context)
    {
        _context = context;
        _ = LoadLocalizations();
        return this;
    }
}
