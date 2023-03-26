using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Models.RedisModels
{
    public class Intersection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public BaseCoordinate Coordinate { get; set; }
        public DateTime LastUpdated { get; set; }
        public Boolean IsActive { get; set; }
    }
}
