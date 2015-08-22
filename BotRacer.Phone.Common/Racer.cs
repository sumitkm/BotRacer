using System;
using System.Runtime.InteropServices.WindowsRuntime; // extension method byte[].AsBuffer()

//  Make it obvious which namespace provided each referenced type:
using ApplicationData = Windows.Storage.ApplicationData;
using ApplicationDataContainer = Windows.Storage.ApplicationDataContainer;
using BackgroundTaskBuilder = Windows.ApplicationModel.Background.BackgroundTaskBuilder;
using BackgroundTaskRegistration = Windows.ApplicationModel.Background.BackgroundTaskRegistration;
using BluetoothConnectionStatus = Windows.Devices.Bluetooth.BluetoothConnectionStatus;
using BluetoothLEDevice = Windows.Devices.Bluetooth.BluetoothLEDevice;
using DeviceConnectionChangeTrigger = Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger;
using GattCharacteristic = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;
using GattCharacteristicUuids = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicUuids;
using GattDeviceService = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;
using GattServiceUuids = Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceUuids;
using GattWriteOption = Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteOption;
using Task = System.Threading.Tasks.Task;

namespace BotRacer.Phone.Common
{
    public sealed class Racer
    {
        // static data
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        // data members
        private BluetoothLEDevice _device;           // constant, always non-null
        private string _addressString;               // constant, Bluetooth address as 12 hex digits
        private GattDeviceService linkLossService;  // constant, may be null
        private bool alertOnPhone;                  // true iff we want a popup when this device disconnects
        private bool alertOnDevice;                 // true iff we want device to alert upon disconnection
        private AlertLevel alertLevel;              // alert level that device will set upon disconnection
        private Guid _blueBrainServiceId = new Guid("7e400001-b5a3-f393-e0a9-e50e24dcca9e");
        private Guid _blueBrainNotifyCharacteristic = new Guid("7e400002-b5a3-f393-e0a9-e50e24dcca9e");
        private Guid _blueBrainWriteCharacteristic = new Guid("7e400003-b5a3-f393-e0a9-e50e24dcca9e");

        // trivial properties
        public BackgroundTaskRegistration TaskRegistration { get; set; }

        // readonly properties
        public bool HasLinkLossService { get { return linkLossService != null; } }
        public string Name { get { return _device.Name; } }
        public string TaskName { get { return _addressString; } }

        public int XValue { get; set; }
        public int YValue { get; set; }
        public int ZValue { get; set; }
        public int BValue { get; set; }

        // settable properties, persisted in LocalSettings

        public bool AlertOnPhone
        {
            get { return alertOnPhone; }
            set
            {
                alertOnPhone = value;
                SaveSettings();
            }
        }

        public bool AlertOnDevice
        {
            get { return alertOnDevice; }
            set
            {
                alertOnDevice = value;
                SaveSettings();
            }
        }


        public AlertLevel AlertLevel
        {
            get { return alertLevel; }
            set
            {
                alertLevel = value;
                SaveSettings();
            }
        }


        // Constructor
        public Racer(BluetoothLEDevice device)
        {
            XValue = 0;
            YValue = 0;
            ZValue = 255;
            BValue = 0;

            this._device = device;
            _addressString = device.BluetoothAddress.ToString("x012");
            try
            {
                linkLossService = device.GetGattService(GattServiceUuids.LinkLoss);
            }
            catch (Exception)
            {
                // e.HResult == 0x80070490 means that the device doesn't have the requested service.
                // We can still alert on the phone upon disconnection, but cannot ask the device to alert.
                // linkLossServer will remain equal to null.
            }

            if (localSettings.Values.ContainsKey(_addressString))
            {
                string[] values = ((string)localSettings.Values[_addressString]).Split(',');
                alertOnPhone = bool.Parse(values[0]);
                alertOnDevice = bool.Parse(values[1]);
                alertLevel = (AlertLevel)Enum.Parse(typeof(AlertLevel), values[2]);
            }
        }

