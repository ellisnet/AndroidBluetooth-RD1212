using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Prism.Commands;
using Prism.Navigation;
using Rd1212.app.Models;
using Rd1212.app.Services;

namespace Rd1212.app.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private IDetectorService _detectorService;
        private readonly string _devicePrefixToLookFor = "RD1212";

        private DetectorDevice _connectedDevice;

        #region Bindable properties

        public bool IsDeviceConnected => _connectedDevice?.IsConnected ?? false;

        private bool _isDeviceConnecting;
        public bool IsDeviceConnecting
        {
            get => _isDeviceConnecting;
            set => SetProperty(ref _isDeviceConnecting, value);
        }

        #endregion

        #region Commands and their implementations

        #region ConnectToDeviceCommand

        private DelegateCommand _connectToDeviceCommand;
        public DelegateCommand ConnectToDeviceCommand => _connectToDeviceCommand
            ?? (_connectToDeviceCommand = new DelegateCommand(
             async () =>
             {
                 IsDeviceConnecting = true;
                 try
                 {
                     if (_connectedDevice == null)
                     {
                         throw new InvalidOperationException("Couldn't find a paired bluetooth device to connect to.");
                     }
                     await _detectorService.ConnectToDevice(_connectedDevice);
                 }
                 catch (Exception ex)
                 {
                     await ShowErrorAsync($"Error while connecting: {ex.Message}");
                 }
                 finally
                 {
                     IsDeviceConnecting = false;
                     NotifyPropertyChanged(nameof(IsDeviceConnected));
                 }
             },
             () => (!IsDeviceConnected) && (!IsDeviceConnecting))
             .ObservesProperty(() => IsDeviceConnected)
             .ObservesProperty(() => IsDeviceConnecting));

        #endregion

        #endregion

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            await Task.Delay(2000); //Wait a couple of secs for page to finish loading

            IList<DetectorDevice> pairedDevices = await _detectorService.FindAvailableDevices();

            if (pairedDevices.Count < 1)
            {
                await ShowInfoAsync("No available bluetooth devices.");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{pairedDevices.Count} available bluetooth device{(pairedDevices.Count > 1 ? "s" : "")} found:");
                foreach (DetectorDevice device in pairedDevices)
                {
                    sb.AppendLine($" - {device.Name} - {device.Address}");
                }

                await ShowInfoAsync(sb.ToString());
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
            _detectorService?.DisconnectDevice(_connectedDevice);
            _connectedDevice = null;
            _detectorService = null;
            base.Destroy();
        }
    }
}
