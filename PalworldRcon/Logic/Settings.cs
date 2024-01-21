using System;
using System.Reflection;
using Microsoft.Win32;

namespace PalworldRcon
{
    public class Settings
    {
        public string ServerAddresss;
        public string RCONPassword;
        public ushort ServerPort;

        private RegistryKey _rconSubkey;

        public Settings()
        {
            _rconSubkey = Registry.CurrentUser.OpenSubKey(@"Software\BTK-Development\PalworldRcon", true) ?? Registry.CurrentUser.CreateSubKey(@"Software\BTK-Development\PalworldRcon");

            LoadSettings();
        }

        public void SaveSettings()
        {
            if (_rconSubkey == null) return;

            _rconSubkey.SetValue("RCONPassword", RCONPassword);
            _rconSubkey.SetValue("ServerAddress", ServerAddresss);
            _rconSubkey.SetValue("ServerPort", ServerPort);
        }

        public void LoadSettings()
        {
            if (_rconSubkey == null) return;

            RCONPassword = Convert.ToString(_rconSubkey.GetValue("RCONPassword", ""));
            ServerAddresss = Convert.ToString(_rconSubkey.GetValue("ServerAddress", ""));
            ServerPort = Convert.ToUInt16(_rconSubkey.GetValue("ServerPort", 25575));
        }
    }
}
