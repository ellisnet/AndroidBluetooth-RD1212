using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Rd1212.app.Droid.Models;
using Rd1212.app.Models;
using Rd1212.app.Services;
using ScanMode = Plugin.BLE.Abstractions.Contracts.ScanMode;

namespace Rd1212.app.Droid.Services
{
    public class RadexRd1212Service : IDetectorService
    {
        private enum RequestCommand : byte
        {
            DeviceInfo = 0,
            DeviceSerialNumber = 1,
            Data = 2,
            EraseMemory = 3,
            Display = 4,
        }

        private static readonly string _devicePrefixToLookFor = "RD1212";

        // ReSharper disable InconsistentNaming
        private static readonly Guid GenericAccessServiceId = Guid.ParseExact("00001800-0000-1000-8000-00805f9b34fb", "D");
        private static readonly Guid DeviceNameCharacteristicId = Guid.ParseExact("00002a00-0000-1000-8000-00805f9b34fb", "D");
        private static readonly Guid AppearanceCharacteristicId = Guid.ParseExact("00002a01-0000-1000-8000-00805f9b34fb", "D");

        private static readonly Guid DataServiceId = Guid.ParseExact("0bd51666-e7cb-469b-8e4d-2742f1ba77cc", "D");
        private static readonly Guid DataCharacteristicId = Guid.ParseExact("e7add780-b042-4876-aae1-112855353cc1", "D");
        private static readonly Guid ClientConfigDescriptorId = Guid.ParseExact("00002902-0000-1000-8000-00805f9b34fb", "D");

        private static readonly byte[] EnableProtocolCommand = {2};

        private static readonly byte CommandIdByte1 = 18;
        private static readonly byte CommandIdByte2 = 18;
        private static readonly byte VersionByte = 1;

        // ReSharper restore InconsistentNaming

        private IAdapter _bleAdapter;
        private IBluetoothLE _ble;
        private readonly object _foundDeviceLocker = new object();
        private readonly List<IDevice> _foundDevices = new List<IDevice>();
        private TaskCompletionSource<bool> _deviceScanCompletion;
        private TaskCompletionSource<byte[]> _waitForReceivedCompletion;

        private IDevice _connectedDevice;
        private IService _dataService;
        private ICharacteristic _dataCharacteristic;

