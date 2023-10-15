using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private string _PNPDeviceID;
        private string _DeviceName;
        private string _ProcessorType;
        private string _uniqueDeviceName;

        public List<RegisteredCmd> _RegisteredCmds = new List<RegisteredCmd>();

        private BlockingCollection<string> _TxQueue = new BlockingCollection<string>();
        private CancellationTokenSource _src = new CancellationTokenSource();

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

        public string PNPDeviceID
        {
            get => _PNPDeviceID;
            set
            {
                _PNPDeviceID = value.ToUpper();
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

        public string DeviceName { get => _DeviceName; }
        
        public string ProcessorType { get => _ProcessorType; }
        
        public COMDevice(string pnpDeviceID, string portName, Int32 baudRate = 500000)
        {
            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;

            //Path = path;
            PNPDeviceID = pnpDeviceID;

            _serialPort.Handshake = Handshake.None;

            // format 8-N-1
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;

            _serialPort.NewLine = "\n";

            _serialPort.ReadTimeout = 1000;
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
                _src.Cancel(false);
                Task.WaitAll(tasks, 100);
                _TxQueue.Dispose();
                _TxQueue = null;
                return true;
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

            while (_serialPort.IsOpen)
            {
                try
                {
                    // blocking read
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
                                if ((sCmd.Length >= 4) && (sCmd[3] == '=') && int.TryParse(sCmd.Substring(0, 3), out iDeviceID))
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
                catch (Exception ex)
                {
                    Logger($"RxPump {_serialPort.PortName}: Exception {ex}", 2);
                }
            }
        }

        private void TxPump()
        {
            while (_serialPort.IsOpen)
            {
                try
                {
                    string sCmd;

                    // blocking take
                    sCmd = _TxQueue.Take(_src.Token);

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
                catch (Exception ex)
                {
                    Logger($"RxPump {_serialPort.PortName}: Exception {ex}", 2);
                }
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
                        //_TxQueue.Enqueue($"{RegCmd.iDeviceID:D03}={sData}");
                        _TxQueue.Add($"{RegCmd.iDeviceID:D03}={sData}");
                        Logger($">{_DeviceName}: '=' - MSFSID:{RegCmd.iMSFSID} DeviceID:{RegCmd.iDeviceID} Cmd:{RegCmd.iDeviceID:D03}={sData}", 1);
                    }
                    break;
                case 'T':
                    //_TxQueue.Enqueue(sData);
                    _TxQueue.Add(sData);
                    Logger($">{_DeviceName}: 'T' - Cmd:{sData}", 1);
                    break;
                default:
                    break;
            }
        }
    }
}
