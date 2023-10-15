using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CockpitHardwareHUB
{
    public static class DeviceServer
    {
        private static bool _IsStarted = false;

        //private static SerialPortManager _SerialPortManager = new SerialPortManager(0x04D8);
        private static SerialPortManager _SerialPortManager = new SerialPortManager();

        private static Action<string, int> _Logger = null;
        public static Action<string, int> Logger { get => _Logger; }

        private static Action<string, bool> _SendToMSFS = null;
        public static Action<string, bool> SendToMSFS { get => _SendToMSFS; }

        private static Action<char, COMDevice> _DeviceAddRemove = null;

        private static readonly ConcurrentDictionary<string, COMDevice> _devices = new ConcurrentDictionary<string, COMDevice>();

        private static void OnPortFoundEvent(object sender, SerialPortEventArgs e)
        {
            COMDevice device = new COMDevice(e.PNPDeviceID, e.DeviceID); // default baudrate is 500000
            Task.Run(() => AddDevice(device));
            //AddDevice(device);
            _Logger?.Invoke($"OnPortFoundEvent {e.DeviceID} VendorID: {e.VendorID} ProductID: {e.ProductID} SerialNr: {e.PNPDeviceID}", 0);
        }

        private static void OnPortRemovedEvent(object sender, SerialPortEventArgs e)
        {
            RemoveDevice(e.PNPDeviceID);
            _Logger?.Invoke($"OnPortRemovedEvent {e.DeviceID} VendorID: {e.VendorID} ProductID: {e.ProductID} SerialNr: {e.PNPDeviceID}", 0);
        }

        private static void OnPortAddedEvent(object sender, SerialPortEventArgs e)
        {
            COMDevice device = new COMDevice(e.PNPDeviceID, e.DeviceID); // default baudrate is 500000
            Task.Run(() => AddDevice(device));
            //AddDevice(device);
            _Logger?.Invoke($"OnPortAddedEvent {e.DeviceID} VendorID: {e.VendorID} ProductID: {e.ProductID} SerialNr: {e.PNPDeviceID}", 0);
        }

        // Init DeviceServer
        public static void Init(Action<string, int> Logger, Action<string, bool> SendToMSFS, Action<char, COMDevice> DeviceAddRemove)
        {
            _Logger = Logger;
            _SendToMSFS = SendToMSFS;
            _DeviceAddRemove = DeviceAddRemove;

            // Setup event handlers for scanning serial ports
            _SerialPortManager.OnPortFoundEvent += OnPortFoundEvent;
            _SerialPortManager.OnPortAddedEvent += OnPortAddedEvent;
            _SerialPortManager.OnPortRemovedEvent += OnPortRemovedEvent;
        }

        // Start DeviceServer
        public static void Start()
        {
            if (_IsStarted)
                return;

            // start scanning for serial ports
            // already connected USB devices will be "found"
            // new connected USB devices will be "added"
            _SerialPortManager.scanPorts(true);

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
            _Logger?.Invoke($"DeviceServer.AddDevice({device.PNPDeviceID})", 0);

            if (_devices.ContainsKey(device.PNPDeviceID))
                return;

            if (!device.Open())
                return;

            // Try a maximum of 20 times to connect with COMDevice
            for (int i = 0; i < 20; i++)
            {
                _Logger?.Invoke($"DeviceServer.AddDevice(): Try {i}", 0);
                if (device.Initialize())
                {
                    _devices.TryAdd(device.PNPDeviceID, device);
                    device.RegisterCommands();
                    _DeviceAddRemove('+', device);
                    return;
                }
                else
                    Thread.Sleep(1000);
            }

            // if we fail after 20 times, close the device
            device.Close();
        }

        // Remove a COMDevice
        private static void RemoveDevice(string pnpDeviceID)
        {
            //path = COMDevice.PathToKey(path);
            _devices.TryGetValue(pnpDeviceID, out COMDevice device);

            if (_devices.TryRemove(pnpDeviceID, out device))
            {
                device.Close();
                _DeviceAddRemove('-', device);
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

        public static COMDevice GetDevice(string pnpDeviceID)
        {
            if (_devices.TryGetValue(pnpDeviceID, out COMDevice device))
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
