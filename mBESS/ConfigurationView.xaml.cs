using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using WiimoteLib;
using System.Windows.Threading;

namespace mBESS
{
    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    /// 
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };


        public static void Refresh(this UIElement uiElement)

        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }

    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            ((Button)sender).IsEnabled = false;

            try
            {
                using (var btClient = new BluetoothClient())
                {
                    // PROBLEM:
                    // false false true: finds only unknown devices, which excludes existing but broken device entries.
                    // false true  true: finds broken entries, but even if powered off, so pairing attempts then crash.
                    // WORK-AROUND:
                    // Remove existing entries first, then find powered on entries.

                    var btIgnored = 0;

                    // Find remembered bluetooth devices.
                    label_Status.Content = "Start searching bluetooth devices...";
                    label_Status.Refresh();

                    if (cbRemoveEntries.IsChecked == true)
                    {
                        label_Status.Content = "Removing existing bluetooth devices...";
                        label_Status.Refresh();

                        var btExistingList = btClient.DiscoverDevices(255, false, true, false);

                        foreach (var btItem in btExistingList)
                        {
                            if (!btItem.DeviceName.Contains("Nintendo")) continue;

                            BluetoothSecurity.RemoveDevice(btItem.DeviceAddress);
                            btItem.SetServiceState(BluetoothService.HumanInterfaceDevice, false);
                        }
                    }

                    // Find unknown bluetooth devices.
                    label_Status.Content = "Searching for bluetooth devices...";
                    label_Status.Refresh();

                    var btDiscoveredList = btClient.DiscoverDevices(255, false, false, true);

                    foreach (var btItem in btDiscoveredList)
                    {
                        // Just in-case any non Wii devices are waiting to be paired.

                        if (cbSkipNameCheck.IsChecked == false && !btItem.DeviceName.Contains("Nintendo"))
                        {
                            btIgnored += 1;
                            continue;
                        }

                        label_Status.Content = "Adding: " + btItem.DeviceName + " ( " + btItem.DeviceAddress + " )";
                        label_Status.Refresh();

                        // Send special pin for permanent sync.

                        if (cbPermanentSync.IsChecked == true)
                        {
                            // Sync button requires host address, holding 1+2 buttons requires device address.

                            var btPin = AddressToWiiPin(BluetoothRadio.PrimaryRadio.LocalAddress.ToString());

                            // Pin needs to be added before doing the pair request.

                            new BluetoothWin32Authentication(btItem.DeviceAddress, btPin);

                            // Null forces legacy pin request instead of SSP authentication.

                            BluetoothSecurity.PairRequest(btItem.DeviceAddress, null);
                        }

                        // Install as a HID device and allow some time for it to finish.

                        btItem.SetServiceState(BluetoothService.HumanInterfaceDevice, true);
                    }

                    // Allow slow computers to finish installation before connecting.

                    System.Threading.Thread.Sleep(4000);

                    // Connect and send a command, otherwise they sleep and the device disappears.

                    try
                    {
                        if (btDiscoveredList.Length > btIgnored)
                        {
                            var deviceCollection = new WiimoteCollection();
                            deviceCollection.FindAllWiimotes();

                            foreach (var wiiDevice in deviceCollection)
                            {
                                wiiDevice.Connect();
                                wiiDevice.SetLEDs(true, false, false, false);
                                wiiDevice.Disconnect();
                            }
                        }
                    }
                    catch (Exception) { }

                    // Status report.

                    if (btDiscoveredList.Length > btIgnored)
                    {
                        label_Status.Content = "Finished - You can now close this tab and start your assessment. Found: " + btDiscoveredList.Length + " Ignored: " + btIgnored;
                    } else
                    {
                        label_Status.Content = "Finished - Try again, it was not possible to connect to the Wii Balance Board. Found: " + btDiscoveredList.Length + " Ignored: " + btIgnored;
                    }
                    label_Status.Refresh();
                }
            }
            catch (Exception ex)
            {
                label_Status.Content = "Error: " + ex.Message;
                label_Status.Refresh();
            }

            ((Button)sender).IsEnabled = true;
        }


        private string AddressToWiiPin(string bluetoothAddress)
        {
            if (bluetoothAddress.Length != 12) throw new Exception("Invalid Bluetooth Address: " + bluetoothAddress);

            var bluetoothPin = "";
            for (int i = bluetoothAddress.Length - 2; i >= 0; i -= 2)
            {
                string hex = bluetoothAddress.Substring(i, 2);
                bluetoothPin += (char)Convert.ToInt32(hex, 16);
            }
            return bluetoothPin;
        }
    }
}
