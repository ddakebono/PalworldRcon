using System;
using System.Reflection;
using Microsoft.Win32;

namespace PalworldRcon
{
    public class Settings
    {
        public string ServerAddress { get; set; }
        public string RCONPassword { get; set; }
        public ushort ServerPort { get; set; }
        public bool ShowJoinLeaves { get; set; }
        public bool DebugMode { get; set; }

        private readonly RegistryKey _rconSubkey;

        public Settings()
        {
            _rconSubkey = Registry.CurrentUser.OpenSubKey(@"Software\BTK-Development\PalworldRcon", true) ?? Registry.CurrentUser.CreateSubKey(@"Software\BTK-Development\PalworldRcon");

            LoadSettings();
        }

        public void SaveSettings()
        {
            if (_rconSubkey == null) return;

            _rconSubkey.SetValue("RCONPassword", RCONPassword);
            _rconSubkey.SetValue("ServerAddress", ServerAddress);
            _rconSubkey.SetValue("ServerPort", ServerPort);
            _rconSubkey.SetValue("ShowJoinLeaves", ShowJoinLeaves);
            _rconSubkey.SetValue("DebugMode", DebugMode);
        }

        public void LoadSettings()
        {
            if (_rconSubkey == null) return;

            RCONPassword = Convert.ToString(_rconSubkey.GetValue("RCONPassword", ""));
            ServerAddress = Convert.ToString(_rconSubkey.GetValue("ServerAddress", ""));
            ServerPort = Convert.ToUInt16(_rconSubkey.GetValue("ServerPort", 25575));
            ShowJoinLeaves = Convert.ToBoolean(_rconSubkey.GetValue("ShowJoinLeaves", true));
            DebugMode = Convert.ToBoolean(_rconSubkey.GetValue("DebugMode", false));
        }
    }
}
