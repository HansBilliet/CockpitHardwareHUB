using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.FlightSimulator.SimConnect;

namespace CockpitHardwareHUB
{
    public static class SimConnectClient
    {
        // SimConnect instance
        private static SimConnect _SimConnect = null;

        // Connection Status
        private static bool _bConnected = false;

        // Window handle
        private static IntPtr _handle = new IntPtr(0);

        // Transmit Pump with its TxQueue
        private static ConcurrentQueue<string> _TxQueue = new ConcurrentQueue<string>();
        private static Task _TxPump;

        // Delegates for Inter Module Communication
        private static Action<string, int> _Logger = null;
        private static Action<int, char, string> _SendToDevice = null;
        //private static Action<string> _SendToDevice = null;
        private static Action<bool> _ConnectStatus = null;
        private static Action<char, string, string> _VariableUpdate = null;
        private static Action<Result> _ExeResult = null;

        // List of registered variables
        private static List<VarData> Vars = new List<VarData>();

        // Client Area Data names
        private const string CLIENT_DATA_NAME_COMMAND = "HW.Command";
        private const string CLIENT_DATA_NAME_ACKNOWLEDGE = "HW.Acknowledge";
        private const string CLIENT_DATA_NAME_RESULT = "HW.Result";
        private const string CLIENT_DATA_NAME_VARS = "HW.Vars_";

        // Flag to indicate that Data Area has already been created
        private static bool[] DataAreaVarsUsed = new bool[10];

        // Currently we are using fixed size strings of 256 characters
        private const int MESSAGE_SIZE = 256;
        //private const uint SIZE_CLIENTAREA_VARS = 8192; //SimConnect.SIMCONNECT_CLIENTDATA_MAX_SIZE;

        // Client Area Data ID's
        private enum CLIENT_DATA_ID
        {
            CMD,
            ACK,
            RESULT,
            VARS
        }

        // Client Data Area DefineID's for Command, Acknowledge and Result
        private enum CLIENTDATA_DEFINITION_ID
        {
            CMD,
            ACK,
            RESULT
        }

        // Client Data Area RequestID's for receiving Acknowledge and LVARs
        private enum CLIENTDATA_REQUEST_ID
        {
            ACK,
            RESULT,
            START_VAR
        }

        // Structure sent back from WASM module to acknowledge for LVars
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct VarAck
        {
            public char VarType;
            public UInt16 lvID;
            public UInt16 DefineID;
            public UInt16 Bank;
            public UInt16 Offset;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String str;
            public UInt16 EventID;
            public char ValType;
            public char ValAccess;
            public double f64; // FLOAT64
            public Int32 i32; // INT32
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String s256; // STRING256
        };

        // Some "dummy" enums for type conversions
        private enum SIMCONNECT_DEFINITION_ID { }
        private enum SIMCONNECT_REQUEST_ID { }
        private enum EVENT_ID { }

        // Connection status
        public static bool IsConnected()
        {
            return _bConnected;
        }

        // Keep Window Handle for connecting with SimConnect
        public static void SetHandle(IntPtr handle)
        {
            _handle = handle;
        }

