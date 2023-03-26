using SegmentCalculatorWorker.Business.Redis;
using SegmentCalculatorWorker;
using SegmentCalculatorWorker.Models.Common;
using Microsoft.Extensions.Configuration;
using Serilog;
using SegmentCalculatorWorker.Business.SegmentCalculator;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(ctx.Configuration.GetSection("Serilog"))
                        .Enrich.FromLogContext()
                        .CreateLogger();

                services.Configure<GlobalConfig>(ctx.Configuration.GetSection("GlobalConfig"));
                services.AddHostedService<Worker>();
                services.AddSingleton<IRedisBusiness, RedisBusiness>();
                services.AddSingleton<ISegmentCalculatorBusiness, SegmentCalculatorBusiness>();
            })
            .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host builder error");

            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}