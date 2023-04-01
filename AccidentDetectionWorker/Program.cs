using AccidentDetectionWorker.Business.Redis;
using AccidentDetectionWorker;
using AccidentDetectionWorker.Business.AccidentDetection;
using AccidentDetectionWorker.Models.Common;
using Microsoft.Extensions.Configuration;
using Serilog;
using AccidentDetectionWorker.Models.RedisModels.RedisDatabase;

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
                services.AddSingleton<IRedisDatabase,RedisDatabase>();
                services.AddSingleton<IRedisBusiness, RedisBusiness>();
                services.AddSingleton<IAccidentDetectionBusiness, AccidentDetectionBusiness>();
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