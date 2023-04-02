
using AccidentDetectionWorker.Business.Redis;
using AccidentDetectionWorker.Models.Common;
using AccidentDetectionWorker.Models.RedisModels;
using Helpers;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack;
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
        public AccidentDetectionBusiness(ILogger<AccidentDetectionBusiness> logger, IRedisBusiness business, Microsoft.Extensions.Options.IOptions<GlobalConfig> options)
        {
            _redisBusiness = business;
            _logger = logger;
            _globalConfig = options.Value;
        }
        //_redisBusiness.GenerateDummyHashData(_globalConfig.Constants.DevicesSegments, 2);
        public void ProcessIntersection(IDatabase db, string intersectionId)
        {
            Console.WriteLine($"Processing {intersectionId}..");

            // Get the devices for the current intersection
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();

            devices = GetDevices(db, intersectionId);

            //_logger.LogInformation($"Found {devices.Count} in intersection {intersectionId}");
            //Populate all possible combinations, this list will be iterated over to check for any possible collision

            var collisions = new List<CollisionAtDistanceAfterTime> ();
            if (devices.Count > 0)
            {
                List<CollisionCheckCombination> combinations = CollisionHelper.PopulateCollisionCombinations(devices);

                if (combinations != null && combinations.Count > 0)
                {
                    foreach (CollisionCheckCombination comb in combinations)
                    {
                        CollisionHelper.CheckFor2DCollisionsV1Nested(_globalConfig,_logger, comb.D1, comb.D2, collisions);
                    }
                }
            }

            _logger.LogWarning($"Detected {collisions.Count} collision(s) in intersection {intersectionId}");

            ///Here I will be propagating the results to MQTT BROKER -> Channel = tracking/intersectionId
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

    }
}
