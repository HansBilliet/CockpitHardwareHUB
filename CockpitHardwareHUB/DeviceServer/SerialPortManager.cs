﻿using System;
using System.Management;

//  SerialPort manager for C# WPF using Windows Management Instrumentation (WMI)
//  This monitor will produce "Port added", "Port Removed" and "Port Found" events
//  and include the DeviceID, VendorID and ProductID in the EventArgs when an event is raised.
//
//  Make sure to install System.Management in your projects references.
//
//  Start the SerialPortManager with SerialPortManager.scanPorts()
//  Call SerialPortManager.scanPorts(false) if you don't want Added or Removed events
//  after the initial scan.
//
//  You can set the VendorID and / or ProductID to filter for matching USB Virtual com ports.
//
//  The reason for this class is to obtain an accurate report on what serial ports are
//  available. The standard method: System.IO.Ports.Serialport.getportnames() just
//  reads the Registry and suffers from caching lag.
//
//  By Paul van Dinther


namespace CockpitHardwareHUB
{
    public class SerialPortEventArgs : EventArgs
    {
        public SerialPortEventArgs(string deviceID, int vendorID, int productID, string pnpDeviceID)
        {
            DeviceID = deviceID;
            VendorID = vendorID;
            ProductID = productID;
            PNPDeviceID = pnpDeviceID;
        }
        public string DeviceID;
        public int VendorID;
        public int ProductID;
        public string PNPDeviceID;
    }

    public class SerialPortManager
    {
        public event EventHandler<SerialPortEventArgs> OnPortFoundEvent;
        public event EventHandler<SerialPortEventArgs> OnPortAddedEvent;
        public event EventHandler<SerialPortEventArgs> OnPortRemovedEvent;
        private static ManagementEventWatcher _watchingAddedObject = null;
        private static ManagementEventWatcher _watchingRemovedObject = null;
        private static WqlEventQuery _watcherQuery;
        private static ManagementScope _scope;
        private int _vendorID;
        private int _productID;
        public int VendorID { get { return _vendorID; } }
        public int ProductID { get { return _productID; } }
        public SerialPortManager(int VendorID = 0, int ProductID = 0)
        {
            _vendorID = VendorID;
            _productID = ProductID;
            _scope = new ManagementScope("root/CIMV2");
            _scope.Options.EnablePrivileges = true;
            AddInsertUSBHandler();
            AddRemoveUSBHandler();
        }

        public void scanPorts(bool watchForChanges = true)
        {
            try
            {
                bool checkID = _vendorID + _productID != 0;
                string queryString = "SELECT DeviceID, PNPDeviceID FROM Win32_SerialPort";
                if (checkID) queryString += " WHERE ";
                if (_vendorID != 0) queryString += "PNPDeviceID Like '%VID_" + _vendorID.ToString("X4") + "%'";
                if (_vendorID != 0 && _productID != 0) queryString += " AND ";
                if (_productID != 0) queryString += "PNPDeviceID Like '%PID_" + _productID.ToString("X4") + "%'";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", queryString);
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    DoPortFoundEvent(CreatePortArgs(queryObj));
                }
                if (watchForChanges)
                {
                    _watchingAddedObject.Start();
                    _watchingRemovedObject.Start();
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }
        }

        public void stop()
        {
            _watchingAddedObject.Stop();
            _watchingRemovedObject.Stop();
        }

        private SerialPortEventArgs CreatePortArgs(ManagementBaseObject queryObj)
        {
            string PNPDeviceID = ((string)queryObj.GetPropertyValue("PNPDeviceID")).ToUpper();
            int vid = 0;
            int pid = 0;
            int index = PNPDeviceID.IndexOf("VID_");
            if (index > -1 && PNPDeviceID.Length >= index + 8)
            {
                string id = PNPDeviceID.Substring(index + 4, 4);
                vid = Convert.ToInt32(id, 16);
            }
            index = PNPDeviceID.IndexOf("PID_");
            if (index > -1 && PNPDeviceID.Length >= index + 8)
            {
                string id = PNPDeviceID.Substring(index + 4, 4);
                pid = Convert.ToInt32(id, 16);
            }
            return new SerialPortEventArgs((string)queryObj["DeviceID"], vid, pid, PNPDeviceID);
        }

        private void AddInsertUSBHandler()
        {
            try
            {
                _watchingAddedObject = USBWatcherSetUp("__InstanceCreationEvent");
                _watchingAddedObject.EventArrived += new EventArrivedEventHandler(HandlePortAdded);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_watchingAddedObject != null)
                    _watchingAddedObject.Stop();
            }
        }

        private void AddRemoveUSBHandler()
        {
            try
            {
                _watchingRemovedObject = USBWatcherSetUp("__InstanceDeletionEvent");
                _watchingRemovedObject.EventArrived += new EventArrivedEventHandler(HandlePortRemoved);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_watchingRemovedObject != null)
                    _watchingRemovedObject.Stop();
            }
        }

        private ManagementEventWatcher USBWatcherSetUp(string eventType)
        {
            _watcherQuery = new WqlEventQuery();
            _watcherQuery.EventClassName = eventType;
            _watcherQuery.WithinInterval = new TimeSpan(0, 0, 2);
            _watcherQuery.Condition = @"TargetInstance ISA 'Win32_SerialPort'";
            return new ManagementEventWatcher(_scope, _watcherQuery);
        }

        private void HandlePortAdded(object sender, EventArrivedEventArgs e)
        {
            var instance = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
            SerialPortEventArgs EventArgs = CreatePortArgs(instance);

            bool checkID = _vendorID + _productID != 0;
            if (checkID)
            {
                string PNPDeviceID = (string)instance.GetPropertyValue("PNPDeviceID");
                if ((EventArgs.VendorID == 0 || PNPDeviceID.Contains("VID_" + EventArgs.VendorID.ToString("X4"))) &&
                    (EventArgs.ProductID == 0 || PNPDeviceID.Contains("VID_" + EventArgs.ProductID.ToString("X4"))))
                {
                    DoPortAddedEvent(EventArgs);
                }
            }
            else DoPortAddedEvent(EventArgs);

        }

        private void HandlePortRemoved(object sender, EventArrivedEventArgs e)
        {
            var instance = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
            DoPortRemovedEvent(CreatePortArgs(instance));
        }

        private void DoPortFoundEvent(SerialPortEventArgs EventArgs)
        {
            if (OnPortFoundEvent != null)
            {
                OnPortFoundEvent(this, EventArgs);
            }
        }

        private void DoPortAddedEvent(SerialPortEventArgs EventArgs)
        {
            if (OnPortAddedEvent != null)
            {
                OnPortAddedEvent(this, EventArgs);
            }
        }

        private void DoPortRemovedEvent(SerialPortEventArgs EventArgs)
        {
            if (OnPortRemovedEvent != null)
            {
                OnPortRemovedEvent(this, EventArgs);
            }
        }
    }
}