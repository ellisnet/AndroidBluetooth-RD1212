using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rd1212.app.Models;

namespace Rd1212.app.Services
{
    public interface IDetectorService : IDisposable
    {
        Task<IList<IDetectorDevice>> FindAvailableDevices(int scanTimeMilliseconds = 10000);
        Task<bool> ConnectToDevice(IDetectorDevice device);
        void DisconnectDevice(IDetectorDevice device);
    }
}
