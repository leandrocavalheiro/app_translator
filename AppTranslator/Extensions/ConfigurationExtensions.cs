using System.Globalization;
using AppTranslator.Utils;
using Microsoft.Extensions.Configuration;

namespace AppTranslator.Extensions;

public static class ConfigurationExtensions
{
    public static string ValueOrDefault(this IConfiguration value, string key, string defaultValue)
    {
        var valueKey = value[key];
        return string.IsNullOrWhiteSpace(valueKey) 
            ? defaultValue 
            : valueKey;
    }
    
    #region Translator
    public static string GetAppTranslatorResourcesPath(this IConfiguration configuration)
        => configuration.ValueOrDefault(
            AppTranslatorConstants.EnvResourcesPath, 
            AppTranslatorConstants.DefaultResourcesPath);
    
    public static string GetAppTranslatorResourceName(this IConfiguration configuration)
        => configuration.ValueOrDefault(
            AppTranslatorConstants.EnvResourcesName, 
            AppTranslatorConstants.DefaultResourcesName);
    
    public static string GetAppTranslatorListLanguages(this IConfiguration configuration)    
        => configuration.ValueOrDefault(
            AppTranslatorConstants.EnvListLanguages, 
            AppTranslatorConstants.DefaultListLanguages);
    
    public static string GetAppTranslatorDefaultLanguage(this IConfiguration configuration)
    {
        var text = configuration.ValueOrDefault(
            AppTranslatorConstants.EnvDefaultLanguage, 
            CultureInfo.CurrentUICulture.Name);
        if (string.IsNullOrWhiteSpace(text))       
            text = AppTranslatorConstants.DefaultLanguage;        
        return text;
    }
    public static string GetAppTranslatorDefaultContext(this IConfiguration configuration)    
        => configuration.ValueOrDefault(
            AppTranslatorConstants.EnvDefaultContext, 
            "");
    #endregion
}