using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Prism.Commands;
using Prism.Navigation;
using Rd1212.app.Models;
using Rd1212.app.Services;
using Xamarin.Essentials;

namespace Rd1212.app.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private IDetectorService _detectorService;
        private IDetectorDevice _detectorDevice;
        private bool _hasLocationPermission;

        #region Bindable properties

        public bool HasConnectableDevice => _detectorDevice != null;
        public bool IsDeviceConnected => _detectorDevice?.IsConnected ?? false;

        private bool _isDeviceConnecting;
        public bool IsDeviceConnecting
        {
            get => _isDeviceConnecting;
            set => SetProperty(ref _isDeviceConnecting, value);
        }

        public string ConnectToDeviceText =>
            (_detectorDevice == null)
                ? "Connect to device"
                : _detectorDevice.IsConnected
                    ? $"Disconnect from {_detectorDevice.Name}"
                    : $"Connect to {_detectorDevice.Name}";

        #endregion

        #region Commands and their implementations

        #region ConnectDisconnectDeviceCommand

        private DelegateCommand _connectDisconnectDeviceCommand;
        public DelegateCommand ConnectDisconnectDeviceCommand => LazyCommand(ref _connectDisconnectDeviceCommand,
            async () =>
            {
                IsDeviceConnecting = true;
                try
                {
                    if (_detectorDevice == null)
                    {
                        throw new InvalidOperationException("Couldn't find an available BLE detector device to connect to.");
                    }

                    if (IsDeviceConnected)
                    {
                        await _detectorService.DisconnectDevice(_detectorDevice);
                    }
                    else
                    {
                        bool connectSuccess = await _detectorService.ConnectToDevice(_detectorDevice);
                        if (!connectSuccess)
                        {
                            await ShowErrorAsync("Unable to connect to device.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"Error while {(IsDeviceConnected ? "disconnecting" : "connecting")}: {ex.Message}");
                }
                finally
                {
                    IsDeviceConnecting = false;
                    NotifyPropertyChanged(nameof(HasConnectableDevice));
                    NotifyPropertyChanged(nameof(IsDeviceConnected));
                    NotifyPropertyChanged(nameof(ConnectToDeviceText));
                }

                if (IsDeviceConnected)
                {
                    await ShowInfoAsync($"Connected to detector device -\nSerial Number:\n{_detectorDevice.SerialNumber}");
                }
            },
            () => (HasConnectableDevice) && (!IsDeviceConnecting))
            .ObservesProperty(() => HasConnectableDevice)
            .ObservesProperty(() => IsDeviceConnecting);            

        #endregion

        #endregion

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            await Task.Delay(2000); //Wait a couple of secs for page to finish loading

            //Make sure we have location permissions - app should prompt for them if we don't
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();
                _hasLocationPermission = location != null;
            }
            catch (Exception)
            {
                _hasLocationPermission = false;
            }

            if (!_hasLocationPermission)
            {
                await ShowErrorAsync("Unable to search for BLE detector devices without device location access.");
            }
            else
            {
                IList<IDetectorDevice> availableDevices = await _detectorService.FindAvailableDevices(5000);

                if (availableDevices.Count < 1)
                {
                    await ShowInfoAsync("No available BLE detector devices.");
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"{availableDevices.Count} available BLE detector device{(availableDevices.Count > 1 ? "s" : "")} found:");
                    foreach (IDetectorDevice device in availableDevices)
                    {
                        sb.AppendLine($" - {device.Name} - {device.Address}");
                    }

                    await ShowInfoAsync(sb.ToString());

                    _detectorDevice = availableDevices[0];
                    NotifyPropertyChanged(nameof(HasConnectableDevice));
                    NotifyPropertyChanged(nameof(ConnectToDeviceText));
                }
            }
        }

        public MainPageViewModel(
            INavigationService navigationService,
            IUserDialogs dialogService,
            IDetectorService detectorService)
            : base(navigationService, dialogService)
        {
            _detectorService = detectorService ?? throw new ArgumentNullException(nameof(detectorService));
        }

        public override void Destroy()
        {
            _detectorService?.DisconnectDevice(_detectorDevice);
            _detectorDevice = null;
            _detectorService = null;
            base.Destroy();
        }
    }
}
