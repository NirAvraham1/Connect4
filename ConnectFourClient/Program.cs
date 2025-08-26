using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using ConnectFourClient.LocalReplay;

namespace ConnectFourClient
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try { ProtocolRegistrar.EnsureCustomProtocol("connectfour"); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int identifier = 0;
            bool launchReplayOnly = false;
            bool purge = false;
            bool purgeAll = false;
            int serverGameId = 0;

            if (args != null && args.Length > 0 && args[0].StartsWith("connectfour://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(args[0]);
                    var action = (uri.Host ?? "").ToLowerInvariant();
                    var qs = ParseQuery(uri.Query);

                    if (qs.ContainsKey("identifier")) int.TryParse(qs["identifier"], out identifier);
                    if (qs.ContainsKey("serverGameId")) int.TryParse(qs["serverGameId"], out serverGameId);

                    launchReplayOnly = action == "replay";
                    purge = action == "purge";
                    purgeAll = action == "purgeall";
                }
                catch { }
            }
            else if (args != null && args.Length > 0)
            {
                int.TryParse(args[0], out identifier);
            }

            if (identifier <= 0)
            {
                MessageBox.Show("Missing identifier. Please launch from the website.", "Connect Four",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ReplayDbInitializer.EnsureCreated();

            if (purgeAll)
            {
                try
                {
                    var repo = new LocalReplayRepository();
                    int n = repo.DeleteAllForIdentifier(identifier);
                    MessageBox.Show($"Removed {n} local replay session(s) for Identifier {identifier}.",
                        "Purge All", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Purge All failed: " + ex.Message, "Purge All",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if (purge)
            {
                try
                {
                    var repo = new LocalReplayRepository();
                    int n = repo.DeleteByServerGameId(identifier, serverGameId);
                    MessageBox.Show($"Removed {n} local replay(s) for game #{serverGameId}.",
                        "Purge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Purge failed: " + ex.Message, "Purge",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            Application.Run(new Form1(identifier,
                                       openLocalReplays: launchReplayOnly,
                                       startLiveGame: !launchReplayOnly));
        }


        
        private static Dictionary<string, string> ParseQuery(string query)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query)) return dict;
            var parts = query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kv in parts)
            {
                var p = kv.Split(new[] { '=' }, 2);
                var key = Uri.UnescapeDataString(p.Length > 0 ? p[0] : "");
                var val = p.Length > 1 ? Uri.UnescapeDataString(p[1]) : "";
                dict[key] = val;
            }
            return dict;    //ParseQuery("?identifier=123&serverGameId=456")
                            // => { ["identifier"]="123", ["serverGameId"]="456" }
        }
    }
}
