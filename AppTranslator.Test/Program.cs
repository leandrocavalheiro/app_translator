using AppTranslator.Configurations;
using AppTranslator.Dtos;
using AppTranslator.Test;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddAppTranslator(
    new TranslatorOptions()
    {
        ResourceName = "Locale",
        ResourcesPath = "Locales",
        Languages = "pt-BR,en-US",
        DefaultLanguage = "pt-BR",
        DefaultContext = ""
    },
    false
);

var host = builder.Build();
host.Run();