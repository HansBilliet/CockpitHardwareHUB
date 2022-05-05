using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Text.RegularExpressions;

namespace CockpitHardwareHUB
{
    public class VarData
    {
        public const UInt16 AUTO_ID = 0xFFFF;
        public const UInt16 NOTUSED_ID = 0xFFFE;

        static private UInt16 uNewUniqueID = 1; // 0 is not allowed
        static private UInt16 uNewDefineID = 0;
        static private UInt16 uNewRequestID = 10;

        private UInt16 _uDefineID;
        public UInt16 uDefineID { get => _uDefineID; }

        private UInt16 _uRequestID;
        public UInt16 uRequestID { get => _uRequestID; }

        private UInt16 _uOffset;
        public UInt16 uOffset { get => _uOffset;  set => _uOffset = value; }

        private UInt16 _uBank;
        public UInt16 uBank { get => _uBank; set => _uBank = value; }

        private string _sVar;
        public string sVar { get => _sVar; }

        private SIMCONNECT_DATATYPE _scValType;
        public SIMCONNECT_DATATYPE scValType { get => _scValType; }

        public char cValTypeLX { get
            {
                switch (_scValType)
                {
                    case SIMCONNECT_DATATYPE.INT32:
                    case SIMCONNECT_DATATYPE.INT64:
                        return 'I';
                    case SIMCONNECT_DATATYPE.FLOAT32:
                    case SIMCONNECT_DATATYPE.FLOAT64:
                        return 'F';
                    case SIMCONNECT_DATATYPE.STRING8:
                    case SIMCONNECT_DATATYPE.STRING32:
                    case SIMCONNECT_DATATYPE.STRING64:
                    case SIMCONNECT_DATATYPE.STRING128:
                    case SIMCONNECT_DATATYPE.STRING256:
                    case SIMCONNECT_DATATYPE.STRING260:
                        return 'S';
                    default:
                        return 'V';
                }
            }
        }

        public char cValAccessLX { get
            {
                if (_bRead && _bWrite)
                    return 'B';
                else if (_bRead)
                    return 'R';
                else if (_bWrite)
                    return 'W';
                else
                    return 'N';
            }
        }

        private bool _bRead;
        public bool bRead { get => _bRead; }

        private bool _bWrite;
        public bool bWrite { get => _bWrite; }

        private char _cVarType;
        public char cVarType { get => _cVarType; }

        private string _sVarName;
        public string sVarName { get => _sVarName; }

        private string _sUnit;
        public string sUnit { get => _sUnit; }

        private UInt16 _uEventID;
        public UInt16 uEventID { get => _uEventID; set => _uEventID = value; }

        private object _oValue;
        public object oValue { get => _oValue; set => _oValue = value; }

        public string sValue { get
            {
                switch (_scValType)
                {
                    case SIMCONNECT_DATATYPE.INT32:
                    case SIMCONNECT_DATATYPE.INT64:
                    case SIMCONNECT_DATATYPE.FLOAT32:
                    case SIMCONNECT_DATATYPE.FLOAT64:
                        return oValue.ToString();
                    case SIMCONNECT_DATATYPE.STRING8:
                        return ((String8)_oValue).Value;
                    case SIMCONNECT_DATATYPE.STRING32:
                        return ((String32)_oValue).Value;
                    case SIMCONNECT_DATATYPE.STRING64:
                        return ((String64)_oValue).Value;
                    case SIMCONNECT_DATATYPE.STRING128:
                        return ((String128)_oValue).Value;
                    case SIMCONNECT_DATATYPE.STRING256:
                        return ((String256)_oValue).Value;
                    case SIMCONNECT_DATATYPE.STRING260:
                        return ((String260)_oValue).Value;
                    default:
                        return "";
                }
            }
        }

        private UInt16 _uUniqueID = 0;
        public int uUniqueID { get => _uUniqueID; }
        public string sUniqueID { get => uUniqueID.ToString("D3"); }
        
        private int _iClientDataID = -1;
        public int iClientDataID { get => _iClientDataID; set => _iClientDataID = value; }

        private ParseResult _Result;
        public ParseResult Result { get => _Result; }

        private bool _IsProcessed = false;
        public bool IsProcessed { get => _IsProcessed; set => _IsProcessed = value; }