        #region IDetectorService implementation

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            Debug.WriteLine($"BLE device discovered: {(String.IsNullOrWhiteSpace(args?.Device?.Name) ? "(no name)" : args.Device.Name)}");
            if (!String.IsNullOrWhiteSpace(args?.Device?.Name)
                && args.Device.Name.Trim().StartsWith(_devicePrefixToLookFor, StringComparison.InvariantCultureIgnoreCase))
            {
                string address = (args.Device.NativeDevice as BluetoothDevice)?.Address;
                lock (_foundDeviceLocker)
                {
                    // ReSharper disable PossibleNullReferenceException
                    if (!String.IsNullOrWhiteSpace(address)
                        && _foundDevices.All(a => !(args.Device.NativeDevice as BluetoothDevice).Address.Equals(address, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        _foundDevices.Add(args.Device);
                    }
                    // ReSharper restore PossibleNullReferenceException
                }
            }
        }

        private void OnScanTimeoutElapsed(object sender, EventArgs args) => _deviceScanCompletion?.TrySetResult(true);

        private void OnDataValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
        {
            byte[] receivedBytes = args?.Characteristic?.Value;
            if (receivedBytes != null)
            {
                Debug.WriteLine($"===========================Incoming bytes: {BitConverter.ToString(receivedBytes)}");
                _waitForReceivedCompletion?.TrySetResult(receivedBytes);
            }
        }

        private async Task CleanupDataService()
        {
            if (_dataCharacteristic != null)
            {
                try
                {
                    _dataCharacteristic.ValueUpdated -= OnDataValueUpdated;
                    await _dataCharacteristic.StopUpdatesAsync();
                }
                catch (Exception)
                {
                    //Nothing to do here, couldn't unsubscribe from characteristic updates
                }
            }
            _dataCharacteristic = null;
            _dataService?.Dispose();
            _dataService = null;
        }

        private Task<bool> SendRequest(ICharacteristic characteristic, RequestCommand command, byte[] bytesToSend = null)
        {
            if (characteristic == null) { throw new ArgumentNullException(nameof(characteristic));}
            var output = new byte[14];
            output[0] = CommandIdByte1;
            output[1] = CommandIdByte2;
            output[2] = VersionByte;
            output[3] = (byte)command;

            if (bytesToSend != null && bytesToSend.Any())
            {
                if (bytesToSend.Length > 10) { throw new ArgumentException("Only able to send 10 bytes max.", nameof(bytesToSend));}
                Buffer.BlockCopy(bytesToSend, 0, output, 4, bytesToSend.Length);
            }

            return characteristic.WriteAsync(output);
        }

        private string ParseSerialNumber(byte[] receivedBytes)
        {
            if (receivedBytes == null) { throw new ArgumentNullException(nameof(receivedBytes));}
            if (receivedBytes.Length < 7) { throw new ArgumentException("Unable to parse serial number - not enough bytes.", nameof(receivedBytes));}
            return BitConverter.ToString(receivedBytes, 0, 7).Replace("-", "");
        }

        public async Task<IList<IDetectorDevice>> FindAvailableDevices(int scanTimeMilliseconds = 10000)
        {
            if (scanTimeMilliseconds < 100) { scanTimeMilliseconds = 100;}

            lock (_foundDeviceLocker)
            {
                _foundDevices.Clear();
            }

            _deviceScanCompletion = new TaskCompletionSource<bool>();
            _bleAdapter.DeviceDiscovered += OnDeviceDiscovered;
            _bleAdapter.ScanTimeoutElapsed += OnScanTimeoutElapsed;

            _bleAdapter.ScanTimeout = scanTimeMilliseconds;
            _bleAdapter.ScanMode = ScanMode.Balanced;

            await _bleAdapter.StartScanningForDevicesAsync();
            await _deviceScanCompletion.Task;

            _bleAdapter.DeviceDiscovered -= OnDeviceDiscovered;
            _bleAdapter.ScanTimeoutElapsed -= OnScanTimeoutElapsed;

            IDetectorDevice[] result;
            lock (_foundDeviceLocker)
            {
                result = _foundDevices.Select(s => new Rd1212DetectorDevice(s)).ToArray();
            }

            return result;
        }

        public async Task<bool> ConnectToDevice(IDetectorDevice device)
        {
            var rd1212 = device as Rd1212DetectorDevice;
            IDevice bleDevice = rd1212?.BleDevice;
            if (bleDevice == null) { throw new ArgumentNullException(nameof(device));}

            bool result = device.IsConnected;

            if (!result)
            {
                await CleanupDataService();

                try
                {
                    await _bleAdapter.ConnectToDeviceAsync(bleDevice);

                    _dataService = await bleDevice.GetServiceAsync(DataServiceId);
                    if (_dataService == null)
                    {
                        throw new Exception("Unable to find the specified GATT Service.");
                    }

                    _dataCharacteristic = await _dataService.GetCharacteristicAsync(DataCharacteristicId);
                    if (_dataCharacteristic == null)
                    {
                        throw new Exception("Unable to find the specified GATT Characteristic.");
                    }

                    _dataCharacteristic.ValueUpdated += OnDataValueUpdated;
                    await _dataCharacteristic.StartUpdatesAsync();

                    IDescriptor clientConfig = await _dataCharacteristic.GetDescriptorAsync(ClientConfigDescriptorId);
                    if (clientConfig == null)
                    {
                        throw new Exception("Unable to find the specified GATT Descriptor.");
                    }

                    await clientConfig.WriteAsync(EnableProtocolCommand);

                    await Task.Delay(500); //Short delay before requesting serial number

                    _waitForReceivedCompletion = new TaskCompletionSource<byte[]>();
                    await SendRequest(_dataCharacteristic, RequestCommand.DeviceSerialNumber);

                    byte[] serialNumberBytes = await _waitForReceivedCompletion.Task;

                    rd1212.SerialNumber = ParseSerialNumber(serialNumberBytes);

                    _connectedDevice = bleDevice;
                    result = rd1212.IsConnected = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    Debugger.Break();
                }
            }

            return result;
        }

        public void DisconnectDevice(IDetectorDevice device)
        {
            //Nothing here yet
        }

        #endregion

        public RadexRd1212Service()
        {
            _ble = CrossBluetoothLE.Current;
            _bleAdapter = CrossBluetoothLE.Current.Adapter;

            _ble.StateChanged += (sender, args) =>
            {
                Debug.WriteLine($"The bluetooth state changed to {(args?.NewState ?? BluetoothState.Unknown)}");
            };
        }

        #region IDisposable implementation

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                _dataCharacteristic = null;
                _dataService?.Dispose();
                _dataService = null;
                _bleAdapter = null;
                _ble = null;
            }
        }

        #endregion
    }
}