using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Models.RedisModels
{
    public class Device
    {
        public string Imei { get; set; }
        public float Speed { get; set; }
        public List<DeviceCoordinate> Coordinates { get; set; } = new List<DeviceCoordinate>();
        public BaseCoordinate Segment { get; set; }
        public string IdIntersection { get; set; }

        public Device(string imei, float speed, List<DeviceCoordinate> coordinates, BaseCoordinate segment, string idIntersection)
        {
            Imei = imei;
            Speed = speed;
            Coordinates = coordinates;
            Segment = segment;
            IdIntersection = idIntersection;
        }
    }

    public class BaseCoordinate
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
    public class DeviceCoordinate : BaseCoordinate
    {
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// This class is used for the deviceSegments hashset to hold all devices segments with their imei and intersection id
    /// </summary>
    public class DeviceSegment
    {
        public string Imei { get; set; }
        public string IdIntersection { get; set; }
        public BaseCoordinate Segment { get; set; }
        public float LastSpeed { get; set; }

        public static DeviceSegment FromKeyValuePair(KeyValuePair<string, string> pair)
        {
            DeviceSegment segment = new DeviceSegment();
            segment.IdIntersection = pair.Key.Split(":")[0];
            segment.Imei = pair.Key.Split(":")[1];

            var currentDeviceCoords = pair.Value.Split(":")[0].Split(",");

            segment.Segment = new BaseCoordinate { X = float.Parse(currentDeviceCoords[0] ?? "0"), Y = float.Parse(currentDeviceCoords[1] ?? "0"), Z = float.Parse(currentDeviceCoords[2] ?? "0") };
            segment.LastSpeed = float.Parse(pair.Value.Split(":")[1] ?? "0");

            return segment;
        }
    }
}
