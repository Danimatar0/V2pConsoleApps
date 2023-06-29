using AccidentDetectionWorker.Business.Redis;
using AccidentDetectionWorker.Models.Common;
using AccidentDetectionWorker.Models.RedisModels;
using Helpers;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;
using MqttService.Models;
using MqttService.Service;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Business.AccidentDetection
{
    public class AccidentDetectionBusiness : IAccidentDetectionBusiness
    {
        private readonly ILogger<AccidentDetectionBusiness> _logger;
        private readonly GlobalConfig _globalConfig;
        private readonly IRedisBusiness _redisBusiness;
        private ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
        private readonly IMQTTService _mqttService;
        public AccidentDetectionBusiness(ILogger<AccidentDetectionBusiness> logger, IRedisBusiness business, IOptions<GlobalConfig> options, IMQTTService mQTTService)
        {
            _redisBusiness = business;
            _logger = logger;
            _globalConfig = options.Value;
            _mqttService = mQTTService;
        }
        //_redisBusiness.GenerateDummyHashData(_globalConfig.Constants.DevicesSegments, 2);
        public void ProcessIntersection(CancellationToken stoppingToken,IDatabase db, string intersectionId)
        {
            Console.WriteLine($"Processing {intersectionId}..");

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount > 4 ? 6 : Environment.ProcessorCount,
                CancellationToken = stoppingToken
            };


            // Get the devices for the current intersection
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();

            devices = GetDevices(db, intersectionId);

            //Populate all possible combinations, this list will be iterated over to check for any possible collision

            var collisions = new List<CollisionAtDistanceAfterTime>();
            if (devices.Count > 0)
            {
                Console.WriteLine($"Populating collision combinations for intersection: {intersectionId}..");
                _logger.LogInformation($"Populating collision combinations for intersection: {intersectionId}..");

                List<CollisionCheckCombination> combinations = CollisionHelper.PopulateCollisionCombinations(devices);

                if (combinations != null && combinations.Count > 0)
                {
                    Console.WriteLine($"Checking for collisions for intersection: {intersectionId}..");
                    _logger.LogInformation($"Checking for collisions for intersection: {intersectionId}..");

                    Parallel.ForEach(combinations, options, comb =>
                    {
                        Task.Run(async () =>
                        {
                            //Old way
                            //CollisionHelper.CheckFor2DCollisionsV1Nested(_globalConfig, _logger, comb.D1, comb.D2, ref collisions);

                            //New way
                            CollisionHelper.CheckFor2DCollisionsV2Nested(_globalConfig, _logger, comb.D1, comb.D2, ref collisions);
                        });
                    });
                }

                if (collisions.Count > 0)
                {
                    Console.WriteLine($"Detected {collisions.Count} collision(s) in intersection {intersectionId}");
                    _logger.LogWarning($"Detected {collisions.Count} collision(s) in intersection {intersectionId}");

                    ///NEEDS TO BE OPTIMIZED(messages queueing)
                    Parallel.ForEach(collisions, coll =>
                    {
                        Task.Run(async () =>
                        {
                            string payload = $"{coll.D1.Imei}:{coll.Distance1}:{coll.Time1}|{coll.D2.Imei}:{coll.Distance2}:{coll.Time2}";
                            ConnectMQTTAndPublish(_globalConfig.MqttConfig, _mqttService, payload);
                        });
                    });
                }
            }
        }
         
        public IEnumerable<string> GetIntersectionsFromRedis()
        {
            IEnumerable<string> intersections = new List<string>();

            try
            {
                return (IEnumerable<string>)_redisBusiness.GetList(_globalConfig.Constants.IntersectionIds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception while trying to fetch intersections from redis: {ex.Message}");
            }
            return intersections;
        }
        public long GetTopicsCountFromChannel()
        {
            long count = 0;
            try
            {
                count = long.Parse(_redisBusiness.StringGet("topicsCount"));
                Console.WriteLine($"Found {count} topics in system");
                //ISubscriber subscriber = _redisBusiness.Subscribe();
                //long topicsCount = default;

                //subscriber.Subscribe(_globalConfig.RedisChannels.TopicsCountChannel, (channel, message) =>
                //{
                //    Console.WriteLine("Received message: " + (string)message);

                //    if (long.TryParse((string)message, out topicsCount))
                //    {
                //        if (topicsCount != default)
                //        {
                //            count = topicsCount;
                //        }
                //        else
                //        {
                //            count = -1;
                //        }
                //    }
                //});
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception while trying to get number of topics from redis channel:{ex.Message}");
            }
            return count;

        }
        private List<KeyValuePair<string, string>> GetDevices(IDatabase db, string intersectionId)
        {
            //List<BaseCoordinate> segments = new List<BaseCoordinate>();

            string pattern = $"{intersectionId}:*";
            long cursor = 0;

            var results = new List<KeyValuePair<string, string>>();

            try
            {
                do
                {
                    //_logger.LogInformation($"Connection Established Before HashScan: {db.Multiplexer.IsConnected}");

                    //IDatabase db = _redisBusiness.GetRedisDatabase();
                    IEnumerable<HashEntry> scanResult = db.HashScan(_globalConfig.Constants.DevicesSegments, pattern, _globalConfig.RedisConfig.ScanPageSize, (int)cursor);
                    cursor = ((IScanningCursor)scanResult).Cursor;

                    foreach (var entry in scanResult)
                    {
                        var field = entry.Name;
                        var value = entry.Value;

                        KeyValuePair<string, string> pair = new KeyValuePair<string, string>(field, value);
                        results.Add(pair);
                    }
                } while (cursor != 0);
                //_redisBusiness.Disconnect(db);
                //_logger.LogInformation($"Connection Established After HashScan: {db.Multiplexer.IsConnected}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while trying to retrieve hash fields matching with pattern {pattern}, {e.Message}");
            }
            return results;
        }

        private async Task ConnectMQTTAndPublish(MqttConfig config,IMQTTService service,string payload)
        {
            string clientId = "AccidentDetector";
            string mqttURI = config.UsePublicHost ? config.PublicHostTest : config.Host;
            int mqttPort = config.Port;

            await service.StartAsync(mqttURI, clientId, string.Empty, string.Empty);
            service.PublishAsync(config.P2PChannel, payload);
        }
    }
}
