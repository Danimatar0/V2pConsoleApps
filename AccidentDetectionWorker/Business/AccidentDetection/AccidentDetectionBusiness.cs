using AccidentDetectionWorker.Business.Redis;
using AccidentDetectionWorker.Models.Common;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
        public AccidentDetectionBusiness(ILogger<AccidentDetectionBusiness> logger, IRedisBusiness business, IOptions<GlobalConfig> options)
        {
            _redisBusiness = business;
            _logger = logger;
            _globalConfig = options.Value;
        }

        public async Task StartService()
        {
            _logger.LogInformation("Starting accident detection service");
            _redisBusiness.Connect();
            long topicsCount = GetTopicsCountFromChannel();

            //_redisBusiness.GenerateDummyHashData(_globalConfig.Constants.DevicesSegments, 100);
            if (topicsCount > 0)
            {
                StartProcesses(topicsCount);
            }
        }

        /*
        public void StartProcesses(long topicsCount)
        {
            if (topicsCount > 0)
            {
                for (long i = 0; i < topicsCount; i++)
                {
                    try
                    {
                        string intersectionId = _redisBusiness.ListGet(_globalConfig.Constants.IntersectionIds, i);
                        tasks.Add(Task.Factory.StartNew(() =>
                        {
                            // Get the devices for the current intersection
                            List<KeyValuePair<string, string>> devices = GetDevices(intersectionId);

                            if (devices != null && devices.Count > 0)
                            {
                                // Check for potential accidents or collisions in each road intersection
                                CheckForCollisions(devices, taskId: Task.CurrentId?? 0);
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception while trying to start process: {ex.Message}, caused by : {ex.InnerException}");
                    }
                }
            }
        }
        */

        public async Task StartProcesses(long topicsCount)
        {
            var stopwatch = Stopwatch.StartNew();
            if (topicsCount > 0)
            {
                try
                {
                    int numberOfProcessors = Environment.ProcessorCount;
                    var options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = numberOfProcessors > 4 ? 6 : numberOfProcessors
                    };

                    for (long i = 0; i < topicsCount; i++)
                    {
                        //Getting intersection id
                        string intersectionId = _redisBusiness.ListGet(_globalConfig.Constants.IntersectionIds, i);

                        // Get the devices for the current intersection
                        List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();

                        // Create a list to hold the collisions
                        var collisions = new List<KeyValuePair<string, string>>();

                        //if (devices != null && devices.Count > 0)
                        //{
                        await Task.Run(() =>
                        {
                            devices = GetDevices(intersectionId);
                            Console.WriteLine("Current task id => " + Task.CurrentId);

                            if (devices.Count > 0)
                            {
                                Parallel.ForEach(devices, (d1) =>
                                {
                                    CheckForCollisions(devices, d1, collisions);
                                });
                            }
                            //CheckForCollisions(devices,collisions);
                        });
                        //}

                        // Output the collisions for the current intersection
                        Console.WriteLine($"Collisions in intersection {i}:");
                        foreach (var collision in collisions)
                        {
                            Console.WriteLine($"Collision between {collision.Key} and {collision.Value}");
                        }
                        Console.WriteLine();
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
        public Task StopService()
        {
            return Task.CompletedTask;
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
        private List<KeyValuePair<string, string>> GetDevices(string intersectionId)
        {
            //List<BaseCoordinate> segments = new List<BaseCoordinate>();

            string pattern = $"{intersectionId}:*";
            long cursor = 0;

            var results = new List<KeyValuePair<string, string>>();

            try
            {
                //do
                //{
                //IEnumerable<HashEntry> scanResult = _redisBusiness.HashScan(_globalConfig.Constants.DevicesSegments, pattern, cursor);

                ////cursor = scanResult.Count();
                //foreach (var entry in scanResult.ToList())
                //{
                //    var field = entry.Name;
                //    var value = entry.Value;

                //    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(field, value);
                //    //_logger.LogInformation($"Adding new kv pair to devices result: {pair}");
                //    results.Add(pair);
                //}
                ////} while (cursor != 0);
                do
                {
                    IDatabase db = _redisBusiness.GetRedisDatabase();
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
                _redisBusiness.Disconnect();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while trying to retrieve hash fields matching with pattern {pattern}, {e.Message}");
            }
            return results;
        }

        /*
        private void CheckForCollisions(List<KeyValuePair<string, string>> devices,List<KeyValuePair<string, string>> collisions)
        {

            for(int i = 0; i < devices.Count; i++)
            {
                var currentDevice = devices[i];

                // Get the 3D coordinates of the first device
                var currentDeviceCooords = currentDevice.Value.ToString().Split(',');
                var currentDeviceImei = currentDevice.Key.Split(":")[1];

                // Loop through the remaining devices
                for (int j = i+1; j< devices.Count; j++)
                {
                    try
                    {
                        var comparableDevice = devices[j];
                        var comparableDeviceImei = comparableDevice.Key.Split(":")[1];

                        // Get the 3D coordinates of the second device
                        var comparableDeviceCoords = comparableDevice.Value.ToString().Split(',');

                        //Console.WriteLine($"Comparing {currentDeviceImei}:{JsonConvert.SerializeObject(currentDeviceCooords)} with {comparableDeviceImei}:{JsonConvert.SerializeObject(comparableDeviceCoords)}");

                        double long1 = double.Parse(currentDeviceCooords[0] ?? "-1");
                        double long2 = double.Parse(comparableDeviceCoords[0] ?? "-1");

                        double lat1 = double.Parse(currentDeviceCooords[1] ?? "-1");
                        double lat2 = double.Parse(comparableDeviceCoords[1] ?? "-1");

                        double alt1 = double.Parse(currentDeviceCooords[2] ?? "-1");
                        double alt2 = double.Parse(comparableDeviceCoords[2] ?? "-1");

                        if (long1 != -1 && long2 != -1 && lat1 != -1 && lat2 != -1 && alt1 != -1 && alt2 != -1)
                        {
                            // Check for collisions
                            if (long1 == long2 && lat1 == lat2 && alt1 == alt2)
                            {
                                _logger.LogWarning($"Collision detection between {currentDeviceImei} & {comparableDeviceImei}");
                                // Add the collision to the list
                                collisions.Add(new KeyValuePair<string, string>(currentDeviceImei, comparableDeviceImei));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{ex.Message} due to {ex.InnerException}. \n Trace -> {ex.StackTrace}");
                    }

                }
            }

        }*/
        private void CheckForCollisions(List<KeyValuePair<string, string>> devices, KeyValuePair<string, string> currentDevice, List<KeyValuePair<string, string>> collisions)
        {
            // Get the 3D coordinates and speed of the first device
            //HXbW5:fec727a1b624483aaf00e871b10462c5
            var currentDeviceImei = currentDevice.Key.Split(":")[1];

            var currentDeviceCooords = currentDevice.Value.Split(":")[0].Split(",");
            float currentDeviceSpeed = float.Parse(currentDevice.Value.Split(":")[1] ?? "0");

            // Loop through the remaining devices
            for (int j = devices.IndexOf(currentDevice) < devices.Count ? devices.IndexOf(currentDevice) + 1 : devices.IndexOf(currentDevice); j < devices.Count; j++)
            {
                try
                {
                    var comparableDevice = devices[j];
                    var comparableDeviceImei = comparableDevice.Key.Split(":")[1];

                    // Get the 3D coordinates and speed of the second device
                    var comparableDeviceCoords = comparableDevice.Value.Split(":")[0].Split(',');
                    float comparableDeviceSpeed = float.Parse(comparableDevice.Value.Split(":")[1] ?? "0");

                    //Console.WriteLine($"Comparing {currentDeviceImei} with {comparableDeviceImei}");

                    float long1 = float.Parse(currentDeviceCooords[0] ?? "-1");
                    float long2 = float.Parse(comparableDeviceCoords[0] ?? "-1");

                    float lat1 = float.Parse(currentDeviceCooords[1] ?? "-1");
                    float lat2 = float.Parse(comparableDeviceCoords[1] ?? "-1");

                    float alt1 = float.Parse(currentDeviceCooords[2] ?? "-1");
                    float alt2 = float.Parse(comparableDeviceCoords[2] ?? "-1");


                    ///OLD WAY
                    //if (long1 != -1 && long2 != -1 && lat1 != -1 && lat2 != -1 && alt1 != -1 && alt2 != -1)
                    //{
                    //    // Check for collisions
                    //    if (long1 == long2 && lat1 == lat2 && alt1 == alt2)
                    //    {
                    //        _logger.LogWarning($"Collision detection between {currentDeviceImei} & {comparableDeviceImei}");
                    //        // Add the collision to the list
                    //        collisions.Add(new KeyValuePair<string, string>(currentDeviceImei, comparableDeviceImei));
                    //    }
                    //}

                    ///NEW WAY

                    //Define the two vectors
                    Vector3 device1Vector = new Vector3(long1, lat1, alt1);
                    Vector3 device2Vector = new Vector3(long2, lat2, alt2);

                    //Unit vector in the direction of motion of vector1
                    Vector3 device1Direction = Normalize(device2Vector - device1Vector);

                    //Unit vector in the direction of motion of vector2
                    Vector3 device2Direction = Normalize(device1Vector - device2Vector);

                    Vector3 device1Velocity = device1Direction * currentDeviceSpeed;
                    Vector3 device2Velocity = device2Direction * comparableDeviceSpeed;

                    //Calculate the relative velocity between the two vectors
                    Vector3 relativeVelocity = device2Velocity - device1Velocity;

                    //Calculate the time until the two vectors will be closest to each other
                    Vector3 distanceVector = device2Vector - device1Vector;

                    float closestApproachTime = -Vector3.Dot(distanceVector, relativeVelocity) / (float)Magnitude(relativeVelocity);

                    //Calculate the distance between the two vectors at the closest approach
                    Vector3 closestApproachPos1 = device1Vector + device1Velocity * closestApproachTime;
                    Vector3 closestApproachPos2 = device2Vector + device2Velocity * closestApproachTime;

                    float distanceAtClosestApproach = Vector3.Distance(closestApproachPos1, closestApproachPos2);

                    //Calcualte the time it will take for the two vectors to collide 
                    float timeToCollision = distanceAtClosestApproach / (float)Magnitude(relativeVelocity);

                    //Calculate the position of the two vectors at the time of collision
                    Vector3 positionAtCollision1 = device1Vector + (device1Velocity * timeToCollision);
                    Vector3 positionAtCollision2 = device2Vector + (device2Velocity * timeToCollision);

                    //Check if the two vectors will collide before they get closest to each other
                    if (timeToCollision < closestApproachTime)
                    {
                        _logger.LogWarning($"Collision detected between {currentDeviceImei} & {comparableDeviceImei}");
                        Console.WriteLine($"Collision detected between {currentDeviceImei} & {comparableDeviceImei}");
                        collisions.Add(new KeyValuePair<string, string>(currentDeviceImei, comparableDeviceImei));
                    }
                    else
                    {
                        Console.WriteLine($"No collision detected between {currentDeviceImei} & {comparableDeviceImei}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message} due to {ex.InnerException}. \n Trace -> {ex.StackTrace}");
                }

            }

        }

        private double Magnitude(Vector3 v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        private Vector3 Normalize(Vector3 v)
        {
            double magnitude = Magnitude(v);
            if (magnitude != 0)
            {
                v.X /= (float)magnitude;
                v.Y /= (float)magnitude;
                v.Z /= (float)magnitude;
            }
            return v;
        }
        private double Distance(Vector3 v1, Vector3 v2)
        {
            double dx = v1.X - v2.X;
            double dy = v1.Y - v2.Y;
            double dz = v1.Z - v2.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private Boolean WillCollide(Vector3 dv1, Vector3 sv1, Vector3 dv2, Vector3 sv2)
        {
            Boolean collision = false;

            //Get the distance between the two vectors
            double distanceInBetween = Distance(dv1, dv2);

            if (distanceInBetween > 0)
            {
                //Vectors still not have not collided

            }
            else
            {
                //Vectors already colliding
            }

            return collision;
        }
    }
    /*private void CheckForCollisions(List<KeyValuePair<string, string>> devices, int taskId)
    {
        foreach (KeyValuePair<string, string> pair in devices)
        {
            string intersectionId = pair.Key.Split(":")[0];
            string imei = pair.Key.Split(":")[1];
            try
            {
                Console.WriteLine($"[{taskId}] - Processing imei: {imei} within intersection: {intersectionId}");

            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while trying to check for potential collision for device imei: {imei} within intersection: {intersectionId} using task id: {taskId}");
            }
        }
    }*/
}
