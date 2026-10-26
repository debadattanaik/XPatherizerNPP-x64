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
        static System.Diagnostics.Stopwatch searchWatch;
        static string LogPath { get { return Path.Combine(Path.GetTempPath(), "XPatherizerNPP-smoketest.log"); } }
        static void Log(string text) { File.AppendAllText(LogPath, text + Environment.NewLine); }

        internal static void Start()
        {
            Log("TEST RUN " + DateTime.Now.ToString("o"));
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
                    searchWatch = System.Diagnostics.Stopwatch.StartNew();
                    Main.frmXPathResults.BeginSearch(new string[] { "/catalog/book" });
                    stage = 1;
                }
                else if (stage == 1)
                {
                    if (Main.frmXPathResults.worker.IsBusy || ++waits < 2) { timer.Start(); return; }
                    searchWatch.Stop();
                    Log("SEARCH completed and UI updated within " + searchWatch.ElapsedMilliseconds + " ms (timer observation)");
                    StringBuilder result = new StringBuilder();
                    foreach (TreeNode node in Main.frmXPathResults.resultsTree.Nodes)
                    {
                        result.AppendLine(node.Text);
                        foreach (TreeNode child in node.Nodes) result.AppendLine("  " + child.Text);
                    }
                    Log("XPATH RESULTS:\n" + result);
                    if (Main.frmXPathResults.resultsTree.Nodes.Count != 1 || Main.frmXPathResults.resultsTree.Nodes[0].Nodes.Count != 2)
                        throw new Exception("Expected exactly two book matches");
                    XPathResultsForm.LazyResultNode book = (XPathResultsForm.LazyResultNode)Main.frmXPathResults.resultsTree.Nodes[0].Nodes[0];
                    book.Expand();
                    if (book.Nodes.Count != 2) throw new Exception("Expected book id attribute and title child after expansion");
                    XPathResultsForm.LazyResultNode title = (XPathResultsForm.LazyResultNode)book.Nodes[1];
                    title.Expand();
                    Main.Highlight((string)book.Tag);
                    Log("PASS: lazy book/title expansion and result-to-editor navigation");
                    XPathSearchForm search = (XPathSearchForm)typeof(Main).GetField("frmXPathSearch",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
                    search.txtXPath.Text = "/catalog/book";
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
                    search.txtXPath.Text = "/different-tab";
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, originalPath);
                    if (search.txtXPath.Text != "/catalog/book") throw new Exception("Per-tab query was not restored");
                    Log("PASS: docked panels, exactly two XPath matches, new buffer and per-tab query restoration");
                    Main.frmXPathResults.BeginSearch(new string[] { "count(/catalog/book)", "/catalog/missing", "//*[" });
                    stage = 2;
                    waits = 0;
                }
                else if (stage == 2)
                {
                    if (Main.frmXPathResults.worker.IsBusy || ++waits < 2) { timer.Start(); return; }
                    TreeNodeCollection nodes = Main.frmXPathResults.resultsTree.Nodes;
                    if (nodes.Count != 3 || !nodes[0].Text.EndsWith(": 2") || nodes[1].Text != "2: 0 Hits" || nodes[2].Text != "3: ERROR")
                        throw new Exception("Scalar, zero-match, or invalid-query regression");
                    Main.frmXPathResults.BeginSearch(new string[] { "/catalog/book" });
                    Main.frmXPathResults.BeginSearch(new string[] { "/catalog/book/title" });
                    stage = 3;
                    waits = 0;
                }
                else if (stage == 3)
                {
                    if (Main.frmXPathResults.worker.IsBusy || ++waits < 2) { timer.Start(); return; }
                    if ((string)Main.frmXPathResults.resultsTree.Nodes[0].Tag != "X:/catalog/book/title")
                        throw new Exception("Restart did not run the most recent query");
                    Log("PASS: scalar query, zero matches, invalid XPath, cancellation/restart and no completion crash");
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
