using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using AppTranslator.Dtos;
using AppTranslator.Interfaces;
using AppTranslator.Utils;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AppTranslator.Extensions;

namespace AppTranslator.Implementations;

public class AppTranslator : IAppTranslator
{
    // Cache estático compartilhado entre todas as instâncias
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _staticCache = new();
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    private Dictionary<string, string> _localizations;
    private string _context;
    private readonly TranslatorOptions _options;
    private string _resourcePath;
    private string _resourceName;
    private string _resourceFullPath;
    private string _culture;
    private readonly HttpClient _httpClient = null;
    private bool _isInitialized;
    private bool _isErrorOnInitialization;

    // Construtor para Server (File System)
    public AppTranslator(IOptions<TranslatorOptions> options)
    {
        _context = options.Value.DefaultContext;
        _options = options.Value;
        _httpClient = null;
        
        SetFullPath(
            _options.ResourcesPath ?? AppTranslatorConstants.DefaultResourcesPath, 
            _options.ResourceName ?? AppTranslatorConstants.DefaultResourcesName, 
            _options.DefaultLanguage ?? AppTranslatorConstants.DefaultLanguage);
    }

    // Construtor para WASM (HttpClient)
    public AppTranslator(IOptions<TranslatorOptions> options, HttpClient httpClient)
    {
        _context = options.Value.DefaultContext;
        _options = options.Value;
        _httpClient = httpClient;
        
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

    // MÉTODO ASYNC para pré-carregar (chamado no startup)
    public async Task PreloadAsync()
    {
        var cacheKey = _resourceFullPath;
        
        if (_staticCache.ContainsKey(cacheKey))
        {
            _localizations = _staticCache[cacheKey];
            _isInitialized = true;
            return;
        }

        await _cacheLock.WaitAsync();
        try
        {
            if (_staticCache.ContainsKey(cacheKey))
            {
                _localizations = _staticCache[cacheKey];
                _isInitialized = true;
                return;
            }

            _localizations = await LoadLocalizationsAsync();
            _staticCache[cacheKey] = _localizations;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    // MÉTODO SYNC para usar depois do preload
    public void Initialize()
    {
        var cacheKey = _resourceFullPath;
        
        if (_staticCache.ContainsKey(cacheKey))
        {
            _localizations = _staticCache[cacheKey];
            _isInitialized = true;
            return;
        }

        // Se não estiver no cache, carrega síncrono (apenas Server)
        if (_httpClient == null)
        {
            _localizations = LoadLocalizationsSync();
        }
        else
        {
            throw new InvalidOperationException(
                "For WASM, you must call PreloadAsync() first in Program.cs startup!");
        }
    }

    private async Task<Dictionary<string, string>> LoadLocalizationsAsync()
    {
        _isInitialized = false;
        _isErrorOnInitialization = false;

        if (_options is null || _resourceName is null)
        {
            _isErrorOnInitialization = true;
            return [];
        }

        var resourceFile = _resourceFullPath;
        
        if (_httpClient is null)
        {
            if (!File.Exists(resourceFile))
                resourceFile = Path.Combine(_resourcePath, $"{_resourceName}.pt-BR.json");
            if (!File.Exists(resourceFile))
                resourceFile = Path.Combine(_resourcePath, $"{_resourceName}.en.json");
            if (!File.Exists(resourceFile))
            {
                _isInitialized = false;
                _isErrorOnInitialization = true;            
                throw new FileNotFoundException($"Localization file not found: {_resourceFullPath}");
            }
        }

        _resourceFullPath = resourceFile;
        
        if (string.IsNullOrWhiteSpace(_context))
            return await LoadWithoutContextAsync();

        return await LoadWithContextAsync();
    }

    private Dictionary<string, string> LoadLocalizationsSync()
    {
        _isInitialized = false;
        _isErrorOnInitialization = false;

        if (_options is null || _resourceName is null)
        {
            _isErrorOnInitialization = true;
            return [];
        }

        var resourceFile = _resourceFullPath;
        
        if (!File.Exists(resourceFile))
            resourceFile = Path.Combine(_resourcePath, $"{_resourceName}.pt-BR.json");
        if (!File.Exists(resourceFile))
            resourceFile = Path.Combine(_resourcePath, $"{_resourceName}.en.json");
        if (!File.Exists(resourceFile))
        {
            _isInitialized = false;
            _isErrorOnInitialization = true;            
            throw new FileNotFoundException($"Localization file not found: {_resourceFullPath}");
        }

        _resourceFullPath = resourceFile;
        
        if (string.IsNullOrWhiteSpace(_context))
            return LoadWithoutContextSync();

        return LoadWithContextSync();
    }

    private async Task<Dictionary<string, string>> LoadWithContextAsync()
    {
        var jsonDocument = JsonDocument.Parse(await GetJsonContentAsync());
        if (!jsonDocument.RootElement.TryGetProperty(_context, out JsonElement contextResult))
        {
            _isInitialized = false;
            _isErrorOnInitialization = true;
            return [];
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(contextResult.GetRawText());
        _isInitialized = true;
        _isErrorOnInitialization = false;        
        return result;
    }

    private Dictionary<string, string> LoadWithContextSync()
    {
        var jsonDocument = JsonDocument.Parse(GetJsonContentSync());
        if (!jsonDocument.RootElement.TryGetProperty(_context, out JsonElement contextResult))
        {
            _isInitialized = false;
            _isErrorOnInitialization = true;
            return [];
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(contextResult.GetRawText());
        _isInitialized = true;
        _isErrorOnInitialization = false;        
        return result;
    }

    private async Task<Dictionary<string, string>> LoadWithoutContextAsync()
    {
        var json = await GetJsonContentAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            _isInitialized = false;
            _isErrorOnInitialization = true;            
            return [];
        }

        var result = json.Deserialize<Dictionary<string, string>>();
        _isInitialized = true;
        _isErrorOnInitialization = false;        
        return result;
    }

    private Dictionary<string, string> LoadWithoutContextSync()
    {
        var json = GetJsonContentSync();
        if (string.IsNullOrWhiteSpace(json))
        {
            _isInitialized = false;
            _isErrorOnInitialization = true;            
            return [];
        }

        var result = json.Deserialize<Dictionary<string, string>>();
        _isInitialized = true;
        _isErrorOnInitialization = false;        
        return result;
    }

    private async Task<string> GetJsonContentAsync()
    {
        if (_httpClient is null)
        {
            return File.ReadAllText(_resourceFullPath);
        }
        else
        {
            return await _httpClient.GetStringAsync(_resourceFullPath);
        }
    }

    private string GetJsonContentSync()
    {
        return File.ReadAllText(_resourceFullPath);
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (!_isInitialized)
            {
                Initialize();
                //return new LocalizedString(name, $"[NOT_INIT:{name}]");
            }

            var value = _localizations is not null && _localizations.ContainsKey(name) ? _localizations[name] : name;
            return new LocalizedString(name, value);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (!_isInitialized)
            {
                Initialize();
                //return new LocalizedString(name, $"[NOT_INIT:{name}]");
            }

            var value = _localizations.ContainsKey(name) ? string.Format(_localizations[name], arguments) : name;
            return new LocalizedString(name, value);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        if (!_isInitialized)
        {
            Initialize();
            return [];
        }

        return _localizations.Select(l => new LocalizedString(l.Key, l.Value)).ToList();
    }

    public AppTranslator SetFileResource(string path, string name, string context = null, string culture = null)
    {
        SetFullPath(path, name, culture);
        return SetContext(context);
    }

    public AppTranslator SetContext(string context)
    {
        _context = context;
        
        var cacheKey = _resourceFullPath;
        if (_staticCache.ContainsKey(cacheKey))
        {
            _localizations = _staticCache[cacheKey];
            _isInitialized = true;
        }
        else if (_httpClient == null)
        {
            _localizations = LoadLocalizationsSync();
        }
        else
        {
            throw new InvalidOperationException("Use PreloadAsync for WASM!");
        }
        
        return this;
    }
}