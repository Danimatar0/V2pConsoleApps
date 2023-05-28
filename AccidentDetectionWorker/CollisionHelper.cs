using AccidentDetectionWorker.Models.Common;
using AccidentDetectionWorker.Models.RedisModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker
{
    public static class CollisionHelper
    {
        public static void CheckFor2DCollisionsV1Nested(GlobalConfig config,ILogger _logger, DeviceSegment d1, DeviceSegment d2, ref List<CollisionAtDistanceAfterTime> collisions)
        {
            //Gathering segments positions
            Vector2 d1Pos = new Vector2(d1.Segment.X, d1.Segment.Y);
            Vector2 d2Pos = new Vector2(d2.Segment.X, d2.Segment.Y);

            Vector2 relativeVelocity = d2Pos - d1Pos; // Calculate the relative velocity vector
            float relativeSpeed = relativeVelocity.Length(); // Calculate the relative speed

            float timeToCollision = relativeSpeed > 0 ? relativeVelocity.Length() / (d1.LastSpeed + d2.LastSpeed) : float.MaxValue; // Calculate the time to collision

            float distanceToCollision = timeToCollision * (d1.LastSpeed + d2.LastSpeed); // Calculate the distance to collision

            //Check if the two devices will collide and output the distance to collision
            if (distanceToCollision < config.Constants.CollisionThreshold) // Replace 0.1f with your own collision threshold
            {
                //Console.WriteLine($"Devices {d1.Imei} & {d2.Imei} will collide in {timeToCollision} seconds at a distance of {distanceToCollision} distance units.");
                collisions.Add(new CollisionAtDistanceAfterTime { D1 = d1, D2 = d2, Distance = distanceToCollision, Time = timeToCollision });
            }
            //else
            //{
            //    Console.WriteLine($"Devices {d1.Imei} & {d2.Imei} will not collide in the foreseeable future.");
            //}


        }
        public static void CheckFor2DCollisionsV1(ILogger _logger, List<KeyValuePair<string, string>> devices, KeyValuePair<string, string> currentDevice, List<KeyValuePair<string, string>> collisions)
        {
            // Get the 3D coordinates and speed of the first device
            //HXbW5:fec727a1b624483aaf00e871b10462c5
            var currentDeviceImei = currentDevice.Key.Split(":")[1];

            var currentDeviceCoords = currentDevice.Value.Split(":")[0].Split(",");

            Vector2 d1Pos = new Vector2(float.Parse(currentDeviceCoords[0] ?? "0"), float.Parse(currentDeviceCoords[1] ?? "0"));
            float d1Speed = float.Parse(currentDevice.Value.Split(":")[1] ?? "0");

            // Loop through the remaining devices
            for (int j = devices.IndexOf(currentDevice) < devices.Count ? devices.IndexOf(currentDevice) + 1 : devices.IndexOf(currentDevice); j < devices.Count; j++)
            {
                try
                {
                    var comparableDevice = devices[j];
                    var comparableDeviceImei = comparableDevice.Key.Split(":")[1];

                    Console.WriteLine($"Checking {currentDeviceImei} & {comparableDeviceImei}");

                    // Get the 3D coordinates and speed of the second device
                    var comparableDeviceCoords = comparableDevice.Value.Split(":")[0].Split(',');

                    Vector2 d2Pos = new Vector2(float.Parse(comparableDeviceCoords[0] ?? "0"), float.Parse(comparableDeviceCoords[1] ?? "0"));
                    float d2Speed = float.Parse(comparableDevice.Value.Split(":")[1] ?? "0");

                    Vector2 relativeVelocity = d2Pos - d1Pos; // Calculate the relative velocity vector
                    float relativeSpeed = relativeVelocity.Length(); // Calculate the relative speed

                    float timeToCollision = relativeSpeed > 0 ? relativeVelocity.Length() / (d1Speed + d2Speed) : float.MaxValue; // Calculate the time to collision

                    float distanceToCollision = timeToCollision * (d1Speed + d2Speed); // Calculate the distance to collision

                    // Check if the two devices will collide and output the distance to collision
                    //if (distanceToCollision < _globalConfig.Constants.CollisionThreshold) // Replace 0.1f with your own collision threshold
                    //{
                    //    Console.WriteLine($"Devices {currentDeviceImei} & {comparableDeviceImei} will collide in {timeToCollision} seconds at a distance of {distanceToCollision} distance units.");
                    //}
                    //else
                    //{
                    //    Console.WriteLine($"Devices {currentDeviceImei} & {comparableDeviceImei} will not collide in the foreseeable future.");
                    //}
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message} due to {ex.InnerException}. \n Trace -> {ex.StackTrace}");
                }

            }

        }
        public static void CheckFor3DCollisionsV2(ILogger _logger, List<KeyValuePair<string, string>> devices, KeyValuePair<string, string> currentDevice, List<KeyValuePair<string, string>> collisions)
        {
            // Get the 3D coordinates and speed of the first device
            //HXbW5:fec727a1b624483aaf00e871b10462c5
            var currentDeviceImei = currentDevice.Key.Split(":")[1];

            var currentDeviceCoords = currentDevice.Value.Split(":")[0].Split(",");
            float currentDeviceSpeed = float.Parse(currentDevice.Value.Split(":")[1] ?? "0");

            Vector3 currentDeviceVector = new Vector3(float.Parse(currentDeviceCoords[0] ?? "0"), float.Parse(currentDeviceCoords[1] ?? "0"), float.Parse(currentDeviceCoords[2] ?? "0"));

            Vector3 currentDeviceDirection = Vector3.Normalize(currentDeviceVector);
            // calculate the direction angle
            double currentAngle = Math.Atan2(currentDeviceVector.Y, currentDeviceVector.X) * 180 / Math.PI;
            double currentElevation = Math.Asin(currentDeviceVector.Z / currentDeviceVector.Length()) * 180 / Math.PI;

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

                    Vector3 comparableDeviceVector = new Vector3(float.Parse(comparableDeviceCoords[0] ?? "0"), float.Parse(comparableDeviceCoords[1] ?? "0"), float.Parse(comparableDeviceCoords[2] ?? "0"));

                    // calculate the direction angle
                    double comparableAngle = Math.Atan2(comparableDeviceVector.Y, comparableDeviceVector.X) * 180 / Math.PI;
                    double comparableElevation = Math.Asin(comparableDeviceVector.Z / comparableDeviceVector.Length()) * 180 / Math.PI;



                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message} due to {ex.InnerException}. \n Trace -> {ex.StackTrace}");
                }

            }

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
        public static void CheckFor3DCollisionsV1(ILogger _logger, List<KeyValuePair<string, string>> devices, KeyValuePair<string, string> currentDevice, List<KeyValuePair<string, string>> collisions)
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
                    Vector3 device1Direction = GeoSpatialHelper.Normalize(device2Vector - device1Vector);

                    //Unit vector in the direction of motion of vector2
                    Vector3 device2Direction = GeoSpatialHelper.Normalize(device1Vector - device2Vector);

                    Vector3 device1Velocity = device1Direction * currentDeviceSpeed;
                    Vector3 device2Velocity = device2Direction * comparableDeviceSpeed;

                    //Calculate the relative velocity between the two vectors
                    Vector3 relativeVelocity = device2Velocity - device1Velocity;

                    //Calculate the time until the two vectors will be closest to each other
                    Vector3 distanceVector = device2Vector - device1Vector;

                    float closestApproachTime = -Vector3.Dot(distanceVector, relativeVelocity) / (float)GeoSpatialHelper.Magnitude(relativeVelocity);

                    //Calculate the distance between the two vectors at the closest approach
                    Vector3 closestApproachPos1 = device1Vector + device1Velocity * closestApproachTime;
                    Vector3 closestApproachPos2 = device2Vector + device2Velocity * closestApproachTime;

                    float distanceAtClosestApproach = Vector3.Distance(closestApproachPos1, closestApproachPos2);

                    //Calcualte the time it will take for the two vectors to collide 
                    float timeToCollision = distanceAtClosestApproach / (float)GeoSpatialHelper.Magnitude(relativeVelocity);

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
        public static Boolean WillCollide(Vector3 dv1, Vector3 sv1, Vector3 dv2, Vector3 sv2)
        {
            Boolean collision = false;

            //Get the distance between the two vectors
            double distanceInBetween = GeoSpatialHelper.Distance(dv1, dv2);

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

        public static List<CollisionCheckCombination> PopulateCollisionCombinations(List<KeyValuePair<string, string>> devices)
        {
            List<CollisionCheckCombination> combinations = new List<CollisionCheckCombination>();

            for (int i=0;i<devices.Count -1;i++)
            {
                DeviceSegment S1 = DeviceSegment.FromKeyValuePair(devices[i]);
                for (int j = i + 1; j < devices.Count; j++)
                {
                    DeviceSegment S2 = DeviceSegment.FromKeyValuePair(devices[j]);

                    if (!combinations.Where(comb => (comb.D1.Imei.Equals(S1.Imei) && comb.D2.Imei.Equals(S2.Imei)) || (comb.D2.Imei.Equals(S1.Imei) && comb.D1.Imei.Equals(S2.Imei))).Any())
                    {
                        combinations.Add(new CollisionCheckCombination { D1= S1, D2 = S2 });
                    }
                }
            }


            return combinations;
        }
    }
}
