using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentDetectionWorker.Business.AccidentDetection
{
    public interface IAccidentDetectionBusiness
    {
        public Task StartService();
        public Task StopService();
    }
}
