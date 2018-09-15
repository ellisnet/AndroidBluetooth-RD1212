using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rd1212.app.Models;

namespace Rd1212.app.Services
{
    public interface IDetectorService : IDisposable
    {
        Task<IList<DetectorDevice>> FindAvailableDevices();
        Task<bool> ConnectToDevice(DetectorDevice device);
        void DisconnectDevice(DetectorDevice device);
    }
}
