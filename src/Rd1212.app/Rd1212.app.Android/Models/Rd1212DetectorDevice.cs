using System;
using Android.Bluetooth;
using Plugin.BLE.Abstractions.Contracts;
using Rd1212.app.Models;

namespace Rd1212.app.Droid.Models
{
    public class Rd1212DetectorDevice : IDetectorDevice
    {
        public bool IsDisposed { get; private set; }

        public IDevice BleDevice { get; private set; }

        public string Name => BleDevice?.Name;
        public string Address => (BleDevice.NativeDevice as BluetoothDevice)?.Address;
        public bool IsConnected { get; set; }
        public string SerialNumber { get; set; }

        public Rd1212DetectorDevice(IDevice bleDevice)
        {
            BleDevice = bleDevice ?? throw new ArgumentNullException(nameof(bleDevice));
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                BleDevice?.Dispose();
                BleDevice = null;
            }
        }
    }
}
