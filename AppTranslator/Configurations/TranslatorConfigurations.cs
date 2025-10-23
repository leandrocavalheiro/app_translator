using System.Globalization;
using AppTranslator.Dtos;
using AppTranslator.Extensions;
using AppTranslator.Interfaces;
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
}