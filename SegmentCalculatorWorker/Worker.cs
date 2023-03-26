using Microsoft.Extensions.Options;
using SegmentCalculatorWorker.Business.SegmentCalculator;
using SegmentCalculatorWorker.Models.Common;

namespace SegmentCalculatorWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly GlobalConfig _globalConfig;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory, IOptions<GlobalConfig> options)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _globalConfig = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");
                    //Console.WriteLine($"Worker running at: {DateTimeOffset.Now}");
                    ISegmentCalculatorBusiness business = scope.ServiceProvider.GetService<ISegmentCalculatorBusiness>();
                    await business.StartService();
                }
                await Task.Delay(TimeSpan.FromSeconds(_globalConfig.Constants.RunJobEvery), stoppingToken);
            }
        }
    }
}