        public void Steer(double value)
        {
            try
            {
                XValue = 255 - (int)value;
                YValue = (int)value; //128; //value > 0 ? 255 + (int)value : 255;
                System.Diagnostics.Debug.WriteLine("Set Speed to value: " + value.ToString());
                var service = this._device.GetGattService(_blueBrainServiceId);
                var characteristics = service.GetCharacteristics(_blueBrainWriteCharacteristic);
                if (characteristics != null)
                {
                    byte[] data = new byte[4];

                    data[0] = (byte)XValue;//Map(XValue, -255, 255, 0, 255);
                    data[1] = (byte)YValue;//Map(YValue, -255, 255, 0, 255);
                    data[2] = (byte)BValue;
                    data[3] = (byte)ZValue;

                    System.Diagnostics.Debug.WriteLine(string.Format("Sending Values to CannyBot: [{0},{1},{2},{3}]", data[0].ToString(), data[1].ToString(), data[2].ToString(), data[3].ToString()));

                    Task.Run(async () =>
                    {
                        await characteristics[0].WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithoutResponse);
                    });
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Blew up trying to steer");

            }
        }

        public void SetSpeed(double value)
        {
            try
            {
                ZValue = 255 - (int)value;
                System.Diagnostics.Debug.WriteLine("Set Speed to value: " + value.ToString());
                var service = this._device.GetGattService(_blueBrainServiceId);
                var characteristics = service.GetCharacteristics(_blueBrainWriteCharacteristic);
                if (characteristics != null)
                {
                    byte[] data = new byte[4];

                    data[0] = (byte)XValue;//Map(XValue, -255, 255, 0, 255);
                    data[1] = (byte)YValue;//Map(YValue, -255, 255, 0, 255);
                    data[2] = (byte)BValue;
                    data[3] = (byte)ZValue;

                    System.Diagnostics.Debug.WriteLine(string.Format("Sending Values to CannyBot: [{0},{1},{2},{3}]", data[0].ToString(), data[1].ToString(), data[2].ToString(), data[3].ToString()));

                    Task.Run(async () =>
                    {
                        await characteristics[0].WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithoutResponse);
                    });
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Blew up trying to Set Speed");

            }
        }

        long Map(long x, long in_min, long in_max, long out_min, long out_max)
        {
            return (long)Math.Ceiling((double)((x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min));
        }


        // React to a change in configuration parameters:
        //    Save new values to local settings
        //    Set link-loss alert level on the device if appropriate
        //    Register or unregister background task if necessary
        private async void SaveSettings()
        {
            // Save this device's settings into nonvolatile storage
            localSettings.Values[_addressString] = string.Join(",", alertOnPhone, alertOnDevice, alertLevel);

            // If the device is connected and wants to hear about the alert level on link loss, tell it
            if (alertOnDevice && _device.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                await SetAlertLevelCharacteristic();
            }

            // If we need a background task and one isn't already registered, create one
            if (TaskRegistration == null && (alertOnPhone || alertOnDevice))
            {
                DeviceConnectionChangeTrigger trigger = await DeviceConnectionChangeTrigger.FromIdAsync(_device.DeviceId);
                trigger.MaintainConnection = true;
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                builder.Name = TaskName;
                builder.TaskEntryPoint = "BotRacer.Phone.Background.RacerTask";
                builder.SetTrigger(trigger);
                TaskRegistration = builder.Register();
            }

            // If we don't need a background task but have one, unregister it
            if (TaskRegistration != null && !alertOnPhone && !alertOnDevice)
            {
                TaskRegistration.Unregister(false);
                TaskRegistration = null;
            }
        }

        // Set the alert-level characteristic on the remote device
        public async Task SetAlertLevelCharacteristic()
        {
            // try-catch block protects us from the race where the device disconnects
            // just after we've determined that it is connected.
            try
            {
                byte[] data = new byte[1];
                data[0] = (byte)alertLevel;

                // The LinkLoss service should contain exactly one instance of the AlertLevel characteristic
                GattCharacteristic characteristic = linkLossService.GetCharacteristics(GattCharacteristicUuids.AlertLevel)[0];

                await characteristic.WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithResponse);
            }
            catch (Exception)
            {
                // ignore exception
            }
        }

        // Provide a human-readable name for this object.
        public override string ToString()
        {
            return _device.Name;
        }
    }
}
