using SegmentCalculatorWorker.Business.Redis;
using SegmentCalculatorWorker;
using SegmentCalculatorWorker.Models.Common;
using Microsoft.Extensions.Configuration;
using SegmentCalculatorWorker.Business.SegmentCalculator;
using NLog.Web;
using NLog.Extensions.Logging;
using NLog;

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
                services.AddSingleton<ISegmentCalculatorBusiness, SegmentCalculatorBusiness>();
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