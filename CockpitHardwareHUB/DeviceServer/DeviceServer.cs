using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CockpitHardwareHUB
{
    public static class DeviceServer
    {
        private static bool _IsStarted = false;

        private static IntPtr _hUsbEventHandle;

        private static Action<string, int> _Logger = null;
        public static Action<string, int> Logger { get => _Logger; }

        private static Action<string, bool> _SendToMSFS = null;
        public static Action<string, bool> SendToMSFS { get => _SendToMSFS; }

        private static Action<char, COMDevice> _DeviceAddRemove = null;

        private static readonly ConcurrentDictionary<string, COMDevice> _devices = new ConcurrentDictionary<string, COMDevice>();

        // Register to receive USB Raw device Arrival or Deviceremovecomplete messages
        public static void RegisterForUsbEvents(IntPtr hWnd)
        {
            var devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
            var devBroadcastDeviceInterfaceBuffer = IntPtr.Zero;

            try
            {
                var size = Marshal.SizeOf(devBroadcastDeviceInterface);
                devBroadcastDeviceInterface.dbcc_size = size;
                devBroadcastDeviceInterface.dbcc_devicetype = Constants.DbtDevtypDeviceinterface;
                devBroadcastDeviceInterface.dbcc_reserved = 0;
                devBroadcastDeviceInterface.dbcc_classguid = new Guid("a5dcbf10-6530-11d2-901f-00c04fb951ed"); // USB Raw device

                devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);

                _hUsbEventHandle = User32.RegisterDeviceNotification(hWnd, devBroadcastDeviceInterfaceBuffer, Constants.DeviceNotifyWindowHandle);
            }
            catch (Exception ex)
            {
                _Logger?.Invoke($"DeviceNotifier.RegisterForUsbEvents() Exception: {ex}", 0);
                _hUsbEventHandle = IntPtr.Zero;
            }
            finally
            {
                // Free the memory allocated previously by AllocHGlobal.
                if (devBroadcastDeviceInterfaceBuffer != IntPtr.Zero)
                {
                    try
                    {
                        Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"DeviceNotifier.RegisterForUsbEvents() Exception: {ex}", 0);
                    }
                }
            }
        }

        // Unregister to receive USB Raw device Arrival or Deviceremovecomplete messages
        public static void UnRegisterForUsbEvents()
        {
            User32.UnregisterDeviceNotification(_hUsbEventHandle);
        }

        // WndProc hook to process USB Raw device Arrival or Deviceremovecomplete messages
        public static void HandleWndProc(ref Message m)
        {
            // Check if we got a device change message from a USB Raw device
            if (!_IsStarted || (m.Msg != Constants.WmDevicechange))
                return;

            // Check if a USB Raw device was inserted or removed
            Int32 msg = m.WParam.ToInt32();
            if (msg != Constants.DbtDevicearrival && msg != Constants.DbtDeviceremovecomplete)
                return;

            // Check if device change is for "Class of devices" (DBT_DEVTYP_DEVICEINTERFACE)
            var devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
            Marshal.PtrToStructure(m.LParam, devBroadcastDeviceInterface);
            if (devBroadcastDeviceInterface.dbcc_devicetype != Constants.DbtDevtypDeviceinterface)
                return;

            // Process the COMDevice Arrival or Deviceremovecomplete messages
            switch (msg)
            {
                case Constants.DbtDevicearrival:
                    string PortName = COMDevice.GetPortNameFromPath(devBroadcastDeviceInterface.dbcc_name);
                    COMDevice device = new COMDevice(devBroadcastDeviceInterface.dbcc_name, PortName); // default baudrate is 500000
                    Task.Run(() => AddDevice(device));
                    _Logger?.Invoke($"Devicearrival for {devBroadcastDeviceInterface.dbcc_name}", 0);
                    break;

                case Constants.DbtDeviceremovecomplete:
                    RemoveDevice(devBroadcastDeviceInterface.dbcc_name);
                    _Logger?.Invoke($"Deviceremovecomplete for {devBroadcastDeviceInterface.dbcc_name}", 0);
                    break;
            }
        }

        // Init DeviceServer
        public static void Init(Action<string, int> Logger, Action<string, bool> SendToMSFS, Action<char, COMDevice> DeviceAddRemove)
        {
            _Logger = Logger;
            _SendToMSFS = SendToMSFS;
            _DeviceAddRemove = DeviceAddRemove;
        }

        // Start DeviceServer
        public static void Start()
        {
            if (_IsStarted)
                return;

            string[] portNames = SerialPort.GetPortNames();
            string path;

            foreach (string portName in portNames)
            {
                path = COMDevice.GetPathFromPortName(portName);
                if (path != "")
                {
                    COMDevice device = new COMDevice(path, portName);
                    AddDevice(device);
                }
            }

            _IsStarted = true;
        }

        // Stop DeviceServer by removing all devices
        public static void Stop()
        {
            if (!_IsStarted)
                return;

            _IsStarted = false;

            foreach (KeyValuePair<string, COMDevice> pair in _devices.ToArray())
                RemoveDevice(pair.Key);

            _devices.Clear();
        }

        // Add a COMDevice
        private static void AddDevice(COMDevice device)
        {
            _Logger?.Invoke($"DeviceServer.AddDevice({device.Path})", 0);

            if (_devices.ContainsKey(device.Key))
                return;

            if (!device.Open())
                return;

            // Try a maximum of 20 times to connect with COMDevice
            for (int i = 0; i < 20; i++)
            {
                _Logger?.Invoke($"DeviceServer.AddDevice(): Try {i}", 0);
                if (device.Initialize())
                {
                    _devices.TryAdd(device.Key, device);
                    device.RegisterCommands();
                    _DeviceAddRemove('+', device);
                    //_DeviceNotification?.Invoke(device, new NotifyData(NotifyAction.deviceAdded));
                    return;
                }
                else
                    Thread.Sleep(1000);
            }

            // if we fail after 20 times, close the device
            device.Close();
        }

        // Remove a COMDevice
        private static void RemoveDevice(string path)
        {
            path = COMDevice.PathToKey(path);
            _devices.TryGetValue(path, out COMDevice device);

            if (_devices.TryRemove(path, out device))
            {
                device.Close();
                _DeviceAddRemove('-', device);
                //_DeviceNotification?.Invoke(device, new NotifyData(NotifyAction.deviceRemoved));
            }
        }

        public static string GetUniqueDeviceName(string fmgsName)
        {
            int maxIndex = 0;
            int index = 1;

            foreach (KeyValuePair<string, COMDevice> pair in _devices.ToArray())
            {
                COMDevice device = pair.Value;

                string[] parts = device.DeviceName.Split(':');
                if (parts[0] == fmgsName)
                {
                    // fmgsName already exists, means that next index is at least 1
                    if (parts.Length > 1)
                    {
                        // fmgsName already exists with an index, means that next index is at least this index + 1
                        int.TryParse(parts[1], out index);
                        index++;
                    }
                    if (index > maxIndex) maxIndex = index;
                }
            }

            // if maxIndex == 0, don't add index, otherwise add "_index"
            fmgsName += (maxIndex == 0) ? "" : $":{maxIndex}";
            return fmgsName;
        }

        public static COMDevice GetDevice(string key)
        {
            if (_devices.TryGetValue(key, out COMDevice device))
                return device;
            else
                return null;
        }

        public static void ResetStatistics()
        {
            foreach (KeyValuePair<string, COMDevice> pair in _devices.ToArray())
                pair.Value.ResetStatistics();
        }

        public static void OnSendToDevice(string sCommand)
        {
            if ((sCommand.Length >= 4) && (sCommand[3] == '=') && int.TryParse(sCommand.Substring(0, 3), out int iCmd))
            {
                // NNN=
                // NNN=Data
                OnSendToDevice(iCmd, '=', sCommand.Substring(4));
            }
            else
                // all other commands
                OnSendToDevice(0, 'T', sCommand);
        }

        public static void OnSendToDevice(int uUniqueID, char cmdType, string sData)
        {
            foreach (KeyValuePair<string, COMDevice> pair in _devices.ToArray())
                pair.Value.TryAddToTxQueue(uUniqueID, cmdType, sData);
        }

        /// <summary>
        /// Test transmissions
        /// </summary>
        /// 
        public static void SendTest(COMDevice device)
        {
            if (device == null)
                return;

            try
            {
                int counter = 0;
                int i = 0;

                Stopwatch sw = Stopwatch.StartNew();

                while (counter++ != 100)
                {
                    foreach (RegisteredCmd regCmd in device._RegisteredCmds)
                    {
                        device.TryAddToTxQueue(0, 'T', "TEST=1");
                        i++;
                    }
                    while (!device.bTxQueueEmpty) ;
                }

                sw.Stop();

                _Logger?.Invoke($"Device: {device.DeviceName} - Total time: {sw.ElapsedMilliseconds} - Total commands: {i} - Cmds/sec: {(double)i / (double)sw.ElapsedMilliseconds * 1000}", 0);
            }
            catch (Exception ex)
            {
                _Logger?.Invoke($"SendTest Exception: {ex}", 0);
            }
        }
    }
}
