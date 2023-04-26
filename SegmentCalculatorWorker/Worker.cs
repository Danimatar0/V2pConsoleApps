using Microsoft.Extensions.Options;
using SegmentCalculatorWorker.Business.Redis;
using SegmentCalculatorWorker.Business.SegmentCalculator;
using SegmentCalculatorWorker.Models.Common;
using StackExchange.Redis;
using System.Diagnostics;

namespace SegmentCalculatorWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly GlobalConfig _globalConfig;
        private readonly IRedisBusiness _redisBusiness;

        public Worker(ILogger<Worker> logger, IRedisBusiness business, IServiceScopeFactory serviceScopeFactory, IOptions<GlobalConfig> options)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _globalConfig = options.Value;
            _redisBusiness = business;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var stopwatch = Stopwatch.StartNew();

                _logger.LogInformation("Starting Segment Calculator Worker..");

                try
                {
                    int numberOfProcessors = Environment.ProcessorCount;
                    var options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = numberOfProcessors > 4 ? 6 : numberOfProcessors,
                        CancellationToken = stoppingToken
                    };

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {

                        //Getting injected IAccidentDetectionBusiness instance
                        ISegmentCalculatorBusiness business = scope.ServiceProvider.GetService<ISegmentCalculatorBusiness>();

                        //Fetching list of intersections names
                        List<object> intersections = (List<object>)_redisBusiness.GetList(_globalConfig.Constants.IntersectionIds);

                        _logger.LogInformation($"Found {intersections.Count} intersections in redis");
                        if (intersections.Count > 0)
                        {
                            //Parallel.ForEach to iterate over the list of intersections in parallel
                            await Task.Run(() =>
                            {
                                Parallel.ForEach(intersections, options, item =>
                                {
                                    _logger.LogInformation("Initiating new redis connection..");
                                    IDatabase db = _redisBusiness.GetRedisDatabase();

                                    business.ProcessIntersection(db, item.ToString());

                                    _redisBusiness.Disconnect(db);
                                });
                            }, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception while trying to start worker service: {ex.Message}");
                }
                stopwatch.Stop();
                Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");

                await Task.Delay(TimeSpan.FromSeconds(_globalConfig.Constants.RunJobEvery), stoppingToken);
            }
        }
    }
}