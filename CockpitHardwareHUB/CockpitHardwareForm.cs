using System;
using System.Collections.Concurrent;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CockpitHardwareHUB
{
    public partial class CockpitHardwareForm : Form
    {
        // Version
        private const string sVersion = "0.005 - 02SEP2027";

        // Log settings
        private bool _bLoggingEnabled = false;
        private bool _bUIRefreshEnabled = true;
        private int _iLogLevel = 0;
        private bool _bLogToFile = false;
        private FileLogger _fileLogger = new FileLogger();

        // Column width in DataGridViews
        private const int wTimeStamp = 80;
        private const int wLogEvent = 5000;
        private const int wID = 50;
        private const int wVariable = 100;
        private const int MaxLogLines = 500;

        // Datasources for DataGridView
        private DataTable dtLogLines = new DataTable();
        private DataTable dtVariables = new DataTable();

        private ConcurrentDictionary<string, string> _DeviceNameToPath = new ConcurrentDictionary<string, string>();

        private Timer _Timer;

        private ConcurrentQueue<LogData> _LogData = new ConcurrentQueue<LogData>();

        private COMDevice _SelectedDevice = null;
        private object debug;

        public CockpitHardwareForm()
        {
            InitializeComponent();
        }

        // As soon as Window Handle is available, pass it to DeviceServer and SimConnectClient
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            DeviceServer.RegisterForUsbEvents(Handle);
            SimConnectClient.SetHandle(Handle);
        }

        // Initialization
        private void CockpitHardwareForm_Load(object sender, EventArgs e)
        {
            UpdateUI_Connection(false);
            UpdateUI_Devices();

            InitializeDataGridViews();

            Text += sVersion;

            cbLoggingEnabled.Checked = _bLoggingEnabled;
            cbUIRefreshEnabled.Checked = _bUIRefreshEnabled;
            cbLogLevel.SelectedIndex = _iLogLevel; // Log level: Low
            cbLogToFile.Checked = _bLogToFile;
            txtLogFile.Text = _fileLogger.sFileName;

            // Initialize SimConnectClient and DeviceServer
            SimConnectClient.Init(OnLogger, DeviceServer.OnSendToDevice, OnConnectStatus, OnVariableUpdate, OnExeResult);
            DeviceServer.Init(OnLogger, SimConnectClient.OnSendToMSFS, OnDeviceAddRemove);

            // Initialize timer
            _Timer = new Timer();
            _Timer.Interval = 10;
            _Timer.Tick += new EventHandler(timer_Tick);
            _Timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (_bUIRefreshEnabled == false)
                return;

            UpdateLogger();

            if (_SelectedDevice == null)
            {
                if (_DeviceNameToPath.TryGetValue(cbDevices.Text, out string key))
                    _SelectedDevice = DeviceServer.GetDevice(key);
            }

            if (_SelectedDevice != null)
                UpdateUIStatistics();
        }

        private void UpdateLogger()
        {
            try
            {
                var count = _LogData.Count;
                if (count == 0)
                    return;

                count = Math.Min(50, count); // update 10 loglines in one go

                for (int i = 0; i < count; i++)
                {
                    _LogData.TryDequeue(out LogData LogData);
                    dtLogLines.Rows.Add(new string[] { LogData.sTimeStamp, LogData.sLogLine });
                    _fileLogger.LogLine($"{LogData.sTimeStamp} - {LogData.sLogLine}");
                }
                _fileLogger.FlushFile();

                if (dtLogLines.Rows.Count > MaxLogLines)
                    dtLogLines.Rows.RemoveAt(0);

                // if logging is enabled, move cursor at the end
                if (cbLoggingEnabled.Checked && dgvLogging.RowCount != 0)
                {
                    dgvLogging.ClearSelection();
                    dgvLogging.CurrentCell = null;
                    dgvLogging.FirstDisplayedScrollingRowIndex = dgvLogging.RowCount - 1;
                }
            }
            catch (Exception ex)
            {
                OnLogger($"CockpitHardwareForm.timer_Tick() Exception: {ex}", 2);
            }
        }

        private void UpdateUIStatistics()
        {
            if (_SelectedDevice == null)
                return;

            string s = $"{_SelectedDevice.DeviceName}";

            if (cbDevices.Text == s)
            {
                lock (_SelectedDevice.stats)
                {
                    lblCmdRxCntValue.Text = _SelectedDevice.stats.cmdRxCnt.ToString();
                    lblCmdTxCntValue.Text = _SelectedDevice.stats.cmdTxCnt.ToString();
                    lblNackCntValue.Text = _SelectedDevice.stats.nackCnt.ToString();
                }
            }
        }

        private void InitializeDataGridViewAppearance(DataGridView dgv)
        {
            dgv.RowHeadersVisible = false;
            dgv.Columns.Cast<DataGridViewColumn>().ToList().ForEach(f => f.SortMode = DataGridViewColumnSortMode.NotSortable);
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = false;
            // Disable standard Windows colors
            dgv.EnableHeadersVisualStyles = false;
            // set color of column headers
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            // avoid highlighting selection in grid
            dgv.DefaultCellStyle.SelectionBackColor = dgv.DefaultCellStyle.BackColor;
            dgv.DefaultCellStyle.SelectionForeColor = dgv.DefaultCellStyle.ForeColor;
        }

        private void InitializeDataGridViews()
        {
            // DataGridView Variables

            dtVariables.Columns.Add("ID", typeof(string));
            dtVariables.Columns.Add("Variable", typeof(string));
            dtVariables.Columns.Add("Value", typeof(string));

            dgvVariables.DataSource = dtVariables;

            dgvVariables.Columns["ID"].Width = wID;
            dgvVariables.Columns["Value"].Width = wVariable;
            dgvVariables.Columns["Variable"].Width = dgvVariables.Width - wID - wVariable - SystemInformation.VerticalScrollBarWidth - 3;

            InitializeDataGridViewAppearance(dgvVariables);

            // DataGridView Logging

            dtLogLines.Columns.Add("TimeStamp", typeof(string));
            dtLogLines.Columns.Add("Log event", typeof(string));
            //dtLogLinesBuffer = dtLogLines.Clone();

            dgvLogging.DataSource = dtLogLines;

            dgvLogging.Columns["TimeStamp"].Width = wTimeStamp;
            dgvLogging.Columns["Log event"].Width = wLogEvent;
            //dgvLogging.Columns["Log event"].Width = dgvLogging.Width - wTimeStamp - SystemInformation.VerticalScrollBarWidth - 3;

            InitializeDataGridViewAppearance(dgvLogging);
        }

        private void CockpitHardwareForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            DeviceServer.UnRegisterForUsbEvents();
            _Timer.Stop();
            _fileLogger.CloseFile();
        }

        // Override WndProc to install hooks for DeviceServer and SimConnectClient
        protected override void WndProc(ref Message m)
        {
            DeviceServer.HandleWndProc(ref m);
            SimConnectClient.HandleWndProc(ref m);

            base.WndProc(ref m);
        }

        // Add line to LogData
        private void OnLogger(string sLogger, int LogLevel)
        {
            if (_bLoggingEnabled && (LogLevel <= _iLogLevel))
                _LogData.Enqueue(new LogData(sLogger));
        }

        // Multithread entry for DeviceNotification
        private void OnDeviceAddRemove(char cAddRemove, COMDevice Device)
        {
            try
            {
                Invoke((MethodInvoker)(() => ProcessDeviceAddRemove(cAddRemove, Device)));
            }
            catch (Exception)
            {
                OnLogger("CockpitHardwareForm.OnDeviceNotication(): Exception", 2);
            }
        }

        // Multithread entry for VariableUpdate
        private void OnVariableUpdate(char cAction, string sVarID, string sVarData)
        {
            try
            {
                Invoke((MethodInvoker)(() => ProcessVariableUpdate(cAction, sVarID, sVarData)));
            }
            catch (Exception)
            {
                OnLogger("CockpitHardwareForm.OnVariableUpdate(): Exception", 2);
            }
        }

        // Multithread entry for ExeResult
        private void OnExeResult(Result ExeResult)
        {
            try
            {
                Invoke((MethodInvoker)(() =>
                {
                    txtExecCalcCodeFloatValue.Text = ExeResult.exeF.ToString("0.000");
                    txtExecCalcCodeIntValue.Text = ExeResult.exeI.ToString();
                    txtExecCalcCodeStringValue.Text = ExeResult.exeS;
                }));
            }
            catch (Exception)
            {
                OnLogger("CockpitHardwareForm.OnVariableUpdate(): Exception", 2);
            }
        }

        // Multithread entry for ConnectStatus
        private void OnConnectStatus(bool bConnected)
        {
            try
            {
                Invoke((MethodInvoker)(() => UpdateUI_Connection(bConnected)));
            }
            catch (Exception)
            {
                OnLogger("CockpitHardwareForm.OnMSFSConnectStatus(): Exception", 2);
            }
        }

        private void ProcessDeviceAddRemove(char cAddRemove, COMDevice Device)
        {
            switch (cAddRemove)
            {
                case '+':
                    string s = $"{Device.DeviceName}";
                    _DeviceNameToPath.TryAdd(Device.DeviceName, Device.Key);
                    cbDevices.Items.Add(s);
                    cbDevices.SelectedItem = s;
                    UpdateUI_Devices();
                    break;

                case '-':
                    _DeviceNameToPath.TryRemove(Device.DeviceName, out _);
                    cbDevices.Items.Remove($"{Device.DeviceName}");
                    UpdateUI_Devices();
                    break;
            }

            OnLogger($"{Device.DeviceName}: {(cAddRemove == '+' ? "Added" : "Removed")}", 3);
        }

        private void ProcessVariableUpdate(char cAction, string sVarID, string sVarData)
        {
            DataRow dr = dtVariables.AsEnumerable()
                .SingleOrDefault(r => r.Field<string>("ID").ToString() == sVarID);

            switch (cAction)
            {
                case '#':
                    if (dr == null)
                    {
                        dtVariables.Rows.Add(new string[] {
                            sVarID,
                            sVarData,
                            "N/A"
                            });
                    }
                    break;

                case '=':
                    if (dr != null)
                    {
                        dr["Value"] = sVarData;
                    }
                    break;

                case '-':
                    if (dr != null)
                        dtVariables.Rows.Remove(dr);
                    break;
            }
        }

        private void UpdateUI_Connection(bool bConnected)
        {
            btnConnect.Text = (bConnected) ? "Disconnect" : "Connect";
            grpConnect.Text = $"MSFS2020 : {(bConnected ? "CONNECTED" : "DISCONNECTED")}";
        }

        private void UpdateUI_Devices()
        {
            if (_DeviceNameToPath.TryGetValue(cbDevices.Text, out string key))
                _SelectedDevice = DeviceServer.GetDevice(key);
            else
                _SelectedDevice = null;


            if (_SelectedDevice == null)
            {
                lblDeviceNameValue.Text = "";
                lblProcessorTypeValue.Text = "";
                lblDevicePathValue.Text = "";
                txtRegisteredVariables.Text = "";

                ResetUIStatistics();
            }
            else
            {
                lblDeviceNameValue.Text = _SelectedDevice.DeviceName;
                lblProcessorTypeValue.Text = _SelectedDevice.ProcessorType;
                string[] path = _SelectedDevice.Path.Split('#');
                lblDevicePathValue.Text = path[1];

                txtRegisteredVariables.Text = "";
                foreach (RegisteredCmd command in _SelectedDevice._RegisteredCmds)
                {
                    if (txtRegisteredVariables.Text != "")
                        txtRegisteredVariables.AppendText(Environment.NewLine);
                    txtRegisteredVariables.AppendText(command.sCmd);
                }
                txtRegisteredVariables.Select(0, 0);
                txtRegisteredVariables.ScrollToCaret();
            }

            //this.Update();
        }

        private void ResetUIStatistics()
        {
            lblCmdRxCntValue.Text = "0";
            lblCmdTxCntValue.Text = "0";
            lblNackCntValue.Text = "0";
        }

        // Interaction with UI Controls

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!SimConnectClient.IsConnected())
            {
                SimConnectClient.Connect();
            }
            else
            {
                SimConnectClient.Disconnect();
            }
            UpdateUI_Connection(SimConnectClient.IsConnected());
        }

        private void cbDevices_SelectionChangeCommitted(object sender, EventArgs e)
        {
            UpdateUI_Devices();
        }

        private void btnLoggingClear_Click(object sender, EventArgs e)
        {
            dtLogLines.Clear();
        }

        private void btnResetStatistics_Click(object sender, EventArgs e)
        {
            DeviceServer.ResetStatistics();

            ResetUIStatistics();
        }

        private void btnTest1_Click(object sender, EventArgs e)
        {
            Task.Run(() => DeviceServer.OnSendToDevice("TEST=1"));
        }

        private void btnSendTest_Click(object sender, EventArgs e)
        {
            Task.Run(() => DeviceServer.SendTest(_SelectedDevice));
        }

        private void txtVariablesFilter_TextChanged(object sender, EventArgs e)
        {
            dtVariables.DefaultView.RowFilter = $"Variable LIKE '%{txtVariablesFilter.Text}%'";
        }

        private void txtLoggingFilter_TextChanged(object sender, EventArgs e)
        {
            dtLogLines.DefaultView.RowFilter = $"[Log event] LIKE '%{txtLoggingFilter.Text}%'";
        }

        private void cbLogLevel_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _iLogLevel = cbLogLevel.SelectedIndex;
        }

        private void cbLoggingEnabled_CheckedChanged(object sender, EventArgs e)
        {
            _bLoggingEnabled = cbLoggingEnabled.Checked;
        }

        private void cbUIRefreshEnabled_CheckedChanged(object sender, EventArgs e)
        {
            _bUIRefreshEnabled = cbUIRefreshEnabled.Checked;
        }

        private void btnSendExecCalcCode_Click(object sender, EventArgs e)
        {
            if (txtExecCalcCode.Text != "")
                SimConnectClient.ExecuteCalculatorCode(txtExecCalcCode.Text);
        }

        private void btnDevice_Click(object sender, EventArgs e)
        {
            if (txtCommand.Text != "")
                DeviceServer.OnSendToDevice(txtCommand.Text);
        }

        private void btnMSFS_Click(object sender, EventArgs e)
        {
            if (txtCommand.Text != "")
                // for manual entries, we just assume that it is a registered command
                SimConnectClient.OnSendToMSFS(txtCommand.Text, true);
        }

        private void cbLogToFile_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLogToFile.Checked)
            {
                if (!_fileLogger.OpenFile())
                    cbLogToFile.Checked = false;
            }
            else
                _fileLogger.CloseFile();

            txtLogFile.Text = _fileLogger.sFileName;

            _bLogToFile = cbLogToFile.Checked;
        }
    }
}
