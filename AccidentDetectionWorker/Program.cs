using AccidentDetectionWorker.Business.Redis;
using AccidentDetectionWorker;
using AccidentDetectionWorker.Business.AccidentDetection;
using AccidentDetectionWorker.Models.Common;
using Microsoft.Extensions.Configuration;
using NLog.Web;
using NLog;
using NLog.Extensions.Logging;
using NLog.Fluent;
using MqttService.Service;
using MqttService.Models;

class Program
{
    static async Task Main(string[] args)
    {
        var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
        try
        {

            IHost host = Host.CreateDefaultBuilder(args)
                            .ConfigureLogging((logging) =>
                            {
                                logging.ClearProviders();
                                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                                logging.AddNLog();
                            }).UseNLog()
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<GlobalConfig>(ctx.Configuration.GetSection("GlobalConfig"));
                services.AddHostedService<Worker>();
                services.AddSingleton<IRedisBusiness, RedisBusiness>();
                services.AddSingleton<IAccidentDetectionBusiness, AccidentDetectionBusiness>();
                services.AddTransient<IMQTTService, MQTTService>();
            })
            .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message, "Host builder error");
            throw;
        }
        finally
        {
            LogManager.Flush();
        }
    }
}