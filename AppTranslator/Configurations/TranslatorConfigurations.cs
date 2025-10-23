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

namespace AppTranslator.Configurations;

public static class TranslatorConfigurations
{
    public static IServiceCollection AddAppTranslator(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return serviceLifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient<IAppTranslator, Implementations.AppTranslator>(),
            ServiceLifetime.Singleton => services.AddSingleton<IAppTranslator, Implementations.AppTranslator>(),
            _ => services.AddScoped<IAppTranslator, Implementations.AppTranslator>()
        };
    }
    public static IServiceCollection AddAppTranslator(this IServiceCollection services, IConfiguration configuration, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        string resourcePath = configuration.GetAppTranslatorResourcesPath();
        string defaultLanguage = configuration.GetAppTranslatorDefaultLanguage();
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
        });
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient<IAppTranslator, Implementations.AppTranslator>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IAppTranslator, Implementations.AppTranslator>();
                break;
            default:
                services.AddScoped<IAppTranslator, Implementations.AppTranslator>();
                break;
        }
        return services;
    }
    public static IServiceCollection AddAppTranslator(this IServiceCollection services, TranslatorOptions translatorOptions, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
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
        });
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient<IAppTranslator, Implementations.AppTranslator>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IAppTranslator, Implementations.AppTranslator>();
                break;
            default:
                services.AddScoped<IAppTranslator, Implementations.AppTranslator>();
                break;
        }
        return services;
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
}