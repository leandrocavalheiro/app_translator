using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppTranslator.Utils;

public static class AppTranslatorConstants
{
    public const string EnvResourcesPath = "APP_TRANSLATOR_RESOURCES_PATH";
    public const string EnvResourcesName = "APP_TRANSLATOR_RESOURCE_NAME";
    public const string EnvListLanguages = "APP_TRANSLATOR_LIST_LANGUAGES";
    public const string EnvDefaultLanguage = "APP_TRANSLATOR_DEFAULT_LANGUAGE";
    public const string EnvDefaultContext = "APP_TRANSLATOR_DEFAULT_CONTEXT";
    public const string DefaultResourcesPath = "Locales";
    public const string DefaultResourcesName = "Locale";
    public const string DefaultListLanguages = "pt-BR";
    public const string DefaultLanguage = "pt-BR";
    public static JsonSerializerOptions GetSerializeOptions(bool formatted = true)
        => new ()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = formatted,
        }; 
}