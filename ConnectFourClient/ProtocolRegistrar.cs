using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;

namespace ConnectFourClient
{
    public static class ProtocolRegistrar
    {
        /// <summary>
        /// Registers a custom URI scheme under HKCU\Software\Classes\{scheme}
        /// so links like connectfour://... are handled by this EXE.
        /// Safe to call on every startup.
        /// </summary>
        public static void EnsureCustomProtocol(string scheme)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(scheme)) return;
                scheme = scheme.Trim().ToLowerInvariant();

                var exe = Application.ExecutablePath;
                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) return;

                using (var root = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{scheme}"))
                {
                    if (root == null) return;
                    root.SetValue("", $"URL:{scheme} Protocol");
                    root.SetValue("URL Protocol", "");

                    using (var icon = root.CreateSubKey("DefaultIcon"))
                        icon?.SetValue("", $"\"{exe}\",1");

                    using (var cmd = root.CreateSubKey(@"shell\open\command"))
                        cmd?.SetValue("", $"\"{exe}\" \"%1\"");
                }
            }
            catch { /* never block app start */ }
        }
    }
}
