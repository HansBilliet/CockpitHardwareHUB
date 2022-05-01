
namespace CockpitHardwareHUB
{
    partial class CockpitHardwareForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpConnect = new System.Windows.Forms.GroupBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.grpDevices = new System.Windows.Forms.GroupBox();
            this.btnSendTest = new System.Windows.Forms.Button();
            this.txtRegisteredVariables = new System.Windows.Forms.TextBox();
            this.lblRegisteredVariables = new System.Windows.Forms.Label();
            this.btnResetStatistics = new System.Windows.Forms.Button();
            this.lblNackCntValue = new System.Windows.Forms.Label();
            this.lblCmdTxCntValue = new System.Windows.Forms.Label();
            this.lblCmdRxCntValue = new System.Windows.Forms.Label();
            this.lblNackCnt = new System.Windows.Forms.Label();
            this.lblCmdTxCnt = new System.Windows.Forms.Label();
            this.lblCmdRxCnt = new System.Windows.Forms.Label();
            this.lblDevicePathValue = new System.Windows.Forms.Label();
            this.lblProcessorTypeValue = new System.Windows.Forms.Label();
            this.lblDeviceNameValue = new System.Windows.Forms.Label();
            this.lblPath = new System.Windows.Forms.Label();
            this.lblProcessorType = new System.Windows.Forms.Label();
            this.lblDeviceName = new System.Windows.Forms.Label();
            this.cbDevices = new System.Windows.Forms.ComboBox();
            this.grpExecCalcCode = new System.Windows.Forms.GroupBox();
            this.txtExecCalcCodeStringValue = new System.Windows.Forms.TextBox();
            this.txtExecCalcCodeIntValue = new System.Windows.Forms.TextBox();
            this.txtExecCalcCodeFloatValue = new System.Windows.Forms.TextBox();
            this.lblExecCalcCodeString = new System.Windows.Forms.Label();
            this.lblExecCalcCodeInt = new System.Windows.Forms.Label();
            this.lblExecCalcCodeFloat = new System.Windows.Forms.Label();
            this.btnSendExecCalcCode = new System.Windows.Forms.Button();
            this.txtExecCalcCode = new System.Windows.Forms.TextBox();
            this.grpCommand = new System.Windows.Forms.GroupBox();
            this.btnDevice = new System.Windows.Forms.Button();
            this.btnMSFS = new System.Windows.Forms.Button();
            this.txtCommand = new System.Windows.Forms.TextBox();
            this.grpVariables = new System.Windows.Forms.GroupBox();
            this.lblVariablesFilter = new System.Windows.Forms.Label();
            this.txtVariablesFilter = new System.Windows.Forms.TextBox();
            this.dgvVariables = new System.Windows.Forms.DataGridView();
            this.grpLogging = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbLogLevel = new System.Windows.Forms.ComboBox();
            this.lblLoggingFilter = new System.Windows.Forms.Label();
            this.txtLoggingFilter = new System.Windows.Forms.TextBox();
            this.dgvLogging = new System.Windows.Forms.DataGridView();
            this.btnLoggingClear = new System.Windows.Forms.Button();
            this.cbLoggingEnabled = new System.Windows.Forms.CheckBox();
            this.grpConnect.SuspendLayout();
            this.grpDevices.SuspendLayout();
            this.grpExecCalcCode.SuspendLayout();
            this.grpCommand.SuspendLayout();
            this.grpVariables.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVariables)).BeginInit();
            this.grpLogging.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogging)).BeginInit();
            this.SuspendLayout();
            // 
            // grpConnect
            // 
            this.grpConnect.Controls.Add(this.btnConnect);
            this.grpConnect.Location = new System.Drawing.Point(13, 13);
            this.grpConnect.Name = "grpConnect";
            this.grpConnect.Size = new System.Drawing.Size(285, 49);
            this.grpConnect.TabIndex = 0;
            this.grpConnect.TabStop = false;
            this.grpConnect.Text = "MSFS2020 : DISCONNECTED";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(6, 19);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(272, 23);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // grpDevices
            // 
            this.grpDevices.Controls.Add(this.btnSendTest);
            this.grpDevices.Controls.Add(this.txtRegisteredVariables);
            this.grpDevices.Controls.Add(this.lblRegisteredVariables);
            this.grpDevices.Controls.Add(this.btnResetStatistics);
            this.grpDevices.Controls.Add(this.lblNackCntValue);
            this.grpDevices.Controls.Add(this.lblCmdTxCntValue);
            this.grpDevices.Controls.Add(this.lblCmdRxCntValue);
            this.grpDevices.Controls.Add(this.lblNackCnt);
            this.grpDevices.Controls.Add(this.lblCmdTxCnt);
            this.grpDevices.Controls.Add(this.lblCmdRxCnt);
            this.grpDevices.Controls.Add(this.lblDevicePathValue);
            this.grpDevices.Controls.Add(this.lblProcessorTypeValue);
            this.grpDevices.Controls.Add(this.lblDeviceNameValue);
            this.grpDevices.Controls.Add(this.lblPath);
            this.grpDevices.Controls.Add(this.lblProcessorType);
            this.grpDevices.Controls.Add(this.lblDeviceName);
            this.grpDevices.Controls.Add(this.cbDevices);
            this.grpDevices.Location = new System.Drawing.Point(13, 68);
            this.grpDevices.Name = "grpDevices";
            this.grpDevices.Size = new System.Drawing.Size(284, 520);
            this.grpDevices.TabIndex = 1;
            this.grpDevices.TabStop = false;
            this.grpDevices.Text = "USB Devices";
            // 
            // btnSendTest
            // 
            this.btnSendTest.Location = new System.Drawing.Point(6, 169);
            this.btnSendTest.Name = "btnSendTest";
            this.btnSendTest.Size = new System.Drawing.Size(72, 23);
            this.btnSendTest.TabIndex = 28;
            this.btnSendTest.Text = "Send test";
            this.btnSendTest.UseVisualStyleBackColor = true;
            this.btnSendTest.Click += new System.EventHandler(this.btnSendTest_Click);
            // 
            // txtRegisteredVariables
            // 
            this.txtRegisteredVariables.BackColor = System.Drawing.SystemColors.Window;
            this.txtRegisteredVariables.Location = new System.Drawing.Point(6, 220);
            this.txtRegisteredVariables.Multiline = true;
            this.txtRegisteredVariables.Name = "txtRegisteredVariables";
            this.txtRegisteredVariables.ReadOnly = true;
            this.txtRegisteredVariables.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRegisteredVariables.Size = new System.Drawing.Size(272, 294);
            this.txtRegisteredVariables.TabIndex = 26;
            this.txtRegisteredVariables.WordWrap = false;
            // 
            // lblRegisteredVariables
            // 
            this.lblRegisteredVariables.AutoSize = true;
            this.lblRegisteredVariables.Location = new System.Drawing.Point(6, 204);
            this.lblRegisteredVariables.Name = "lblRegisteredVariables";
            this.lblRegisteredVariables.Size = new System.Drawing.Size(107, 13);
            this.lblRegisteredVariables.TabIndex = 25;
            this.lblRegisteredVariables.Text = "Registered Variables:";
            // 
            // btnResetStatistics
            // 
            this.btnResetStatistics.Location = new System.Drawing.Point(223, 110);
            this.btnResetStatistics.Name = "btnResetStatistics";
            this.btnResetStatistics.Size = new System.Drawing.Size(56, 47);
            this.btnResetStatistics.TabIndex = 15;
            this.btnResetStatistics.Text = "Reset statistics";
            this.btnResetStatistics.UseVisualStyleBackColor = true;
            this.btnResetStatistics.Click += new System.EventHandler(this.btnResetStatistics_Click);
            // 
            // lblNackCntValue
            // 
            this.lblNackCntValue.AutoSize = true;
            this.lblNackCntValue.Location = new System.Drawing.Point(97, 144);
            this.lblNackCntValue.Name = "lblNackCntValue";
            this.lblNackCntValue.Size = new System.Drawing.Size(13, 13);
            this.lblNackCntValue.TabIndex = 13;
            this.lblNackCntValue.Text = "0";
            // 
            // lblCmdTxCntValue
            // 
            this.lblCmdTxCntValue.AutoSize = true;
            this.lblCmdTxCntValue.Location = new System.Drawing.Point(97, 127);
            this.lblCmdTxCntValue.Name = "lblCmdTxCntValue";
            this.lblCmdTxCntValue.Size = new System.Drawing.Size(13, 13);
            this.lblCmdTxCntValue.TabIndex = 12;
            this.lblCmdTxCntValue.Text = "0";
            // 
            // lblCmdRxCntValue
            // 
            this.lblCmdRxCntValue.AutoSize = true;
            this.lblCmdRxCntValue.Location = new System.Drawing.Point(97, 110);
            this.lblCmdRxCntValue.Name = "lblCmdRxCntValue";
            this.lblCmdRxCntValue.Size = new System.Drawing.Size(13, 13);
            this.lblCmdRxCntValue.TabIndex = 11;
            this.lblCmdRxCntValue.Text = "0";
            // 
            // lblNackCnt
            // 
            this.lblNackCnt.AutoSize = true;
            this.lblNackCnt.Location = new System.Drawing.Point(6, 144);
            this.lblNackCnt.Name = "lblNackCnt";
            this.lblNackCnt.Size = new System.Drawing.Size(52, 13);
            this.lblNackCnt.TabIndex = 9;
            this.lblNackCnt.Text = "NackCnt:";
            // 
            // lblCmdTxCnt
            // 
            this.lblCmdTxCnt.AutoSize = true;
            this.lblCmdTxCnt.Location = new System.Drawing.Point(6, 127);
            this.lblCmdTxCnt.Name = "lblCmdTxCnt";
            this.lblCmdTxCnt.Size = new System.Drawing.Size(59, 13);
            this.lblCmdTxCnt.TabIndex = 8;
            this.lblCmdTxCnt.Text = "CmdTxCnt:";
            // 
            // lblCmdRxCnt
            // 
            this.lblCmdRxCnt.AutoSize = true;
            this.lblCmdRxCnt.Location = new System.Drawing.Point(6, 110);
            this.lblCmdRxCnt.Name = "lblCmdRxCnt";
            this.lblCmdRxCnt.Size = new System.Drawing.Size(60, 13);
            this.lblCmdRxCnt.TabIndex = 7;
            this.lblCmdRxCnt.Text = "CmdRxCnt:";
            // 
            // lblDevicePathValue
            // 
            this.lblDevicePathValue.AutoSize = true;
            this.lblDevicePathValue.Location = new System.Drawing.Point(97, 85);
            this.lblDevicePathValue.Name = "lblDevicePathValue";
            this.lblDevicePathValue.Size = new System.Drawing.Size(90, 13);
            this.lblDevicePathValue.TabIndex = 6;
            this.lblDevicePathValue.Text = "DevicePathValue";
            // 
            // lblProcessorTypeValue
            // 
            this.lblProcessorTypeValue.AutoSize = true;
            this.lblProcessorTypeValue.Location = new System.Drawing.Point(97, 68);
            this.lblProcessorTypeValue.Name = "lblProcessorTypeValue";
            this.lblProcessorTypeValue.Size = new System.Drawing.Size(105, 13);
            this.lblProcessorTypeValue.TabIndex = 5;
            this.lblProcessorTypeValue.Text = "ProcessorTypeValue";
            // 
            // lblDeviceNameValue
            // 
            this.lblDeviceNameValue.AutoSize = true;
            this.lblDeviceNameValue.Location = new System.Drawing.Point(97, 51);
            this.lblDeviceNameValue.Name = "lblDeviceNameValue";
            this.lblDeviceNameValue.Size = new System.Drawing.Size(96, 13);
            this.lblDeviceNameValue.TabIndex = 4;
            this.lblDeviceNameValue.Text = "DeviceNameValue";
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.lblPath.Location = new System.Drawing.Point(6, 85);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(32, 13);
            this.lblPath.TabIndex = 3;
            this.lblPath.Text = "Path:";
            // 
            // lblProcessorType
            // 
            this.lblProcessorType.AutoSize = true;
            this.lblProcessorType.Location = new System.Drawing.Point(6, 68);
            this.lblProcessorType.Name = "lblProcessorType";
            this.lblProcessorType.Size = new System.Drawing.Size(81, 13);
            this.lblProcessorType.TabIndex = 2;
            this.lblProcessorType.Text = "ProcessorType:";
            // 
            // lblDeviceName
            // 
            this.lblDeviceName.AutoSize = true;
            this.lblDeviceName.Location = new System.Drawing.Point(6, 51);
            this.lblDeviceName.Name = "lblDeviceName";
            this.lblDeviceName.Size = new System.Drawing.Size(72, 13);
            this.lblDeviceName.TabIndex = 1;
            this.lblDeviceName.Text = "DeviceName:";
            // 
            // cbDevices
            // 
            this.cbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDevices.FormattingEnabled = true;
            this.cbDevices.Location = new System.Drawing.Point(6, 20);
            this.cbDevices.Name = "cbDevices";
            this.cbDevices.Size = new System.Drawing.Size(272, 21);
            this.cbDevices.TabIndex = 0;
            this.cbDevices.SelectionChangeCommitted += new System.EventHandler(this.cbDevices_SelectionChangeCommitted);
            // 
            // grpExecCalcCode
            // 
            this.grpExecCalcCode.Controls.Add(this.txtExecCalcCodeStringValue);
            this.grpExecCalcCode.Controls.Add(this.txtExecCalcCodeIntValue);
            this.grpExecCalcCode.Controls.Add(this.txtExecCalcCodeFloatValue);
            this.grpExecCalcCode.Controls.Add(this.lblExecCalcCodeString);
            this.grpExecCalcCode.Controls.Add(this.lblExecCalcCodeInt);
            this.grpExecCalcCode.Controls.Add(this.lblExecCalcCodeFloat);
            this.grpExecCalcCode.Controls.Add(this.btnSendExecCalcCode);
            this.grpExecCalcCode.Controls.Add(this.txtExecCalcCode);
            this.grpExecCalcCode.Location = new System.Drawing.Point(305, 13);
            this.grpExecCalcCode.Name = "grpExecCalcCode";
            this.grpExecCalcCode.Size = new System.Drawing.Size(715, 71);
            this.grpExecCalcCode.TabIndex = 2;
            this.grpExecCalcCode.TabStop = false;
            this.grpExecCalcCode.Text = "execute_calculator_code";
            // 
            // txtExecCalcCodeStringValue
            // 
            this.txtExecCalcCodeStringValue.Location = new System.Drawing.Point(292, 41);
            this.txtExecCalcCodeStringValue.Name = "txtExecCalcCodeStringValue";
            this.txtExecCalcCodeStringValue.ReadOnly = true;
            this.txtExecCalcCodeStringValue.Size = new System.Drawing.Size(417, 20);
            this.txtExecCalcCodeStringValue.TabIndex = 7;
            // 
            // txtExecCalcCodeIntValue
            // 
            this.txtExecCalcCodeIntValue.Location = new System.Drawing.Point(161, 41);
            this.txtExecCalcCodeIntValue.Name = "txtExecCalcCodeIntValue";
            this.txtExecCalcCodeIntValue.ReadOnly = true;
            this.txtExecCalcCodeIntValue.Size = new System.Drawing.Size(82, 20);
            this.txtExecCalcCodeIntValue.TabIndex = 6;
            // 
            // txtExecCalcCodeFloatValue
            // 
            this.txtExecCalcCodeFloatValue.Location = new System.Drawing.Point(45, 41);
            this.txtExecCalcCodeFloatValue.Name = "txtExecCalcCodeFloatValue";
            this.txtExecCalcCodeFloatValue.ReadOnly = true;
            this.txtExecCalcCodeFloatValue.Size = new System.Drawing.Size(82, 20);
            this.txtExecCalcCodeFloatValue.TabIndex = 5;
            // 
            // lblExecCalcCodeString
            // 
            this.lblExecCalcCodeString.AutoSize = true;
            this.lblExecCalcCodeString.Location = new System.Drawing.Point(249, 44);
            this.lblExecCalcCodeString.Name = "lblExecCalcCodeString";
            this.lblExecCalcCodeString.Size = new System.Drawing.Size(37, 13);
            this.lblExecCalcCodeString.TabIndex = 4;
            this.lblExecCalcCodeString.Text = "String:";
            // 
            // lblExecCalcCodeInt
            // 
            this.lblExecCalcCodeInt.AutoSize = true;
            this.lblExecCalcCodeInt.Location = new System.Drawing.Point(133, 44);
            this.lblExecCalcCodeInt.Name = "lblExecCalcCodeInt";
            this.lblExecCalcCodeInt.Size = new System.Drawing.Size(22, 13);
            this.lblExecCalcCodeInt.TabIndex = 3;
            this.lblExecCalcCodeInt.Text = "Int:";
            // 
            // lblExecCalcCodeFloat
            // 
            this.lblExecCalcCodeFloat.AutoSize = true;
            this.lblExecCalcCodeFloat.Location = new System.Drawing.Point(6, 44);
            this.lblExecCalcCodeFloat.Name = "lblExecCalcCodeFloat";
            this.lblExecCalcCodeFloat.Size = new System.Drawing.Size(33, 13);
            this.lblExecCalcCodeFloat.TabIndex = 2;
            this.lblExecCalcCodeFloat.Text = "Float:";
            // 
            // btnSendExecCalcCode
            // 
            this.btnSendExecCalcCode.Location = new System.Drawing.Point(667, 12);
            this.btnSendExecCalcCode.Name = "btnSendExecCalcCode";
            this.btnSendExecCalcCode.Size = new System.Drawing.Size(42, 23);
            this.btnSendExecCalcCode.TabIndex = 1;
            this.btnSendExecCalcCode.Text = "Send";
            this.btnSendExecCalcCode.UseVisualStyleBackColor = true;
            this.btnSendExecCalcCode.Click += new System.EventHandler(this.btnSendExecCalcCode_Click);
            // 
            // txtExecCalcCode
            // 
            this.txtExecCalcCode.Location = new System.Drawing.Point(6, 15);
            this.txtExecCalcCode.Name = "txtExecCalcCode";
            this.txtExecCalcCode.Size = new System.Drawing.Size(654, 20);
            this.txtExecCalcCode.TabIndex = 0;
            // 
            // grpCommand
            // 
            this.grpCommand.Controls.Add(this.btnDevice);
            this.grpCommand.Controls.Add(this.btnMSFS);
            this.grpCommand.Controls.Add(this.txtCommand);
            this.grpCommand.Location = new System.Drawing.Point(305, 84);
            this.grpCommand.Name = "grpCommand";
            this.grpCommand.Size = new System.Drawing.Size(715, 45);
            this.grpCommand.TabIndex = 3;
            this.grpCommand.TabStop = false;
            this.grpCommand.Text = "Command";
            // 
            // btnDevice
            // 
            this.btnDevice.Location = new System.Drawing.Point(579, 12);
            this.btnDevice.Name = "btnDevice";
            this.btnDevice.Size = new System.Drawing.Size(62, 23);
            this.btnDevice.TabIndex = 2;
            this.btnDevice.Text = ">Device";
            this.btnDevice.UseVisualStyleBackColor = true;
            this.btnDevice.Click += new System.EventHandler(this.btnDevice_Click);
            // 
            // btnMSFS
            // 
            this.btnMSFS.Location = new System.Drawing.Point(647, 12);
            this.btnMSFS.Name = "btnMSFS";
            this.btnMSFS.Size = new System.Drawing.Size(62, 23);
            this.btnMSFS.TabIndex = 1;
            this.btnMSFS.Text = ">MSFS";
            this.btnMSFS.UseVisualStyleBackColor = true;
            this.btnMSFS.Click += new System.EventHandler(this.btnMSFS_Click);
            // 
            // txtCommand
            // 
            this.txtCommand.Location = new System.Drawing.Point(9, 15);
            this.txtCommand.Name = "txtCommand";
            this.txtCommand.Size = new System.Drawing.Size(564, 20);
            this.txtCommand.TabIndex = 0;
            // 
            // grpVariables
            // 
            this.grpVariables.Controls.Add(this.lblVariablesFilter);
            this.grpVariables.Controls.Add(this.txtVariablesFilter);
            this.grpVariables.Controls.Add(this.dgvVariables);
            this.grpVariables.Location = new System.Drawing.Point(305, 129);
            this.grpVariables.Name = "grpVariables";
            this.grpVariables.Size = new System.Drawing.Size(715, 226);
            this.grpVariables.TabIndex = 17;
            this.grpVariables.TabStop = false;
            this.grpVariables.Text = "Variables";
            // 
            // lblVariablesFilter
            // 
            this.lblVariablesFilter.AutoSize = true;
            this.lblVariablesFilter.Location = new System.Drawing.Point(6, 18);
            this.lblVariablesFilter.Name = "lblVariablesFilter";
            this.lblVariablesFilter.Size = new System.Drawing.Size(32, 13);
            this.lblVariablesFilter.TabIndex = 16;
            this.lblVariablesFilter.Text = "Filter:";
            // 
            // txtVariablesFilter
            // 
            this.txtVariablesFilter.Location = new System.Drawing.Point(45, 15);
            this.txtVariablesFilter.Name = "txtVariablesFilter";
            this.txtVariablesFilter.Size = new System.Drawing.Size(170, 20);
            this.txtVariablesFilter.TabIndex = 15;
            this.txtVariablesFilter.TextChanged += new System.EventHandler(this.txtVariablesFilter_TextChanged);
            // 
            // dgvVariables
            // 
            this.dgvVariables.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvVariables.Location = new System.Drawing.Point(6, 41);
            this.dgvVariables.Name = "dgvVariables";
            this.dgvVariables.Size = new System.Drawing.Size(703, 179);
            this.dgvVariables.TabIndex = 14;
            // 
            // grpLogging
            // 
            this.grpLogging.Controls.Add(this.label1);
            this.grpLogging.Controls.Add(this.cbLogLevel);
            this.grpLogging.Controls.Add(this.lblLoggingFilter);
            this.grpLogging.Controls.Add(this.txtLoggingFilter);
            this.grpLogging.Controls.Add(this.dgvLogging);
            this.grpLogging.Controls.Add(this.btnLoggingClear);
            this.grpLogging.Controls.Add(this.cbLoggingEnabled);
            this.grpLogging.Location = new System.Drawing.Point(303, 355);
            this.grpLogging.Name = "grpLogging";
            this.grpLogging.Size = new System.Drawing.Size(717, 233);
            this.grpLogging.TabIndex = 18;
            this.grpLogging.TabStop = false;
            this.grpLogging.Text = "Logging";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(221, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Log level:";
            // 
            // cbLogLevel
            // 
            this.cbLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLogLevel.FormattingEnabled = true;
            this.cbLogLevel.Items.AddRange(new object[] {
            "Low",
            "Medium",
            "High"});
            this.cbLogLevel.Location = new System.Drawing.Point(280, 15);
            this.cbLogLevel.Name = "cbLogLevel";
            this.cbLogLevel.Size = new System.Drawing.Size(121, 21);
            this.cbLogLevel.TabIndex = 19;
            this.cbLogLevel.SelectionChangeCommitted += new System.EventHandler(this.cbLogLevel_SelectionChangeCommitted);
            // 
            // lblLoggingFilter
            // 
            this.lblLoggingFilter.AutoSize = true;
            this.lblLoggingFilter.Location = new System.Drawing.Point(6, 18);
            this.lblLoggingFilter.Name = "lblLoggingFilter";
            this.lblLoggingFilter.Size = new System.Drawing.Size(32, 13);
            this.lblLoggingFilter.TabIndex = 18;
            this.lblLoggingFilter.Text = "Filter:";
            // 
            // txtLoggingFilter
            // 
            this.txtLoggingFilter.Location = new System.Drawing.Point(45, 14);
            this.txtLoggingFilter.Name = "txtLoggingFilter";
            this.txtLoggingFilter.Size = new System.Drawing.Size(170, 20);
            this.txtLoggingFilter.TabIndex = 17;
            this.txtLoggingFilter.TextChanged += new System.EventHandler(this.txtLoggingFilter_TextChanged);
            // 
            // dgvLogging
            // 
            this.dgvLogging.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLogging.Location = new System.Drawing.Point(6, 41);
            this.dgvLogging.Name = "dgvLogging";
            this.dgvLogging.Size = new System.Drawing.Size(703, 185);
            this.dgvLogging.TabIndex = 0;
            // 
            // btnLoggingClear
            // 
            this.btnLoggingClear.Location = new System.Drawing.Point(565, 13);
            this.btnLoggingClear.Name = "btnLoggingClear";
            this.btnLoggingClear.Size = new System.Drawing.Size(75, 23);
            this.btnLoggingClear.TabIndex = 15;
            this.btnLoggingClear.Text = "Clear Log";
            this.btnLoggingClear.UseVisualStyleBackColor = true;
            this.btnLoggingClear.Click += new System.EventHandler(this.btnLoggingClear_Click);
            // 
            // cbLoggingEnabled
            // 
            this.cbLoggingEnabled.AutoSize = true;
            this.cbLoggingEnabled.Location = new System.Drawing.Point(646, 17);
            this.cbLoggingEnabled.Name = "cbLoggingEnabled";
            this.cbLoggingEnabled.Size = new System.Drawing.Size(65, 17);
            this.cbLoggingEnabled.TabIndex = 14;
            this.cbLoggingEnabled.Text = "Enabled";
            this.cbLoggingEnabled.UseVisualStyleBackColor = true;
            this.cbLoggingEnabled.CheckedChanged += new System.EventHandler(this.cbLoggingEnabled_CheckedChanged);
            // 
            // CockpitHardwareForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1032, 593);
            this.Controls.Add(this.grpLogging);
            this.Controls.Add(this.grpVariables);
            this.Controls.Add(this.grpCommand);
            this.Controls.Add(this.grpExecCalcCode);
            this.Controls.Add(this.grpDevices);
            this.Controls.Add(this.grpConnect);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximumSize = new System.Drawing.Size(1048, 632);
            this.MinimumSize = new System.Drawing.Size(1048, 632);
            this.Name = "CockpitHardwareForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Cockpit Hardware HUB";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CockpitHardwareForm_FormClosed);
            this.Load += new System.EventHandler(this.CockpitHardwareForm_Load);
            this.grpConnect.ResumeLayout(false);
            this.grpDevices.ResumeLayout(false);
            this.grpDevices.PerformLayout();
            this.grpExecCalcCode.ResumeLayout(false);
            this.grpExecCalcCode.PerformLayout();
            this.grpCommand.ResumeLayout(false);
            this.grpCommand.PerformLayout();
            this.grpVariables.ResumeLayout(false);
            this.grpVariables.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVariables)).EndInit();
            this.grpLogging.ResumeLayout(false);
            this.grpLogging.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogging)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpConnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.GroupBox grpDevices;
        private System.Windows.Forms.Label lblProcessorType;
        private System.Windows.Forms.Label lblDeviceName;
        private System.Windows.Forms.ComboBox cbDevices;
        private System.Windows.Forms.Button btnResetStatistics;
        private System.Windows.Forms.Label lblNackCntValue;
        private System.Windows.Forms.Label lblCmdTxCntValue;
        private System.Windows.Forms.Label lblCmdRxCntValue;
        private System.Windows.Forms.Label lblNackCnt;
        private System.Windows.Forms.Label lblCmdTxCnt;
        private System.Windows.Forms.Label lblCmdRxCnt;
        private System.Windows.Forms.Label lblDevicePathValue;
        private System.Windows.Forms.Label lblProcessorTypeValue;
        private System.Windows.Forms.Label lblDeviceNameValue;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtRegisteredVariables;
        private System.Windows.Forms.Label lblRegisteredVariables;
        private System.Windows.Forms.GroupBox grpExecCalcCode;
        private System.Windows.Forms.Label lblExecCalcCodeString;
        private System.Windows.Forms.Label lblExecCalcCodeInt;
        private System.Windows.Forms.Label lblExecCalcCodeFloat;
        private System.Windows.Forms.Button btnSendExecCalcCode;
        private System.Windows.Forms.TextBox txtExecCalcCode;
        private System.Windows.Forms.TextBox txtExecCalcCodeStringValue;
        private System.Windows.Forms.TextBox txtExecCalcCodeIntValue;
        private System.Windows.Forms.TextBox txtExecCalcCodeFloatValue;
        private System.Windows.Forms.GroupBox grpCommand;
        private System.Windows.Forms.Button btnMSFS;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.GroupBox grpVariables;
        private System.Windows.Forms.Label lblVariablesFilter;
        private System.Windows.Forms.TextBox txtVariablesFilter;
        private System.Windows.Forms.DataGridView dgvVariables;
        //private CockpitHardwareForm.CustomDataGridView dgvVariables;
        private System.Windows.Forms.GroupBox grpLogging;
        private System.Windows.Forms.CheckBox cbLoggingEnabled;
        private System.Windows.Forms.Button btnLoggingClear;
        private System.Windows.Forms.Label lblLoggingFilter;
        private System.Windows.Forms.TextBox txtLoggingFilter;
        private System.Windows.Forms.DataGridView dgvLogging;
        private System.Windows.Forms.Button btnDevice;
        private System.Windows.Forms.Button btnSendTest;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbLogLevel;
        //private CockpitHardwareForm.CustomDataGridView dgvLogging;
    }
}

