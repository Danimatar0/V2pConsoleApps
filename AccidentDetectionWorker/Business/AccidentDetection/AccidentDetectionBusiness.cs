
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
            List<KeyValuePair<string, string>> processed = new List<KeyValuePair<string, string>>();


            devices = GetDevices(db, intersectionId);



            if (devices.Count > 0)
            {
                //Dividing devices list into multiple batches in order to optimize the processing
                //var batches = devices.Batch(_globalConfig.Constants.BatchSize);

                // Create a list to hold the collisions
                var collisions = new List<KeyValuePair<string, string>>();

                //foreach (var device in devices)
                //{
                //    CollisionHelper.CheckFor2DCollisionsV1(_logger, devices, device, collisions);
                //}

                for (int i = 0; i < devices.Count - 1; i++)
                {
                    var d1 = devices[i];
                    for (int j = i + 1; j < devices.Count; j++)
                    {
                        var d2 = devices[j];

                        Console.WriteLine("Adding new entry:" + $"{d1.Key.Split(":")[1]},{d2.Key.Split(":")[1]}");


                        //if (processed.Contains($"{d1.Key.Split(":")[1]},{d2.Key.Split(":")[1]}") || processed.Contains($"{d2.Key.Split(":")[1]},{d1.Key.Split(":")[1]}"))
                        //{
                        //    _logger.LogWarning("These imeis has been already checked.");
                        //}
                        //else
                        //{
                        CollisionHelper.CheckFor2DCollisionsV1Nested(_logger, d1, d2, collisions);

                        //}
                    }
                }
            }
            //Parallel.ForEach(batches, batch =>
            //{
            //    // simulate processing the batch
            //    Console.WriteLine($"Processing batch [{string.Join(",", batch)}] on thread {Task.CurrentId}");
            //    CollisionHelper.CheckFor2DCollisionsV1Batch(_logger, batches.FirstOrDefault().ToList(), batch.ToList().FirstOrDefault(), collisions);
            //});

            //Console.WriteLine("Done processing batches");
            //foreach (var batch in batches)
            //{
            //    CheckFor2DCollisionsV1(batches, batch, collisions);
            //}
            ////Parallel.ForEach(batches, (d1) =>
            ////{
            ////    CheckFor2DCollisionsV1(batches, d1, collisions);
            ////});
        }

        //CheckForCollisions(devices,collisions);

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
