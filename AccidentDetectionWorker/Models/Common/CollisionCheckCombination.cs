using AccidentDetectionWorker.Models.RedisModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Models.Common
{
    public class CollisionCheckCombination
    {
        public DeviceSegment D1 { get; set; }
        public DeviceSegment D2 { get; set; }

    }

    public class CollisionAtDistanceAfterTime : CollisionCheckCombination
    {
        public double Time1 { get; set; }
        public double Time2 { get; set; }
        public double Distance1 { get; set; }
        public double Distance2 { get; set; }
    }
}
