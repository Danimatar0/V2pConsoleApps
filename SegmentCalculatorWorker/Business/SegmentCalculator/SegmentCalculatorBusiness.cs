using Microsoft.Extensions.Options;
using SegmentCalculatorWorker.Business.Redis;
using SegmentCalculatorWorker.Models.Common;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public SegmentCalculatorBusiness(ILogger<SegmentCalculatorBusiness> logger, IRedisBusiness business, IOptions<GlobalConfig> options)
        {
            _redisBusiness = business;
            _logger = logger;
            _globalConfig = options.Value;
        }

        public void ProcessIntersection(IDatabase db, string intersectionId)
        {
            Console.WriteLine($"Processing {intersectionId}..");

            // Get the devices for the current intersection
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();


        }
        public async Task StartService()
        {
            //_logger.LogInformation("Starting segment calculator service");
            //_redisBusiness.Connect();
            //long topicsCount = GetTopicsCountFromChannel();

            ////_redisBusiness.GenerateDummyHashData(_globalConfig.Constants.DevicesSegments,100);
            ////_redisBusiness.GenerateDummyDeviceCoordinateStringPair("HXbW5", 50);
            //if (topicsCount > 0)
            //{
            //    StartProcesses(topicsCount);
            //}
        }
        public async Task StartProcesses(long topicsCount)
        {
            var stopwatch = Stopwatch.StartNew();

            if (topicsCount > 0)
            {
                try
                {
                    for (long i = 0; i < topicsCount; i++)
                    {
                        //Getting intersection id
                        string intersectionId = _redisBusiness.ListGet(_globalConfig.Constants.IntersectionIds, i);




                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception while trying to start process: {ex.Message}, caused by : {ex.InnerException}");
                }
            }

            // Stop the stopwatch and print the time taken
            stopwatch.Stop();
            Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
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
        private async Task PushDeviceSegment(string intersectionId)
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
