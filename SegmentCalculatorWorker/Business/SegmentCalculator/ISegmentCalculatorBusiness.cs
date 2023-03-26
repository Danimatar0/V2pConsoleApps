using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SegmentCalculatorWorker.Business.SegmentCalculator
{
    public interface ISegmentCalculatorBusiness
    {
        public Task StartService();
        public Task StopService();
    }
}