        // WndProc hook
        public static void HandleWndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_USER_SIMCONNECT)
            {
                try
                {
                    _SimConnect?.ReceiveMessage();
                }
                catch (Exception ex)
                {
                    _Logger?.Invoke($"ReceiveSimConnectMessage Error: {ex.Message}", 0);
                    Disconnect();
                }
            }
        }

        public static void Init(
            Action<string, int> Logger,
            Action<int, char, string> SendToDevice,
            Action<bool> ConnectStatus,
            Action<char, string, string> VariableUpdate,
            Action<Result> ExeResult)
        {
            _Logger = Logger;
            _SendToDevice = SendToDevice;
            _ConnectStatus = ConnectStatus;
            _VariableUpdate = VariableUpdate;
            _ExeResult = ExeResult;
        }

        private static void TxPump()
        {
            try
            {
                while (_bConnected)
                {
                    string sCmd;

                    if (_TxQueue.Count == 0)
                        continue;
                    else
                        _TxQueue.TryDequeue(out sCmd);

                    ProcessCmd(sCmd);
                }

                while (_TxQueue.TryDequeue(out _)) ;
            }
            catch (Exception ex)
            {
                _Logger?.Invoke($"TxPump Exception {ex}", 0);
            }
        }

        private static void SetConnectionStatus(bool bConnect)
        {
            if (bConnect == _bConnected)
                return;

            _bConnected = bConnect;

            if (bConnect)
                _TxPump = Task.Run(() => TxPump());
            else
            {
                _TxPump.Wait(100);
                _TxPump = null;
            }

            _ConnectStatus?.Invoke(_bConnected);
        }

        public static void Connect()
        {
            if (_bConnected)
            {
                _Logger?.Invoke("MSFS Already connected", 0);
                return;
            }

            try
            {
                _Logger?.Invoke("Connecting MSFS...", 0);

                _SimConnect = new SimConnect("CockpitHardwareHub", _handle, Constants.WM_USER_SIMCONNECT, null, 0);

                // Listen for connect and quit msgs
                _SimConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                _SimConnect.OnRecvQuit += SimConnect_OnRecvQuit;

                // Listen for Exceptions
                _SimConnect.OnRecvException += SimConnect_OnRecvException;

                // Listen for SimVar Data
                _SimConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;

                // Listen for ClientData
                _SimConnect.OnRecvClientData += SimConnect_OnRecvClientData;
            }
            catch (COMException ex)
            {
                _Logger?.Invoke($"MSFS Connect Error: {ex.Message}", 0);
            }
        }

        public static void Disconnect()
        {
            if (!_bConnected)
            {
                _Logger?.Invoke("MSFS Already disconnected", 0);
                return;
            }

            try
            {
                DeviceServer.Stop();
                SimConnectClient.RemoveAllVariables();

                _Logger?.Invoke("Disconnecting MSFS...", 0);

                // Stop listening for connect and quit msgs
                _SimConnect.OnRecvOpen -= SimConnect_OnRecvOpen;
                _SimConnect.OnRecvQuit -= SimConnect_OnRecvQuit;

                // Stop listening for Exceptions
                _SimConnect.OnRecvException -= SimConnect_OnRecvException;

                // Stop listening for SimVar Data
                _SimConnect.OnRecvSimobjectData -= SimConnect_OnRecvSimobjectData;

                // Stop listening for ClientData
                _SimConnect.OnRecvClientData -= SimConnect_OnRecvClientData;

                _SimConnect.Dispose();
                _SimConnect = null;
            }
            catch (COMException ex)
            {
                _Logger?.Invoke($"MSFS Disconnect Error: {ex.Message}", 0);
            }

            SetConnectionStatus(false);

            _Logger?.Invoke("MSFS Disconnected", 0);
        }

        private static void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            SetConnectionStatus(true);

            _Logger?.Invoke($"SimConnect_OnRecvOpen", 0);
            _Logger?.Invoke($"- Application name: {data.szApplicationName}", 0);
            _Logger?.Invoke($"- Application Version {data.dwApplicationVersionMajor}.{data.dwApplicationVersionMinor} - build {data.dwApplicationBuildMajor}.{data.dwApplicationBuildMinor}", 0);
            _Logger?.Invoke($"- SimConnect  Version {data.dwSimConnectVersionMajor}.{data.dwSimConnectVersionMinor} - build {data.dwSimConnectBuildMajor}.{data.dwSimConnectBuildMinor}", 0);

            InitializeClientDataAreas();

            DeviceServer.Start();
        }

        private static void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            DeviceServer.Stop();
            RemoveAllVariables();

            _Logger?.Invoke("SimConnect_OnRecvQuit", 0);

            Disconnect();

            SetConnectionStatus(false);
        }

        private static void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
            _Logger?.Invoke($"SimConnectDLL.SimConnect_OnRecvException - {eException}", 0);
        }

        private static void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (_SimConnect != null)
            {
                VarData vInList = Vars.Find(x => x.cVarType == 'A' && x.uRequestID == data.dwRequestID);
                if (vInList != null)
                {
                    vInList.oValue = data.dwData[0];
                    _Logger?.Invoke($"SimVar data received: DefineID: {data.dwDefineID} Value: {vInList}", 2);

                    _SendToDevice?.Invoke(vInList.uUniqueID, '=', vInList.sValue);
                    _VariableUpdate?.Invoke('=', vInList.sUniqueID, vInList.sValue);

                    // A Var is fully processed
                    vInList.IsProcessed = true;
                }
            }
        }

        private static void SimConnect_OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            if (data.dwRequestID == (uint)CLIENTDATA_REQUEST_ID.ACK)
            {
                try
                {
                    var ackData = (VarAck)(data.dwData[0]);
                    _Logger?.Invoke($"----> Acknowledge: Name: {ackData.str}, Event: {ackData.EventID}", 2);
                    _Logger?.Invoke($"----> Acknowledge: ID: {ackData.DefineID}, Bank: {ackData.Bank} Offset: {ackData.Offset}", 2);

                    // If Var DefineID already exists, ignore it, otherwise we will get "DUPLICATE_ID" exception
                    if (Vars.Exists(x => (x.cVarType == 'L' || x.cVarType == 'X') && x.uDefineID == ackData.DefineID))
                    {
                        _Logger?.Invoke($"----> Acknowledge: Issue Duplicate DefineID", 2);
                        return;
                    }

                    // Find the Var, and update it with ackData
                    VarData vInList = Vars.Find(x =>
                        x.cVarType == ackData.VarType && 
                        x.cValTypeLX == ackData.ValType && 
                        x.cValAccessLX == ackData.ValAccess && 
                        x.sVarName == ackData.str
                        );
                    if (vInList == null)
                    {
                        _Logger?.Invoke($"----> Acknowledge: Issue Var not found", 2);
                        return;
                    }

                    // Set the ID's - use ackData.DefineID and RequestID only needed if we have a return value
                    vInList.SetID(ackData.DefineID, vInList.cValTypeLX == 'V' ? VarData.NOTUSED_ID : VarData.AUTO_ID);

                    if (vInList.bWrite)
                    {
                        // Register the event
                        vInList.uEventID = ackData.EventID;
                        _SimConnect?.MapClientEventToSimEvent((EVENT_ID)vInList.uUniqueID, $"HW.EVENT_{vInList.uEventID:d04}");
                        _Logger?.Invoke($"----> Acknowledge: Registered for write", 2);
                    }

                    // Send back to the devices
                    _SendToDevice?.Invoke(vInList.uUniqueID, '#', vInList.sVar);
                    _VariableUpdate?.Invoke('#', vInList.sUniqueID, vInList.sVar);

                    // Register for listening to return value only if required
                    if (vInList.bRead)
                    {
                        vInList.uBank = ackData.Bank; 
                        vInList.uOffset = ackData.Offset;

                        switch (ackData.ValType)
                        {
                            case 'I':
                                vInList.oValue = ackData.i32;
                                _SimConnect?.AddToClientDataDefinition(
                                    (SIMCONNECT_DEFINITION_ID)ackData.DefineID,
                                    vInList.uOffset,
                                    sizeof(Int32),
                                    0,
                                    0);
                                _SimConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, Int32>((SIMCONNECT_DEFINITION_ID)vInList.uDefineID);
                                break;
                            case 'F':
                                vInList.oValue = ackData.f64;
                                _SimConnect?.AddToClientDataDefinition(
                                    (SIMCONNECT_DEFINITION_ID)ackData.DefineID,
                                    vInList.uOffset,
                                    sizeof(double),
                                    0,
                                    0);
                                _SimConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, double>((SIMCONNECT_DEFINITION_ID)vInList.uDefineID);
                                break;
                            case 'S':
                                String256 obj;
                                obj.Value = ackData.s256;
                                vInList.oValue = obj;
                                _SimConnect?.AddToClientDataDefinition(
                                    (SIMCONNECT_DEFINITION_ID)ackData.DefineID,
                                    vInList.uOffset,
                                    256,
                                    0,
                                    0);
                                _SimConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, String256>((SIMCONNECT_DEFINITION_ID)vInList.uDefineID);
                                break;
                        }

                        if (!DataAreaVarsUsed[vInList.uBank])
                        {
                            // Avoid to map Client Area Data more than once
                            DataAreaVarsUsed[vInList.uBank] = true;
                            _SimConnect?.MapClientDataNameToID($"{CLIENT_DATA_NAME_VARS}{vInList.uBank}", CLIENT_DATA_ID.VARS + vInList.uBank);
                        }

                        _SimConnect?.RequestClientData(
                            CLIENT_DATA_ID.VARS + vInList.uBank,
                            (SIMCONNECT_REQUEST_ID)vInList.uRequestID,
                            (SIMCONNECT_DEFINITION_ID)vInList.uDefineID,
                            SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET, // data will be sent whenever SetClientData is used on this client area (even if this defineID doesn't change)
                            SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED, // if this is used, this defineID only is sent when its value has changed
                            0, 0, 0);

                        _SendToDevice?.Invoke(vInList.uUniqueID, '=', vInList.sValue);
                        _VariableUpdate?.Invoke('=', vInList.sUniqueID, vInList.sValue);

                        _Logger?.Invoke($"----> Acknowledge: Registered for read", 2);
                    }
                    // L or X Var is fully processed
                    vInList.IsProcessed = true;
                }
                catch (Exception ex)
                {
                    _Logger?.Invoke($"SimConnect_OnRecvClientData Error: {ex.Message}", 0);
                }
            }
            else if (data.dwRequestID == (uint)CLIENTDATA_REQUEST_ID.RESULT)
            {
                try
                {
                    var exeResult = (Result)data.dwData[0];
                    _Logger?.Invoke($"----> Result: float: {exeResult.exeF}, int: {exeResult.exeI}, string: {exeResult.exeS}", 2);

                    _ExeResult?.Invoke(exeResult);
                }
                catch (Exception ex)
                {
                    _Logger?.Invoke($"SimConnect_OnRecvClientData Error: {ex.Message}", 0);
                }
            }
            else if (data.dwRequestID >= (uint)CLIENTDATA_REQUEST_ID.START_VAR)
            {
                VarData vInList = Vars.Find(x => x.uRequestID == data.dwRequestID);

                if (vInList != null)
                {
                    vInList.oValue = data.dwData[0];
                    _Logger?.Invoke($"Var data received: DefineID: {data.dwDefineID} Offset: {vInList.uOffset} Value: {vInList}", 2);

                    _SendToDevice?.Invoke(vInList.uUniqueID, '=', vInList.sValue);
                    _VariableUpdate?.Invoke('=', vInList.sUniqueID, vInList.sValue);
                }
                else
                    _Logger?.Invoke($"Var data received: unknown LVar DefineID {data.dwDefineID}", 2);
            }
        }

        private static void InitializeClientDataAreas()
        {
            try
            {
                // register Client Data (for WASM Module Commands)
                _SimConnect?.MapClientDataNameToID(CLIENT_DATA_NAME_COMMAND, CLIENT_DATA_ID.CMD);
                _SimConnect?.CreateClientData(CLIENT_DATA_ID.CMD, MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);
                _SimConnect?.AddToClientDataDefinition(CLIENTDATA_DEFINITION_ID.CMD, 0, MESSAGE_SIZE, 0, 0);

                // register Client Data (for Var acknowledge)
                _SimConnect?.MapClientDataNameToID(CLIENT_DATA_NAME_ACKNOWLEDGE, CLIENT_DATA_ID.ACK);
                _SimConnect?.CreateClientData(CLIENT_DATA_ID.ACK, (uint)Marshal.SizeOf<VarAck>(), SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);
                _SimConnect?.AddToClientDataDefinition(CLIENTDATA_DEFINITION_ID.ACK, 0, (uint)Marshal.SizeOf<VarAck>(), 0, 0);
                _SimConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, VarAck>(CLIENTDATA_DEFINITION_ID.ACK);
                _SimConnect?.RequestClientData(
                    CLIENT_DATA_ID.ACK,
                    CLIENTDATA_REQUEST_ID.ACK,
                    CLIENTDATA_DEFINITION_ID.ACK,
                    SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,
                    0, 0, 0);

                // register Client Data (for RESULT)
                _SimConnect?.MapClientDataNameToID(CLIENT_DATA_NAME_RESULT, CLIENT_DATA_ID.RESULT);
                _SimConnect?.CreateClientData(CLIENT_DATA_ID.RESULT, (uint)Marshal.SizeOf<Result>(), SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);
                _SimConnect?.AddToClientDataDefinition(CLIENTDATA_DEFINITION_ID.RESULT, 0, (uint)Marshal.SizeOf<Result>(), 0, 0);
                _SimConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, Result>(CLIENTDATA_DEFINITION_ID.RESULT);
                _SimConnect?.RequestClientData(
                    CLIENT_DATA_ID.RESULT,
                    CLIENTDATA_REQUEST_ID.RESULT,
                    CLIENTDATA_DEFINITION_ID.RESULT,
                    SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,
                    0, 0, 0);
            }
            catch (Exception ex)
            {
                _Logger?.Invoke($"InitializeClientDataAreas Error: {ex.Message}", 0);
            }
        }

        private static void SendWASMCmd(String command)
        {
            if (!_bConnected)
            {
                _Logger?.Invoke("Not connected", 2);
                return;
            }

            String256 cmd;
            cmd.Value = command;

            try
            {
                _SimConnect.SetClientData(
                    CLIENT_DATA_ID.CMD,
                    CLIENTDATA_DEFINITION_ID.CMD,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    cmd
                );
            }
            catch (Exception ex)
            {
                _Logger?.Invoke($"SendWASMCmd Error: {ex.Message}", 0);
            }
        }

        public static void ExecuteCalculatorCode(string sExe)
        {
            SendWASMCmd($"HW.Exe.{sExe}");
        }

        private static bool AddVariable(string sVar)
        {

            if (!_bConnected)
            {
                _Logger?.Invoke("Not connected", 0);
                return false;
            }

            // Check if variable already exists
            VarData v = Vars.Find(x => x.sVar == sVar);
            if (v != null)
            {
                _Logger?.Invoke($"Variable \"{sVar}\" already exists", 0);

                if (!v.IsProcessed)
                    // Variable still not fully processed, some data will still be missing - Invokes will still be issued through the normal process
                    return true;

                // Variables have already been fully processed, so we can immediately issue the Invokes
                _SendToDevice?.Invoke(v.uUniqueID, '#', v.sVar);
                if (v.bRead)
                    _SendToDevice?.Invoke(v.uUniqueID, '=', v.sValue);
                _VariableUpdate?.Invoke('#', v.sUniqueID, v.sVar);

                return true;
            }

            // Create a new variable
            v = new VarData(sVar);

            if (v.Result != ParseResult.Ok)
            {
                _Logger?.Invoke($"Command error: {v.Result}", 0);
                return false;
            }

            // Add the variable in the list
            Vars.Add(v);

            // Register the variable
            switch (v.cVarType)
            {
                case 'A':
                    // Register a SimVar
                    try
                    {
                        // Registration of a SimVar
                        v.SetID(VarData.AUTO_ID, v.bRead ? VarData.AUTO_ID : VarData.NOTUSED_ID);
                        _SimConnect.AddToDataDefinition(
                            (SIMCONNECT_DEFINITION_ID)v.uDefineID,
                            v.sVarName,
                            v.sUnit == "STRING" ? "" : v.sUnit,
                            v.scValType,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        if (v.bRead)
                        {
                            switch (v.scValType)
                            {
                                case SIMCONNECT_DATATYPE.INT32:
                                    _SimConnect.RegisterDataDefineStruct<Int32>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.INT64:
                                    _SimConnect.RegisterDataDefineStruct<Int64>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.FLOAT32:
                                    _SimConnect.RegisterDataDefineStruct<float>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.FLOAT64:
                                    _SimConnect.RegisterDataDefineStruct<Double>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.STRING8:
                                    _SimConnect.RegisterDataDefineStruct<String8>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.STRING32:
                                    _SimConnect.RegisterDataDefineStruct<String32>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.STRING64:
                                    _SimConnect.RegisterDataDefineStruct<String64>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.STRING128:
                                    _SimConnect.RegisterDataDefineStruct<String128>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.STRING256:
                                    _SimConnect.RegisterDataDefineStruct<String256>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                                case SIMCONNECT_DATATYPE.STRING260:
                                    _SimConnect.RegisterDataDefineStruct<String260>((SIMCONNECT_DEFINITION_ID)v.uDefineID);
                                    break;
                            }

                            _SimConnect.RequestDataOnSimObject(
                                (SIMCONNECT_REQUEST_ID)v.uRequestID,
                                (SIMCONNECT_DEFINITION_ID)v.uDefineID,
                                0,
                                SIMCONNECT_PERIOD.SIM_FRAME,
                                SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                                0, 0, 0);
                        }

                        _SendToDevice?.Invoke(v.uUniqueID, '#', v.sVar);
                        _VariableUpdate?.Invoke('#', v.sUniqueID, v.sVar);
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"Register SimVar Error: {ex.Message}", 0);
                    }
                    break;
                case 'L':
                    // Register an LVar
                    SendWASMCmd($"HW.RegL{v.cValTypeLX}{v.cValAccessLX}.{v.sVarName}");
                    break;
                case 'X':
                    SendWASMCmd($"HW.RegX{v.cValTypeLX}{v.cValAccessLX}.{v.sVarName}");
                    break;
                case 'K':
                    // Register a K-event
                    try
                    {
                        v.SetID(VarData.AUTO_ID, VarData.NOTUSED_ID);

                        // Registration of a SimConnect Event
                        _SimConnect?.MapClientEventToSimEvent((EVENT_ID)v.uUniqueID, v.sVarName);
                        _Logger?.Invoke($"K: {v.sVarName} SimConnect Event added and registered", 2);

                        _SendToDevice?.Invoke(v.uUniqueID, '#', v.sVar);
                        _VariableUpdate?.Invoke('#', v.sUniqueID, v.sVar);

                        // K Var is fully processed
                        v.IsProcessed = true;
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"Register K-event Error: {ex.Message}", 2);
                    }
                    break;
                default:
                    break;
            }

            _Logger?.Invoke($"AddVariable(\"{v.scValType}\", \"{v.cVarType}\", \"{v.sVarName}\") - unit = \"{v.sUnit}\"", 2);
            return true;
        }

        private static bool SetVariable(VarData v, string sValue)
        {
            if (!_bConnected)
            {
                _Logger?.Invoke("Not connected", 0);
                return false;
            }

            if (!v.bWrite)
            {
                _Logger?.Invoke("Variable can not be set", 0);
                return false;
            }

            if (v.scValType == SIMCONNECT_DATATYPE.INVALID && sValue != "")
            {
                _Logger?.Invoke("VOID can not have a value", 0);
                return false;
            }

            switch (v.cVarType)
            {
                case 'A':
                    try
                    {
                        object obj = v.ConvertValue(sValue);
                        _SimConnect.SetDataOnSimObject((SIMCONNECT_DEFINITION_ID)v.uDefineID, 0, SIMCONNECT_DATA_SET_FLAG.DEFAULT, obj);
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"SetValue SimVar Error: {ex.Message}", 0);
                    }
                    break;

                case 'K':
                    try
                    {
                        // K variables can only be INT32 or VOID
                        object obj = v.ConvertValue(sValue);
                        uint u;
                        if (obj == null)
                            u = 0;
                        else
                            u = (uint)(Int32)obj;
                        _SimConnect.TransmitClientEvent(
                            0,
                            //(EVENT_ID)v.uDefineID,
                            (EVENT_ID)v.uUniqueID,
                            u,
                            (EVENT_ID)SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST,
                            SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"SetValue SimConnect Event Error: {ex.Message}", 0);
                    }
                    break;

                case 'L':
                case 'X':
                    try
                    {
                        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated

                        object obj = v.ConvertValue(sValue);
                        switch (v.scValType)
                        {
                            case SIMCONNECT_DATATYPE.INT32:
                                {
                                    uint u = (uint)(Int32)obj;
                                    _SimConnect.TransmitClientEvent(
                                        0,
                                        //(EVENT_ID)v.uDefineID,
                                        (EVENT_ID)v.uUniqueID,
                                        u,
                                        (EVENT_ID)SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST,
                                        SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    break;
                                }
                            case SIMCONNECT_DATATYPE.FLOAT64:
                                FormattableString sCmd = $"{v.uEventID:d04}={obj}";
                                SendWASMCmd(sCmd.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")));
                                break;
                            case SIMCONNECT_DATATYPE.STRING256:
                                {
                                    string s = ((String256)obj).Value;
                                    SendWASMCmd($"{v.uEventID:d04}={s}");
                                    break;
                                }
                            case SIMCONNECT_DATATYPE.INVALID:
                                _SimConnect.TransmitClientEvent(
                                    0,
                                    //(EVENT_ID)v.uDefineID,
                                    (EVENT_ID)v.uUniqueID,
                                    0,
                                    (EVENT_ID)SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST,
                                    SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                break;
                            default:
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"SetValue SimConnect Event Error: {ex.Message}", 0);
                    }
                    break;
            }

            return true;
        }

        private static bool RemoveVariable(VarData v)
        {
            if (!_bConnected)
            {
                _Logger?.Invoke("Not connected", 0);
                return false;
            }

            if (!Vars.Remove(v))
            {
                _Logger?.Invoke("Remove failed", 0);
                return false;
            }

            // Unregister the variable
            switch (v.cVarType)
            {
                case 'A':
                    // Unregister a SimVar
                    try
                    {
                        _SimConnect.RequestDataOnSimObject(
                            (SIMCONNECT_REQUEST_ID)v.uRequestID,
                            (SIMCONNECT_DEFINITION_ID)v.uDefineID,
                            0,
                            SIMCONNECT_PERIOD.NEVER,
                            SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                            0, 0, 0);
                        _SimConnect.ClearDataDefinition((SIMCONNECT_DEFINITION_ID)v.uDefineID);

                        _Logger?.Invoke($"{v} SimVar removed and Unregistered", 2);
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"Unregister SimVar Error: {ex.Message}", 0);
                    }
                    break;

                case 'L':
                case 'X':
                    // Unregister an LVar or XVar
                    try
                    {
                        _SimConnect?.RequestClientData(
                            CLIENT_DATA_ID.VARS + v.uBank,
                            //CLIENT_DATA_ID.LVARS,
                            (SIMCONNECT_REQUEST_ID)v.uRequestID,
                            (SIMCONNECT_DEFINITION_ID)v.uDefineID,
                            SIMCONNECT_CLIENT_DATA_PERIOD.NEVER,
                            SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,
                            0, 0, 0);
                        _SimConnect?.ClearClientDataDefinition((SIMCONNECT_DEFINITION_ID)v.uDefineID);

                        _Logger?.Invoke($"{v} Var removed and Unregistered", 2);
                    }
                    catch (Exception ex)
                    {
                        _Logger?.Invoke($"Unregister LVar Error: {ex.Message}", 2);
                    }
                    break;

                case 'K':
                    // Unregister a KVar --> Not really neccessary
                    _Logger?.Invoke($"{v} Event removed", 2);
                    break;
            }

            _VariableUpdate?.Invoke('-', v.sUniqueID, v.sVar);

            return true;
        }

        public static void OnSendToMSFS(string sCmd, bool bRegistered)
        {
            if (_bConnected)
            {
                if (bRegistered)
                    _TxQueue.Enqueue(sCmd);
                _Logger?.Invoke($">MSFS {sCmd}", 1);
            }
        }

        public static void ProcessCmd(string sCmd)
        {
            // Possible commands from Device or Commandline
            // NNN              SetVariable without value
            // NNN=Data         SetVariable with value
            // NNN-             RemoveVariable
            // RETRN_RW_X:...   AddVariable type X with return type RETRN
            // VOID_X:...       AddVariable type X without return type

            if (sCmd.Length >= 3)
            {
                if (int.TryParse(sCmd.Substring(0, 3), out int iCmd))
                {
                    // Possible Numeric command
                    VarData v = Vars.Find(x => x.uUniqueID == iCmd);
                    if (v != null)
                    {
                        if (sCmd.Length == 3)
                            SetVariable(v, "");
                        else if ((sCmd.Length >= 4) && (sCmd[3] == '='))
                            SetVariable(v, sCmd.Substring(4).Trim());
                        else if ((sCmd.Length == 4) && (sCmd[3] == '-'))
                            RemoveVariable(v);
                        else
                            _Logger?.Invoke("Command error: Unknown variable command", 2);
                    }
                    else
                        _Logger?.Invoke("Command error: Variable not found", 2);
                }
                else
                    // Possible registration
                    AddVariable(sCmd);
            }
            else
                _Logger?.Invoke("Command error: Should be at least 3 characters", 2);

            return;
        }

        public static void RemoveAllVariables()
        {
            for (int i = 0; i < 10; i++)
                DataAreaVarsUsed[i] = false;

            while (Vars.Count != 0)
            {
                ProcessCmd($"{Vars[0].uUniqueID:D3}-");
            }

            VarData.ResetUniqueID();
        }
    }
}
