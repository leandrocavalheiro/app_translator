using System.Globalization;
using AppTranslator.Dtos;
using AppTranslator.Extensions;
using AppTranslator.Interfaces;
using AppTranslator.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AppTranslator.Configurations;

public static class TranslatorConfigurations
{
    public static IServiceCollection AddAppTranslator(
        this IServiceCollection services,
        bool isWasm = false,
        string httpClientName = AppTranslatorConstants.HttpClientName,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return services.Register(isWasm, httpClientName, serviceLifetime);
    }
    
    public static IServiceCollection AddAppTranslator(
        this IServiceCollection services, 
        IConfiguration configuration,
        bool isWasm = false,
        string httpClientName = AppTranslatorConstants.HttpClientName,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var resourcePath = configuration.GetAppTranslatorResourcesPath();
        var defaultLanguage = configuration.GetAppTranslatorDefaultLanguage();
        
        services.AddLocalization(delegate (LocalizationOptions options)
        {
            options.ResourcesPath = resourcePath;
        });
        
        services.Configure(delegate (RequestLocalizationOptions options)
        {
            CultureInfo[] languages = GetLanguages(configuration);
            options.DefaultRequestCulture = new RequestCulture(defaultLanguage);
            options.SupportedCultures = languages;
            options.SupportedUICultures = languages;
        });
        
        services.Configure(delegate (TranslatorOptions options)
        {
            options.ResourcesPath = configuration.GetAppTranslatorResourcesPath();
            options.ResourceName = configuration.GetAppTranslatorResourceName();
            options.Languages = configuration.GetAppTranslatorListLanguages();
            options.DefaultLanguage = configuration.GetAppTranslatorDefaultLanguage();
            options.DefaultContext = configuration.GetAppTranslatorDefaultContext();
            options.LoadAllLocaleResources = configuration.GetAppTranslatorLoadAllLocaleResources();
        });
        
        return services.Register(isWasm, httpClientName, serviceLifetime);
    }
    
    public static IServiceCollection AddAppTranslator(
        this IServiceCollection services, 
        TranslatorOptions translatorOptions, 
        bool isWasm = false,
        string httpClientName = AppTranslatorConstants.HttpClientName,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var resourcePath = string.IsNullOrEmpty(translatorOptions.ResourcesPath) 
            ? AppTranslatorConstants.DefaultResourcesPath 
            : translatorOptions.ResourcesPath; 
        
        var resourceName = string.IsNullOrEmpty(translatorOptions.ResourceName) 
            ? AppTranslatorConstants.DefaultResourcesName 
            : translatorOptions.ResourceName; 
        
        var defaultLanguageList = string.IsNullOrEmpty(translatorOptions.Languages) 
            ? AppTranslatorConstants.DefaultListLanguages 
            : translatorOptions.Languages;         
        
        var defaultLanguage = string.IsNullOrEmpty(translatorOptions.DefaultLanguage) 
            ? AppTranslatorConstants.DefaultLanguage 
            : translatorOptions.DefaultLanguage;
        
        var defaultContext = string.IsNullOrEmpty(translatorOptions.DefaultContext) 
            ? "" 
            : translatorOptions.DefaultContext;
        
        services.AddLocalization(delegate (LocalizationOptions options)
        {
            options.ResourcesPath = resourcePath;
        });
        
        services.Configure(delegate (RequestLocalizationOptions options)
        {
            CultureInfo[] languages = GetLanguages(translatorOptions);
            options.DefaultRequestCulture = new (defaultLanguage);
            options.SupportedCultures = languages;
            options.SupportedUICultures = languages;
            
        });
        
        services.Configure(delegate (TranslatorOptions options)
        {
            options.ResourcesPath = resourcePath;
            options.ResourceName = resourceName;
            options.Languages = defaultLanguageList;
            options.DefaultLanguage = defaultLanguage;
            options.DefaultContext = defaultContext;
            options.LoadAllLocaleResources = translatorOptions.LoadAllLocaleResources;
        });
        
        return services.Register(isWasm, httpClientName, serviceLifetime);
    }    
    
    private static CultureInfo[] GetLanguages(IConfiguration configuration)
    {
        var listLanguages = configuration.GetAppTranslatorListLanguages();
        var defaultLanguage = configuration.GetAppTranslatorDefaultLanguage();
        var array = Array.Empty<CultureInfo>();
        var array2 = listLanguages.Split(",");
        foreach (var name in array2)
            _ = array.Append(new (name));
        
        _ = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new (defaultLanguage),
            SupportedCultures = array,
            SupportedUICultures = array
        };
        return array;
    }
    
    private static CultureInfo[] GetLanguages(TranslatorOptions option)
    {
        var listLanguages = string.IsNullOrEmpty(option.Languages) 
            ? AppTranslatorConstants.DefaultListLanguages 
            : option.Languages;
        
        var defaultLanguage = string.IsNullOrWhiteSpace(option.DefaultLanguage)
            ? AppTranslatorConstants.DefaultLanguage 
            : option.DefaultLanguage;
        
        var array = Array.Empty<CultureInfo>();
        var array2 = listLanguages.Split(",");
        foreach (var name in array2)
            _ = array.Append(new (name));
        
        _ = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new (defaultLanguage),
            SupportedCultures = array,
            SupportedUICultures = array
        };
        return array;
    }
    
    private static Func<IServiceProvider, IAppTranslator> CreateAppTranslatorFactory(
        bool isWasm = false, 
        string httpClientName = AppTranslatorConstants.HttpClientName)
    {
        return sp =>
        {
            var options = sp.GetRequiredService<IOptions<TranslatorOptions>>();
        
            if (isWasm)
            {
                
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(httpClientName);

                var translator = new Implementations.AppTranslator(options, httpClient);
                // NÃO chama Initialize aqui - será feito no Program.cs
                return translator;
            }
            else
            {
                var translator = new Implementations.AppTranslator(options);
                translator.Initialize(); // Server inicializa síncrono normalmente
                return translator;
            }
        };
    }

    public static IServiceCollection Register(
        this IServiceCollection services,
        bool isWasm = false,
        string httpClientName = AppTranslatorConstants.HttpClientName,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var factory = CreateAppTranslatorFactory(isWasm, httpClientName);
    
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient(factory);
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton(factory);
                break;
            case ServiceLifetime.Scoped:
            default:
                services.AddScoped(factory);
                break;
        }

        return services;
    }
}