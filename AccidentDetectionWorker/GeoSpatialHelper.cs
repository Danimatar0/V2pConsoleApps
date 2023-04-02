using AccidentDetectionWorker.Models.RedisModels;
using System;
using System.Collections.Generic;
//using System.Device.Location;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
namespace AccidentDetectionWorker
{
    public static class GeoSpatialHelper
    {
        public static double Haversine(DeviceCoordinate coord1, DeviceCoordinate coord2)
        {
            double R = 6371000.0; // radius of the Earth in meters

            // Convert latitudes and Ys to radians
            double lat1 = coord1.X * Math.PI / 180.0;
            double lon1 = coord1.Y * Math.PI / 180.0;
            double lat2 = coord2.X * Math.PI / 180.0;
            double lon2 = coord2.Y * Math.PI / 180.0;

            // Calculate differences in X and Y
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            // Calculate Haversine formula components
            double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;

            // Calculate Z difference
            double ZDiff = coord2.Z - coord1.Z;

            // Calculate total distance using distance formula
            double totalDistance = Math.Sqrt(Math.Pow(d, 2) + Math.Pow(ZDiff, 2));

            return totalDistance;
        }

        public static double GetDistance(DeviceCoordinate coord1, DeviceCoordinate coord2)
        {
            //GeoCoordinate geo1 = new GeoCoordinate(coord1.X, coord1.Y,coord1.Z);
            //GeoCoordinate geo2 = new GeoCoordinate(coord2.X, coord2.Y,coord2.Z);
            //return geo1.GetDistanceTo(geo2);
            return 0;
        }
        public static double GetSpeed()
        {
            double speed = 0.0;
            // Example array of DeviceCoordinate objects
            DeviceCoordinate[] coords = new DeviceCoordinate[] {
                new DeviceCoordinate {
                    X = 37.7749F,
                    Y = -122.4194F,
                    Z = 0.0F,
                    Timestamp = DateTime.Parse("2021-08-01 10:00:00")
                },
                new DeviceCoordinate {
                    X = 37.7752F,
                    Y = -122.4186F,
                    Z = 10.0F,
                    Timestamp = DateTime.Parse("2021-08-01 10:01:00")
                }
            };


            // Loop through coordinates to calculate speed
            for (int i = 0; i < coords.Length - 1; i++)
            {
                // Calculate distance between two coordinates using GetDistance Or Haversine
                double d = GetDistance(coords[i], coords[i + 1]); 

                // Calculate time elapsed between two coordinates
                double t = (coords[i + 1].Timestamp - coords[i].Timestamp).TotalSeconds;

                // Calculate speed as distance/time
                speed = d / t;

                Console.WriteLine($"Speed between coordinates {i} and {i + 1}: {speed} m/s");
            }
            return speed;
        }


        public static double Magnitude(Vector3 v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        public static Vector3 Normalize(Vector3 v)
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
        public static double Distance(Vector3 v1, Vector3 v2)
        {
            double dx = v1.X - v2.X;
            double dy = v1.Y - v2.Y;
            double dz = v1.Z - v2.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

    }
}