        public VarData(string sVar)
        {
            // Keep the original variable name for comparison reasons
            // Example: FLOAT64_RW_A:AUTOPILOT ALTITUDE LOCK VAR:3,feet
            _sVar = sVar;

            // Extract ValType
            if (new Regex(@"^VOID_[ALKX]:.+$", RegexOptions.IgnoreCase).IsMatch(sVar))
            {
                // VOID_A:Command[,Unit]
                _scValType = SIMCONNECT_DATATYPE.INVALID;
                _oValue = null;
                _bRead = false;
                _bWrite = true;
            }
            else if (new Regex(@"^([A-Z0-9]+)_[RW]((?<!W)W){0,1}_[ALKX]:.+$", RegexOptions.IgnoreCase).IsMatch(sVar))
            {
                // VALTYPE_RW_A:Command[,Unit]
                int iUnderscore = sVar.IndexOf('_');
                if (!GetValType(sVar.Substring(0, iUnderscore)))
                {
                    _Result = ParseResult.UnsupportedValType;
                    return;
                }
                _bRead = (char.ToUpper(sVar[iUnderscore + 1]) == 'R');
                _bWrite = (char.ToUpper(sVar[iUnderscore + 1]) == 'W' || char.ToUpper(sVar[iUnderscore + 2]) == 'W');
            }
            else
            {
                _Result = ParseResult.UnsupportedFormat;
                return;
            }

            // Extract VarType
            int iColon = sVar.IndexOf(':');
            _cVarType = char.ToUpper(sVar[iColon - 1]);

            // Extract the trailing Unit if it exists
            int iComma = sVar.LastIndexOf(',');
            if ((iComma != -1) && (sVar.Length > iComma + 1))
            {
                // Extract the Unit
                _sUnit = sVar.Substring(iComma + 1).Trim().ToUpper();
                // Avoid that we don't take a part of the variable name that contains a comma
                // Example: INT32_X:4 (>L:A32NX_EFIS_L_OPTION,enum) (L:A32NX_EFIS_L_OPTION,enum)
                // The above would return ",enum)", but the ')' character indicates that it's not a Unit, but part of the variable name
                if (new Regex(@"^[A-Z0-9]+$").IsMatch(_sUnit))
                    sVar = sVar.Substring(0, iComma).Trim();
                else
                    _sUnit = "";
            }
            else
                _sUnit = "";

            // Extract VarName
            if (sVar.Length < iColon + 2)
            {
                _Result = ParseResult.MissingVarName;
                return;
            }
            _sVarName = sVar.Substring(iColon + 1).Trim();

            // A and L variables require a Unit
            if (((_cVarType == 'A') || (_cVarType == 'L')) && (_sUnit == ""))
            {
                _Result = ParseResult.MissingUnit;
                return;
            }

            // K variables can only be INT32 or VOID
            SIMCONNECT_DATATYPE[] KTypes = {
                SIMCONNECT_DATATYPE.INT32,
                SIMCONNECT_DATATYPE.INVALID
            };
            if (_cVarType == 'K' && !Array.Exists(KTypes, x => x == _scValType))
            {
                _Result = ParseResult.KVarUnsupportedValType;
                return;
            }

            // K variables can only be write
            if (_cVarType == 'K' && cValAccessLX != 'W')
            {
                _Result = ParseResult.KVarCanOnlyBeWrite;
                return;
            }

            // L variables can only be FLOAT64 or VOID
            SIMCONNECT_DATATYPE[] LTypes = {
                SIMCONNECT_DATATYPE.INT32,
                SIMCONNECT_DATATYPE.FLOAT64,
                SIMCONNECT_DATATYPE.INVALID
            };
            if (_cVarType == 'L' && !Array.Exists(LTypes, x => x == _scValType))
            {
                _Result = ParseResult.LVarUnsupportedValType;
                return;
            }

            // X variables can only be INT32, FLOAT64, STRING256 or VOID
            SIMCONNECT_DATATYPE[] XTypes = {
                SIMCONNECT_DATATYPE.INT32,
                SIMCONNECT_DATATYPE.FLOAT64,
                SIMCONNECT_DATATYPE.STRING256,
                SIMCONNECT_DATATYPE.INVALID
            };
            if (_cVarType == 'X' && !Array.Exists(XTypes, x => x == _scValType))
            {
                _Result = ParseResult.XVarUnsupportedValType;
                return;
            }

            // X variables can never be both Read and Write
            if (_cVarType == 'X' && cValAccessLX == 'B')
            {
                _Result = ParseResult.XVarCanNotBeBothReadAndWrite;
                return;
            }

            _Result = ParseResult.Ok;
        }

