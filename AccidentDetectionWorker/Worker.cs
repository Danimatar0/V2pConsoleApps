using AccidentDetectionWorker.Business.AccidentDetection;
using AccidentDetectionWorker.Business.Redis;
using AccidentDetectionWorker.Models.Common;
using AccidentDetectionWorker.Models.RedisModels.RedisDatabase;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

namespace AccidentDetectionWorker
{
    public sealed class Worker : BackgroundService
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

                _logger.LogInformation("Starting Accident Detection Worker..");

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
                        IAccidentDetectionBusiness business = scope.ServiceProvider.GetService<IAccidentDetectionBusiness>();

                        _logger.LogInformation("Fetching intersections..");

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

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    using (var scope = _serviceScopeFactory.CreateScope())
            //    {
            //        _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");
            //        //Console.WriteLine($"Worker running at: {DateTimeOffset.Now}");
            //        IAccidentDetectionBusiness business = scope.ServiceProvider.GetService<IAccidentDetectionBusiness>();
            //        await business.StartService();
            //    }
            //    await Task.Delay(TimeSpan.FromSeconds(_globalConfig.Constants.RunJobEvery), stoppingToken);
            //}
        }
    }
}