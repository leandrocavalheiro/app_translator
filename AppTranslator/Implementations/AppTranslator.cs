using System.Collections.Concurrent;
using System.Text.Json;
using AppTranslator.Dtos;
using AppTranslator.Extensions;
using AppTranslator.Interfaces;
using AppTranslator.Utils;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AppTranslator.Implementations;

public class AppTranslator : IAppTranslator
{
    public event Action LanguageChanged;
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();

    private Dictionary<string, string> _localizations = [];
    private readonly TranslatorOptions _options;

    private string _context;
    private string _resourcePath;
    private string _resourceName;
    private string _culture;

    private readonly HttpClient _httpClient;

    private bool _isInitialized;

    public string CurrentCulture => _culture;

    // SERVER
    public AppTranslator(IOptions<TranslatorOptions> options)
    {
        _options = options.Value;
        _context = _options.DefaultContext;

        _resourcePath = _options.ResourcesPath ?? AppTranslatorConstants.DefaultResourcesPath;
        _resourceName = _options.ResourceName ?? AppTranslatorConstants.DefaultResourcesName;
        _culture = _options.DefaultLanguage ?? AppTranslatorConstants.DefaultLanguage;
    }

    // WASM
    public AppTranslator(IOptions<TranslatorOptions> options, HttpClient httpClient) : this(options)
    {
        _httpClient = httpClient;
    }

    private string BuildCacheKey(string culture)
        => $"{_resourcePath}/{_resourceName}.{culture}.json::{_context}";

    private string BuildFilePath(string culture)
        => Path.Combine(_resourcePath, $"{_resourceName}.{culture}.json");

    public async Task PreloadAsync()
    {
        if (_options.LoadAllLocaleResources)
        {
            var langs = (_options.Languages ?? _culture)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var lang in langs)
                await LoadLanguageAsync(lang);
        }
        else
        {
            await LoadLanguageAsync(_culture);
        }

        _isInitialized = true;
    }

    public void Initialize()
    {
        if (_httpClient != null)
            throw new InvalidOperationException("Use PreloadAsync() for WASM.");

        LoadLanguageSync(_culture);

        _isInitialized = true;
    }

    public async Task SetLanguageAsync(string culture)
    {
        if (_culture == culture)
            return;

        _culture = culture;

        if (!_cache.ContainsKey(BuildCacheKey(culture)))
            await LoadLanguageAsync(culture);

        _localizations = _cache[BuildCacheKey(culture)];
        LanguageChanged?.Invoke();
    }

    public void SetLanguage(string culture)
    {
        if (_culture == culture)
            return;

        _culture = culture;

        if (!_cache.ContainsKey(BuildCacheKey(culture)))
            LoadLanguageSync(culture);

        _localizations = _cache[BuildCacheKey(culture)];
        LanguageChanged?.Invoke();
    }

    private async Task LoadLanguageAsync(string culture)
    {
        var key = BuildCacheKey(culture);

        if (_cache.ContainsKey(key))
        {
            _localizations = _cache[key];
            return;
        }

        var json = await GetJsonAsync(culture);

        var dict = ParseJson(json);

        _cache[key] = dict;

        if (culture == _culture)
            _localizations = dict;
    }

    private void LoadLanguageSync(string culture)
    {
        var key = BuildCacheKey(culture);

        if (_cache.ContainsKey(key))
        {
            _localizations = _cache[key];
            return;
        }

        var json = File.ReadAllText(BuildFilePath(culture));

        var dict = ParseJson(json);

        _cache[key] = dict;

        if (culture == _culture)
            _localizations = dict;
    }

    private async Task<string> GetJsonAsync(string culture)
    {
        var file = BuildFilePath(culture);

        if (_httpClient == null)
            return await File.ReadAllTextAsync(file);

        return await _httpClient.GetStringAsync(file);
    }

    private Dictionary<string, string> ParseJson(string json)
    {
        if (string.IsNullOrWhiteSpace(_context))
            return json.Deserialize<Dictionary<string, string>>();

        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty(_context, out var ctx))
            return [];

        return JsonSerializer.Deserialize<Dictionary<string, string>>(ctx.GetRawText())!;
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Translator not initialized.");

            return _localizations.TryGetValue(name, out var value)
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Translator not initialized.");

            if (_localizations.TryGetValue(name, out var value))
                return new LocalizedString(name, string.Format(value, arguments));

            return new LocalizedString(name, name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        if (!_isInitialized)
            return [];

        return _localizations.Select(x => new LocalizedString(x.Key, x.Value));
    }

    public IAppTranslator SetContext(string context)
    {
        _context = context;

        if (_cache.ContainsKey(BuildCacheKey(_culture)))
            _localizations = _cache[BuildCacheKey(_culture)];

        return this;
    }

    public IAppTranslator SetFileResource(string path, string name, string context = null, string culture = null)
    {
        _resourcePath = path;
        _resourceName = name;

        if (context != null)
            _context = context;

        if (culture != null)
            _culture = culture;

        return this;
    }
}