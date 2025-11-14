using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.HostService;

class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "KvmSwitch Host Service";
        });
        
        builder.Services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddEventLog();
        });
        
        builder.Services.AddHostedService<KvmHostService>();
        
        var host = builder.Build();
        host.Run();
    }
}

