using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AppTranslator.BlazorWasmTest;
using AppTranslator.Configurations;
using AppTranslator.Dtos;
using AppTranslator.Interfaces;
using AppTranslator.Utils;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient(AppTranslatorConstants.HttpClientName, client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); // Base local do WASM
});

builder
    .Services
    .AddAppTranslator(new TranslatorOptions()
    {
        ResourcesPath = "Locales",
        ResourceName = "Locale",
        Languages = "pt-BR,en-US",
        DefaultLanguage = "pt-BR",
        DefaultContext = ""
    }, 
    isWasm: true,
    serviceLifetime: ServiceLifetime.Singleton); 

var host = builder.Build();

var translator = host.Services.GetRequiredService<IAppTranslator>();
await translator.PreloadAsync(); 

Console.WriteLine("✅ Translator carregado com sucesso!");

await host.RunAsync();