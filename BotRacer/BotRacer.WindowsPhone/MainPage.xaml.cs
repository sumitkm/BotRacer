using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using BackgroundAccessStatus = Windows.ApplicationModel.Background.BackgroundAccessStatus;
using BackgroundExecutiondManager = Windows.ApplicationModel.Background.BackgroundExecutionManager;
using BackgroundTaskRegistration = Windows.ApplicationModel.Background.BackgroundTaskRegistration;
using DeviceInformation = Windows.Devices.Enumeration.DeviceInformation;
using BluetoothLEDevice = Windows.Devices.Bluetooth.BluetoothLEDevice;
using Racer = BotRacer.Phone.Common.Racer;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BotRacer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Racer> Devices { get; private set; }

        public MainPage()
        {
            Devices = new ObservableCollection<Racer>();

            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            // Request the right to have background tasks run in the future. This need only be done once
            // after the app is installed, but it is harmless to do it every time the app is launched.
            if (await BackgroundExecutiondManager.RequestAccessAsync() == BackgroundAccessStatus.Denied)
            {
                // TODO: What?
            }


            // Acquire the set of background tasks that we already have registered. Store them into a dictionary, keyed
            // by task name. (For each LE device, we will use a task name that is derived from its Bluetooth address).
            Dictionary<string, BackgroundTaskRegistration> taskRegistrations = new Dictionary<string, BackgroundTaskRegistration>();
            foreach (BackgroundTaskRegistration reg in BackgroundTaskRegistration.AllTasks.Values)
            {
                taskRegistrations[reg.Name] = reg;
            }

            // Get the list of paired Bluetooth LE devicdes, and add them to our 'devices' list. Associate each device with
            // its pre-existing registration if any, and remove that registration from our dictionary.
            Devices.Clear();

            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);
                Racer device = new Racer(bleDevice);
                if (taskRegistrations.ContainsKey(device.TaskName))
                {
                    device.TaskRegistration = taskRegistrations[device.TaskName];
                    taskRegistrations.Remove(device.TaskName);
                }
                Devices.Add(device);
            }

            // Unregister any remaining background tasks that remain in our dictionary. These are tasks that we registered
            // for Bluetooth LE devices that have since been unpaired.
            foreach (BackgroundTaskRegistration reg in taskRegistrations.Values)
            {
                reg.Unregister(false);
            }

        }

        private void deviceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
