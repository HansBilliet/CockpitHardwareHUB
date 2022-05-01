using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CockpitHardwareHUB
{
    public class COMDevice
    {
        private readonly byte[] LF = { (byte)'\n' };

        public SerialPort _serialPort = new SerialPort();

        public class Statistics
        {
            public ulong cmdRxCnt;
            public ulong cmdTxCnt;
            public ulong nackCnt;
        }
        public Statistics stats = new Statistics();

        private string _path;
        private string _DeviceName;
        private string _ProcessorType;
        private string _uniqueDeviceName;

        public List<RegisteredCmd> _RegisteredCmds = new List<RegisteredCmd>();

        private ConcurrentQueue<string> _TxQueue = new ConcurrentQueue<string>();
        public bool bTxQueueEmpty { get => (_TxQueue.Count == 0); } 

        private Task _RxPump;
        private Task _TxPump;

        private ManualResetEvent _mreAck = new ManualResetEvent(false);

        public static void Logger(string sLogger, int LogLevel)
        {
            DeviceServer.Logger?.Invoke(sLogger, LogLevel);
        }

        public static void SendToMSFS(string sCmd, bool bRegistered)
        {
            DeviceServer.SendToMSFS?.Invoke(sCmd, bRegistered);
        }

        public string Path
        {
            get => _path;
            set
            {
                _path = value.ToLower();
            }
        }

        public string UniqueDeviceName
        {
            get => _uniqueDeviceName;
            set
            {
                _uniqueDeviceName = value;
            }
        }

        public static string PathToKey(string path)
        {
            int i1 = path.IndexOf('#');
            int i2 = path.LastIndexOf('\\', i1);
            return path.Substring(i2 + 1).ToLower();
        }
        public string DeviceName { get => _DeviceName; }
        public string ProcessorType { get => _ProcessorType; }
        public string Key { get => PathToKey(_path); }

        public COMDevice(string path, string portName, Int32 baudRate = 500000)
        {
            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;

            Path = path;

            _serialPort.Handshake = Handshake.None;

            // format 8-N-1
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;

            _serialPort.NewLine = "\n";

            _serialPort.ReadTimeout = 100;
            _serialPort.WriteTimeout = 100;
        }

        public void ResetStatistics()
        {
            lock (stats)
            {
                stats.cmdRxCnt = 0;
                stats.cmdTxCnt = 0;
                stats.nackCnt = 0;
            }
        }

        private static IEnumerable<RegistryKey> GetSubKeys(RegistryKey key)
        {
            foreach (string keyName in key.GetSubKeyNames())
                using (var subKey = key.OpenSubKey(keyName))
                    yield return subKey;
        }

        // Static helper method to get the Path from a PortName
        public static string GetPathFromPortName(string PortName)
        {
            try
            {
                var enumUsbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
                {
                    if (enumUsbKey == null)
                        throw new ArgumentNullException("USB", "No enumerable USB devices found in registry");
                    foreach (var devVID_PID in GetSubKeys(enumUsbKey))
                    {
                        foreach (var device in GetSubKeys(devVID_PID))
                        {
                            var devParamsKey = device.OpenSubKey("Device Parameters");
                            if (devParamsKey != null)
                            {
                                if (PortName == (string)devParamsKey.GetValue("PortName", ""))
                                {
                                    string Path = (string)devParamsKey.GetValue("SymbolicName", "");
                                    return Path;
                                }
                            }
                        }
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                Logger($"GetPathFromPortName Exception: {ex}", 2);
                return "";
            }
        }

        // Static helper method to get the PortName from a Path
        public static string GetPortNameFromPath(string path)
        {
            try
            {
                // Path format: "\\?\USB#VID_2341&PID_0042#75834353330351E03212#{a5dcbf10-6530-11d2-901f-00c04fb951ed}"

                // Find "#VID_"
                int i1 = path.IndexOf("#VID_");
                if (i1 == -1)
                    return "";

                // Find next "#"
                int i2 = path.IndexOf("#", i1 + 1);
                if (i2 <= i1)
                    return "";

                // Find next "#"
                int i3 = path.IndexOf("#", i2 + 1);
                if (i3 <= i2)
                    return "";

                // Construct key to find
                string keyDevice = path.Substring(i1 + 1, i2 - i1 - 1) + "\\" + path.Substring(i2 + 1, i3 - i2 - 1);
                string PortName = (string)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\USB\{keyDevice}\Device Parameters", "PortName", "");

                if ((PortName == null) || (PortName == ""))
                    return "";

                string SymbolicName = (string)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\USB\{keyDevice}\Device Parameters", "SymbolicName", "");
                if ((SymbolicName == null) || (SymbolicName == ""))
                    return "";

                i1 = path.IndexOf("USB#VID");
                i2 = SymbolicName.IndexOf("USB#VID");
                if (i1 == -1 || i2 == -1)
                    return "";

                if (path.Substring(i1) != SymbolicName.Substring(i2))
                    return "";

                return PortName;
            }
            catch (Exception ex)
            {
                Logger($"GetPortNameFromPath Exception: {ex}", 0);
                return "";
            }
        }
        
        public bool Open()
        {
            try
            {
                _serialPort.Open();
                return true;
            }
            catch (TimeoutException ex)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                Logger($"Open {_serialPort.PortName}: TimeoutException {ex}", 0);
                return false;
            }
            catch (Exception ex)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                Logger($"Open {_serialPort.PortName}: Exception {ex}", 0);
                return false;
            }
        }

        public bool Close()
        {
            if (!_serialPort.IsOpen)
                return false;

            try
            {
                _serialPort.Write("RESET\n");
                _serialPort.Close();

                Task[] tasks = { _TxPump, _RxPump };
                return Task.WaitAll(tasks, 100);
            }
            catch (Exception ex)
            {
                Logger($"Close {_serialPort.PortName}: Exception  {ex}", 0);
                return false;
            }
        }

        private void ClearInputBuffer()
        {
            while (true)
            {
                int readCount = _serialPort.BytesToRead;
                if (readCount == 0)
                    break;
                byte[] buffer = new byte[readCount];
                _serialPort.Read(buffer, 0, readCount);
            }
        }

        public bool Initialize()
        {
            try
            {
                // clean receive buffer of Arduino and this application
                _serialPort.Write("\n");
                ClearInputBuffer();

                // get identification
                _serialPort.Write("IDENT\n");
                _DeviceName = DeviceServer.GetUniqueDeviceName(_serialPort.ReadLine());
                _ProcessorType = _serialPort.ReadLine();
                Logger($"Initialize {_serialPort.PortName}: New Device found: IDENT = \"{_DeviceName}\" - \"{_ProcessorType}\"", 2);

                // counter for DeviceID
                int iDeviceID = 1;

                // register the LED/Data commands
                _serialPort.Write("REGISTER\n");
                while (true)
                {
                    string sRegister = _serialPort.ReadLine();
                    RegisteredCmd rxCmd = new RegisteredCmd(sRegister, iDeviceID++);

                    if (sRegister == "")
                        break;
                    Logger($"{_DeviceName}: REGISTER = \"{sRegister}\"", 2);
                    _RegisteredCmds.Add(rxCmd);
                }
                Logger($"Initialize {_serialPort.PortName}: {_RegisteredCmds.Count} Commands Registered", 2);

                // start transmitting and receiving commands
                _TxPump = Task.Run(() => TxPump());
                _RxPump = Task.Run(() => RxPump());

                return true;
            }
            catch (TimeoutException ex)
            {
                Logger($"Initialize {_serialPort.PortName}: TimeoutException {ex}", 0);
                return false;
            }
            catch (Exception ex)
            {
                Logger($"Initialize {_serialPort.PortName}: Exception {ex}", 0);
                return false;
            }
        }

        private void RxPump()
        {
            var buffer = new byte[1024];
            StringBuilder sbCmd = new StringBuilder("", 256);

            try
            {
                while (_serialPort.IsOpen)
                {
                    if (_serialPort.BytesToRead == 0)
                        continue;

                    int cnt = _serialPort.BaseStream.Read(buffer, 0, 1024);

                    for (int i = 0; i < cnt; i++)
                    {
                        if ((char)buffer[i] == '\n')
                        {
                            if ((sbCmd.Length == 1) && (sbCmd[0] == 'A'))
                                // ACK sequence received
                                _mreAck.Set();
                            else if (sbCmd.Length != 0)
                            {
                                // Command received - check if it has format 'NNN=...' and if it is a RegisteredCmd
                                string sCmd = sbCmd.ToString();
                                int iDeviceID = -1;
                                if ((sCmd.Length >= 4) && (sCmd[3] == '=') && int.TryParse(sCmd.Substring(0,3), out iDeviceID))
                                {
                                    RegisteredCmd RegCmd = _RegisteredCmds.Find(x => x.iDeviceID == iDeviceID);
                                    if (RegCmd != null)
                                        sCmd = $"{RegCmd.iMSFSID:D03}{sCmd.Substring(3)}";
                                    SendToMSFS(sCmd, true); // send to MSFS as registered command
                                }
                                else
                                    SendToMSFS(sCmd, false); // send to MSFS as unregistered command
                                stats.cmdRxCnt++;
                            }
                            sbCmd.Clear();
                        }
                        else
                            sbCmd.Append((char)buffer[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger($"RxPump {_serialPort.PortName}: Exception {ex}", 0);
            }
        }

        private void TxPump()
        {
            try
            {
                while (_serialPort.IsOpen)
                {
                    string sCmd;

                    if (_TxQueue.Count == 0)
                        continue;
                    else
                        _TxQueue.TryDequeue(out sCmd);

                    byte[] buffer = Encoding.ASCII.GetBytes($"{sCmd}\n");

                    int attempts = 2;

                    while (attempts-- != 0)
                    {
                        _serialPort.BaseStream.Write(buffer, 0, sCmd.Length + 1);

                        // Reset the ManualResetEvent and wait for ACK for 50 msec
                        _mreAck.Reset();
                        if (_mreAck.WaitOne(150))
                        {
                            stats.cmdTxCnt++;
                            break;
                        }
                        else
                        {
                            stats.nackCnt++;
                            Logger($"TxPump {_serialPort.PortName}: Not Ack attempt {2 - attempts} for \"{sCmd}\"", 2);
                            // Send linefeed to be sure we are in sync
                            _serialPort.BaseStream.Write(LF, 0, 1);
                        }
                    }
                }

                while (_TxQueue.TryDequeue(out _));
            }
            catch (Exception ex)
            {
                Logger($"RxPump {_serialPort.PortName}: Exception {ex}", 0);
            }
        }

        public void RegisterCommands()
        {
            foreach (RegisteredCmd Cmd in _RegisteredCmds)
            {
                SendToMSFS(Cmd.sCmd, true);
            }
        }

        public void TryAddToTxQueue(int uUniqueID, char cmdType, string sData)
        {
            if (!_serialPort.IsOpen)
                return;
            
            RegisteredCmd RegCmd = null;

            switch (cmdType)
            {
                case '#':
                    RegCmd = _RegisteredCmds.Find(x => x.sCmd == sData);
                    if (RegCmd != null)
                    //if (RegCmd?.iMSFSID != uUniqueID)
                    {
                        // link the command ID with the registered command, and send to device
                        RegCmd.iMSFSID = uUniqueID;
                        Logger($">{_DeviceName}: '#' - MSFSID:{RegCmd.iMSFSID} DeviceID:{RegCmd.iDeviceID} Cmd:{RegCmd.sCmd}", 1);
                    }
                    break;
                case '=':
                    RegCmd = _RegisteredCmds.Find(x => x.iMSFSID == uUniqueID);
                    if (RegCmd != null)
                    {
                        _TxQueue.Enqueue($"{RegCmd.iDeviceID:D03}={sData}");
                        Logger($">{_DeviceName}: '=' - MSFSID:{RegCmd.iMSFSID} DeviceID:{RegCmd.iDeviceID} Cmd:{RegCmd.iDeviceID:D03}={sData}", 1);
                    }
                    break;
                case 'T':
                    _TxQueue.Enqueue(sData);
                    Logger($">{_DeviceName}: 'T' - Cmd:{sData}", 1);
                    break;
                default:
                    break;
            }
        }
    }
}
