#if PLUGIN_TEST
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace XPatherizerNPP
{
    // Included only in the test build, and exercised inside real Notepad++.
    static class PluginSmokeTest
    {
        static Timer timer;
        static int stage;
        static int waits;
        static string originalPath;
        static string LogPath { get { return Path.Combine(Path.GetTempPath(), "XPatherizerNPP-smoketest.log"); } }
        static void Log(string text) { File.AppendAllText(LogPath, text + Environment.NewLine); }

        internal static void Start()
        {
            timer = new Timer();
            timer.Interval = 300;
            timer.Tick += Tick;
            timer.Start();
        }

        static void Tick(object sender, EventArgs args)
        {
            timer.Stop();
            try
            {
                if (stage == 0)
                {
                    Log("READY reached; header size=" + System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sci_NotifyHeader)));
                    StringBuilder path = new StringBuilder(260);
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
                    originalPath = path.ToString();
                    Main.ShowXPatherizer();
                    Main.frmXPathResults.BeginSearch(new string[] { "/catalog/book" });
                    stage = 1;
                }
                else if (stage == 1)
                {
                    if (++waits < 8) { timer.Start(); return; }
                    StringBuilder result = new StringBuilder();
                    foreach (TreeNode node in Main.frmXPathResults.resultsTree.Nodes)
                    {
                        result.AppendLine(node.Text);
                        foreach (TreeNode child in node.Nodes) result.AppendLine("  " + child.Text);
                    }
                    Log("XPATH RESULTS:\n" + result);
                    if (Main.frmXPathResults.resultsTree.Nodes.Count != 1 || Main.frmXPathResults.resultsTree.Nodes[0].Nodes.Count != 2)
                        throw new Exception("Expected exactly two book matches");
                    XPathSearchForm search = (XPathSearchForm)typeof(Main).GetField("frmXPathSearch",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
                    search.txtXPath.Text = "/catalog/book";
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
                    search.txtXPath.Text = "/different-tab";
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, originalPath);
                    if (search.txtXPath.Text != "/catalog/book") throw new Exception("Per-tab query was not restored");
                    Log("PASS: docked panels, exactly two XPath matches, new buffer and per-tab query restoration");
                    timer.Dispose();
                    return;
                }
                timer.Start();
            }
            catch (Exception ex) { Log("FAIL: " + ex); timer.Dispose(); }
        }
    }
}
#endif
