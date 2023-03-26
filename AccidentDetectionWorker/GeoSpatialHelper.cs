﻿using AccidentDetectionWorker.Models.RedisModels;
using System;
using System.Collections.Generic;
//using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace AccidentDetectionWorker
{
    public static class GeoSpatialHelper
    {
        public static double Haversine(DeviceCoordinate coord1, DeviceCoordinate coord2)
        {
            double R = 6371000.0; // radius of the Earth in meters

            // Convert latitudes and longitudes to radians
            double lat1 = coord1.Latitude * Math.PI / 180.0;
            double lon1 = coord1.Longitude * Math.PI / 180.0;
            double lat2 = coord2.Latitude * Math.PI / 180.0;
            double lon2 = coord2.Longitude * Math.PI / 180.0;

            // Calculate differences in latitude and longitude
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            // Calculate Haversine formula components
            double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;

            // Calculate altitude difference
            double altitudeDiff = coord2.Altitude - coord1.Altitude;

            // Calculate total distance using distance formula
            double totalDistance = Math.Sqrt(Math.Pow(d, 2) + Math.Pow(altitudeDiff, 2));

            return totalDistance;
        }

        public static double GetDistance(DeviceCoordinate coord1, DeviceCoordinate coord2)
        {
            //GeoCoordinate geo1 = new GeoCoordinate(coord1.Latitude, coord1.Longitude,coord1.Altitude);
            //GeoCoordinate geo2 = new GeoCoordinate(coord2.Latitude, coord2.Longitude,coord2.Altitude);
            //return geo1.GetDistanceTo(geo2);
            return 0;
        }
        public static double GetSpeed()
        {
            double speed = 0.0;
            // Example array of DeviceCoordinate objects
            DeviceCoordinate[] coords = new DeviceCoordinate[] {
                new DeviceCoordinate {
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    Altitude = 0.0,
                    Timestamp = DateTime.Parse("2021-08-01 10:00:00")
                },
                new DeviceCoordinate {
                    Latitude = 37.7752,
                    Longitude = -122.4186,
                    Altitude = 10.0,
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
    }
}