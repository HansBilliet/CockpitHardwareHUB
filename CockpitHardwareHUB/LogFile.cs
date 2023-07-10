using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CockpitHardwareHUB
{

    public class FileLogger
    {
        public string sFileName { get => _bIsOpen ? _sFileName : "No log file active"; }

        private StreamWriter LogFile;
        private string _sFileName = "";
        private bool _bIsOpen = false;

        public bool OpenFile()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\CockpitHardwareHUB");
            _sFileName = (string)key.GetValue("LogFileName");

            if (string.IsNullOrEmpty(_sFileName))
                _sFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\log.txt";

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dialog.FileName = Path.GetFileName(_sFileName);
                dialog.InitialDirectory = Path.GetDirectoryName(_sFileName);
                dialog.Title = "Select Log File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _sFileName = dialog.FileName;
                    key.SetValue("LogFileName", _sFileName);
                    LogFile = new StreamWriter(_sFileName, false);
                    _bIsOpen = true;
                    LogFile.WriteLine($"{DateTime.Now}: Logfile created");
                    LogFile.WriteLine("-------------------------------------------------");
                    return true;
                }
                else
                    _bIsOpen = false;
            }

            key.Close();

            return _bIsOpen;
        }

        public void CloseFile()
        {
            if (_bIsOpen)
            {
                LogFile.Close();
                _bIsOpen = false;
            }
        }

        public void FlushFile()
        {
            if (_bIsOpen)
            {
                LogFile.Flush();
            }
        }

        public void LogLine(string sLogLine)
        {
            if (_bIsOpen)
                LogFile.WriteLine(sLogLine);
        }
    }
}

