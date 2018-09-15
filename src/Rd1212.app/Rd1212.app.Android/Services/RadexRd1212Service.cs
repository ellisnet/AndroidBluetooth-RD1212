using System.Collections.Generic;
using System.Threading.Tasks;
using Rd1212.app.Models;
using Rd1212.app.Services;

namespace Rd1212.app.Droid.Services
{
    public class RadexRd1212Service : IDetectorService
    {
        #region IDetectorService implementation

        public Task<IList<DetectorDevice>> FindAvailableDevices()
        {
            //Returning crap for now
            return Task.FromResult(new DetectorDevice[] {new DetectorDevice()} as IList<DetectorDevice>);
        }

        public Task<bool> ConnectToDevice(DetectorDevice device)
        {
            //Returning crap for now
            return Task.FromResult(true);
        }

        public void DisconnectDevice(DetectorDevice device)
        {
            //Nothing here yet
        }

        #endregion

        #region IDisposable implementation

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }

        #endregion
    }
}