using Helpers;
using Helpers.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SegmentCalculatorWorker.Business.Redis;
using SegmentCalculatorWorker.Models.Common;
using ServiceStack;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SegmentCalculatorWorker.Business.SegmentCalculator
{
    public class SegmentCalculatorBusiness : ISegmentCalculatorBusiness
    {
        private readonly ILogger<SegmentCalculatorBusiness> _logger;
        private readonly GlobalConfig _globalConfig;
        private readonly IRedisBusiness _redisBusiness;
        private ConnectionMultiplexer _redis;
        private ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();

        public SegmentCalculatorBusiness(ILogger<SegmentCalculatorBusiness> logger, IRedisBusiness business, Microsoft.Extensions.Options.IOptions<GlobalConfig> options)
        {
            _redisBusiness = business;
            _logger = logger;
            _globalConfig = options.Value;
        }

        public async void ProcessIntersection(IDatabase db, string intersectionId, CancellationToken stoppingToken)
        {
            Console.WriteLine($"Processing {intersectionId}..");

            // Get the devices for the current intersection
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();

            //this string will be used to fetch all redis keys linked to devices coordinates
            string topicPrefix = $"realData/{intersectionId}:*";

            try
            {
                //Get list of devices coordinates list keys which name starting with pattern -> topicPrefix
                IEnumerable<RedisKey> devicesCoordinatesListKeys = _redisBusiness.ScanKeys(topicPrefix);

                Console.WriteLine($"Found {devicesCoordinatesListKeys.Count()} devices coordinates list keys for pattern {topicPrefix}");

                if (devicesCoordinatesListKeys != null && devicesCoordinatesListKeys.Count() > 0)
                {
                    List<object> coordinatesPerIntersection = new List<object>();
                    //foreach (RedisKey key in devicesCoordinatesListKeys)
                    //{
                    //    var dataForTopic = _redisBusiness.GetList(key).Select(cord => $"{key};{cord}").ToList();
                    //    coordinatesPerIntersection.Add(dataForTopic);
                    //}

                    coordinatesPerIntersection.AddRange(devicesCoordinatesListKeys
                            .Select(key => _redisBusiness.GetList(key).Select(cord => $"{key};{cord}").ToList())
                            .ToList());


                    if (coordinatesPerIntersection.Count() > 0)
                    {


                        //Now using multi-threading/processing, push these coordinates to deviceSegments Hash
                        IEnumerable<IEnumerable<object>> batches = coordinatesPerIntersection.Batch<object>(_globalConfig.Constants.BatchSize);


                        Parallel.ForEach(batches, batch =>
                        {
                            var batchTasks = new List<Task>();
                            Parallel.ForEach(batch, coordinateArray =>
                            {
                                var task = Task.Run(async () => PushDeviceSegmentV2(((IEnumerable<object>)coordinateArray).ToList()));
                            });
                            //foreach (IEnumerable<object> coordinateArray in batch)
                            //{
                            //    batchTasks.Add(task);
                            //}
                            //Task.WaitAll(batchTasks.ToArray());
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }


        }

        public long GetTopicsCountFromChannel()
        {
            long count = 0;
            try
            {
                count = long.Parse(_redisBusiness.StringGet("topicsCount"));
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

        private async Task PushDeviceSegmentV2(List<object> deviceCoordinates)
        {

            if (!deviceCoordinates.IsEmpty())
            {
                //string hashKey = $"{intersectionId}:{deviceCoordinates[0].ToString().Split(":")[0] ?? ""}";
                try
                {
                    string redisKey = $"{deviceCoordinates[0].ToString().Split(";")[0].Split("/")[1]}";

                    ///Time format should be : yyyyMMddHHmmss
                    string lastCoordTime = deviceCoordinates.LastOrDefault().ToString().Split(";")[1];

                    var lastTime = DateTime.ParseExact(lastCoordTime.Split("|")[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    List<float> l1 = lastCoordTime.Split("|")[0].TrimStart('[').TrimEnd(']').Split(',')
                           .Select(s => float.Parse(s)).ToList();

                    string firstCoordTime = deviceCoordinates.FirstOrDefault().ToString().Split(";")[1];
                    var firstTime = DateTime.ParseExact(firstCoordTime.Split("|")[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    List<float> l2 = firstCoordTime.Split("|")[0].TrimStart('[').TrimEnd(']').Split(',')
                           .Select(s => float.Parse(s)).ToList();

                    Console.WriteLine($"Calculating segment for {redisKey.Split(":")[1]}");

                    // Calculate the coordinates of the segment joining the two points.
                    string segmentCoordinateString = GetSegmentCoordinates(l2, l1);
                    string segment = segmentCoordinateString.Split("|")[0];
                    float distanceTraveled = (float)Math.Round(double.Parse(segmentCoordinateString.Split("|")[1] ?? "0"));

                    // Calculate the speed.
                    var speed = distanceTraveled == 0 ? 0 : distanceTraveled / ((lastTime - firstTime).TotalSeconds);

                    string redisValue = $"{segment}:{speed}";

                    _redisBusiness.HashSetAdd(_globalConfig.Constants.DevicesSegments, redisKey, redisValue);

                    //Remove the list from redis for optimization
                    string keyToDelete = $"{deviceCoordinates[0].ToString().Split(";")[0]}";
                    _redisBusiness.ListDel(keyToDelete);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Exception while trying to push new segment to deviceSegments: {e.Message}");
                }

            }
        }

        public static string GetSegmentCoordinates(List<float> point1, List<float> point2)
        {
            var difference = point2.Select((x, i) => point2[i] - point1[i]).ToList();

            // Calculate the length of the segment.
            float length = Vector3.Distance(new Vector3(point1[0], point1[1], point1[2]), new Vector3(point2[0], point2[1], point2[2]));

            // Create a list to store the coordinates of the segment.
            var segmentCoordinates = new List<float>();

            segmentCoordinates.AddRange(CalculateLineCoordinate(point1.ToArray(), point2.ToArray()));

            Console.WriteLine($"Device Segment ==> {JsonConvert.SerializeObject(segmentCoordinates)}");
            return string.Join(",", segmentCoordinates) + "|" + length;
        }

        static float[] CalculateLineCoordinate(float[] point1, float[] point2)
        {
            // Calculate the x-coordinate of the coordinate joining the two points.
            Line line = new Line(new Point3D(point1[0], point1[1], point1[2]), new Point3D(point2[0], point2[1], point2[2]));
            float[] coords = line.ToString().Split(",").Select(x => float.Parse(x)).ToArray();

            float x = coords[0];
            float y = coords[1];
            float z = coords[2];

            // Return the coordinate.
            return new float[] { x, y, z };
        }

        private async Task PushDeviceSegmentV1(string intersectionId)
        {

            string pattern = $"{intersectionId}:*";
            long cursor = 0;


            try
            {
                do
                {

                    await Task.Run(() =>
                    {

                        Console.WriteLine("Current task id => " + Task.CurrentId);

                        IDatabase db = _redisBusiness.GetRedisDatabase();
                        IEnumerable<HashEntry> scanResult = db.HashScan(_globalConfig.Constants.DevicesCoordinates, pattern, (int)cursor);
                        cursor = ((IScanningCursor)scanResult).Cursor;

                        foreach (var entry in scanResult)
                        {
                            var field = entry.Name;
                            var value = entry.Value;

                            KeyValuePair<string, string> pair = new KeyValuePair<string, string>(field, value);

                            var currentDeviceImei = pair.Key.Split(":")[1];
                            IEnumerable<string> coordinates = new List<string>();

                            string segment = string.Empty;

                            //Calculate device segments

                            //Push segment along with intersectionId and imei to deviceSegments

                        }
                    });
                } while (cursor != 0);
                //_redisBusiness.Disconnect();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while trying to retrieve hash fields matching with pattern {pattern}, {e.Message}");
            }
        }
        public Task StopService()
        {
            throw new NotImplementedException();
        }
    }
}
