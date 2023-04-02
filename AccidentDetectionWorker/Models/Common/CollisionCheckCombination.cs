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
        public float Time { get; set; }
        public float Distance { get; set; }
    }
}
