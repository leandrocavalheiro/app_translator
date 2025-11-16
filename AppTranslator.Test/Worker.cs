using AppTranslator.Interfaces;

namespace AppTranslator.Test;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAppTranslator _translator;

    public Worker(ILogger<Worker> logger, IAppTranslator translator)
    {
        _logger = logger;
        _translator = translator;       
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //await Task.Delay(10);
        
        
        Console.WriteLine(_translator["hello"]);
        
        
    }
}