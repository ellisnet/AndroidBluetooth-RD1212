using System;

namespace Rd1212.app.Models
{
    public interface IDetectorDevice : IDisposable
    {
        string Name { get; }
        string Address { get; }
        string SerialNumber { get; }
        bool IsConnected { get; }
    }
}
