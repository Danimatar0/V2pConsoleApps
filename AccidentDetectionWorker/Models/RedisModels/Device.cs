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
        public double Speed { get; set; }
        public List<DeviceCoordinate> Coordinates { get; set; } = new List<DeviceCoordinate>();
        public BaseCoordinate Segment { get; set; }
        public string IdIntersection { get; set; }

        public Device(string imei, double speed, List<DeviceCoordinate> coordinates, BaseCoordinate segment, string idIntersection)
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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
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
        public Tuple<double, double, double> Segment { get; set; }
    }
}