        public void SetID(UInt16 DefineID, UInt16 RequestID)
        {
            _uUniqueID = uNewUniqueID++;
            _uDefineID = (DefineID == AUTO_ID) ? uNewDefineID++ : DefineID;
            _uRequestID = (RequestID == AUTO_ID) ? uNewRequestID++ : RequestID;
        }

        private bool GetValType(string sValType)
        {
            switch (sValType.ToUpper())
            {
                case "INT32":
                    _scValType = SIMCONNECT_DATATYPE.INT32;
                    _oValue = Activator.CreateInstance<Int32>();
                    return true;
                case "INT64":
                    _scValType = SIMCONNECT_DATATYPE.INT64;
                    _oValue = Activator.CreateInstance<Int64>();
                    return true;
                case "FLOAT32":
                    _scValType = SIMCONNECT_DATATYPE.FLOAT32;
                    _oValue = Activator.CreateInstance<float>();
                    return true;
                case "FLOAT64":
                    _scValType = SIMCONNECT_DATATYPE.FLOAT64;
                    _oValue = Activator.CreateInstance<double>();
                    return true;
                case "STRING8":
                    _scValType = SIMCONNECT_DATATYPE.STRING8;
                    _oValue = Activator.CreateInstance<String8>();
                    return true;
                case "STRING32":
                    _scValType = SIMCONNECT_DATATYPE.STRING32;
                    _oValue = Activator.CreateInstance<String32>();
                    return true;
                case "STRING64":
                    _scValType = SIMCONNECT_DATATYPE.STRING64;
                    _oValue = Activator.CreateInstance<String64>();
                    return true;
                case "STRING128":
                    _scValType = SIMCONNECT_DATATYPE.STRING128;
                    _oValue = Activator.CreateInstance<String128>();
                    return true;
                case "STRING256":
                    _scValType = SIMCONNECT_DATATYPE.STRING256;
                    _oValue = Activator.CreateInstance<String256>();
                    return true;
                case "STRING260":
                    _scValType = SIMCONNECT_DATATYPE.STRING260;
                    _oValue = Activator.CreateInstance<String260>();
                    return true;
                default:
                    _Result = ParseResult.UnsupportedValType;
                    return false;
            }
        }

        public object ConvertValue(string sValue)
        {
            switch (_scValType)
            {
                case SIMCONNECT_DATATYPE.INT32:
                    if (Int32.TryParse(sValue, out Int32 i32))
                        return i32;
                    else
                        return (Int32)0;
                case SIMCONNECT_DATATYPE.INT64:
                    if (Int64.TryParse(sValue, out Int64 i64))
                        return i64;
                    else
                        return (Int64)0;
                case SIMCONNECT_DATATYPE.FLOAT32:
                    if (Single.TryParse(sValue, out Single f32))
                        return f32;
                    else
                        return (Single)0;
                case SIMCONNECT_DATATYPE.FLOAT64:
                    if (Double.TryParse(sValue, out Double f64))
                        return f64;
                    else
                        return (Double)0;
                case SIMCONNECT_DATATYPE.STRING8:
                    String8 oString8;
                    oString8.Value = sValue.Substring(0, Math.Min(8, sValue.Length));
                    return oString8;
                case SIMCONNECT_DATATYPE.STRING32:
                    String32 oString32;
                    oString32.Value = sValue.Substring(0, Math.Min(32, sValue.Length));
                    return oString32;
                case SIMCONNECT_DATATYPE.STRING64:
                    String64 oString64;
                    oString64.Value = sValue.Substring(0, Math.Min(64, sValue.Length));
                    return oString64;
                case SIMCONNECT_DATATYPE.STRING128:
                    String128 oString128;
                    oString128.Value = sValue.Substring(0, Math.Min(128, sValue.Length));
                    return oString128;
                case SIMCONNECT_DATATYPE.STRING256:
                    String256 oString256;
                    oString256.Value = sValue.Substring(0, Math.Min(256, sValue.Length));
                    return oString256;
                case SIMCONNECT_DATATYPE.STRING260:
                    String260 oString260;
                    oString260.Value = sValue.Substring(0, Math.Min(260, sValue.Length));
                    return oString260;
                default:
                    return null;
            }
        }
    }

    public enum ParseResult
    {
        Ok,
        UnsupportedFormat,
        MissingVarName,
        MissingUnit,
        UnsupportedValType,
        KVarUnsupportedValType,
        KVarCanOnlyBeWrite,
        LVarUnsupportedValType,
        XVarUnsupportedValType,
        XVarCanNotBeBothReadAndWrite
    }
}
