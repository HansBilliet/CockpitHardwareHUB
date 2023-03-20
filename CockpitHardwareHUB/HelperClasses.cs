using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace CockpitHardwareHUB
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String8
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String32
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String64
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String128
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String256
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String260
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct String512
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string dbcc_name;
    }

    public class Constants
    {
        public const Int32 DbtDevtypDeviceinterface = 5;
        public const Int32 DeviceNotifyWindowHandle = 0;
        
        // USB Arrival/Remove
        public const Int32 DbtDevicearrival = 0x8000;
        public const Int32 DbtDeviceremovecomplete = 0x8004;
        
        // Windows Messages
        public const Int32 WmDevicechange = 0x219;
        public const Int32 WM_USER_SIMCONNECT = 0x0402;
    }

    public static class User32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, Int32 flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern Boolean UnregisterDeviceNotification(IntPtr handle);
    }

    public class RegisteredCmd
    {
        private int _iDeviceID = -1;
        private int _iMSFSID = -1;
        private string _sCmd;

        public int iDeviceID { get => _iDeviceID;  }
        public int iMSFSID { get => _iMSFSID; set => _iMSFSID = value; }
        public string sCmd { get => _sCmd; }

        public RegisteredCmd(string sCmd, int iDeviceID)
        {
            _sCmd = sCmd;
            _iDeviceID = iDeviceID;
        }

        public static string MSFS_to_Device(string sCmd)
        {
            return "";
        }
    }

    // Structure to get the result of execute_calculator_code
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Result
    {
        public double exeF;
        public Int32 exeI;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public String exeS;
    }

    public struct LogData
    {
        private DateTime _dtTimeStamp;
        private string _sLogLine;

        public string sTimeStamp { get => _dtTimeStamp.ToString("HH:mm:ss:fff"); }
        public string sLogLine { get => _sLogLine; }

        public LogData(string sLogLine)
        {
            _dtTimeStamp = DateTime.Now;
            _sLogLine = sLogLine;
        }
    }
}
