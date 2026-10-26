using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using NppPluginNET;

namespace XPatherizerNPP
{
    public partial class XPathResultsForm : Form
    {
        public bool HasBeenShown;
        public ArrayList SelectedNodes;
        public TreeNode LastSelectedNode;
        public bool VerifyResults;
        private bool bitRestart;
        private string[] pendingQueries;

        public XPathResultsForm()
        {
            InitializeComponent();
            resultsTree.BeforeExpand += resultsTree_BeforeExpand;
            HasBeenShown = false;
            SelectedNodes = new ArrayList();
            VerifyResults = false;
            HideStopButton();
        }

        public void ShowStopButton()
        {
            panel1.Visible = true;
        }

        public void HideStopButton()
        {
            panel1.Visible = false;
        }

        private void btnShowAll_Click(object sender, EventArgs e)
        {
            resultsTree.BeginUpdate();
            try
            {
                Stack<TreeNode> pending = new Stack<TreeNode>();
                foreach (TreeNode root in resultsTree.Nodes) pending.Push(root);
                while (pending.Count > 0)
                {
                    TreeNode node = pending.Pop();
                    LazyResultNode lazy = node as LazyResultNode;
                    if (lazy != null) lazy.LoadChildren();
                    foreach (TreeNode child in node.Nodes) pending.Push(child);
                }
                resultsTree.ExpandAll();
            }
            finally { resultsTree.EndUpdate(); }
        }

        private void resultsTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            LazyResultNode node = e.Node as LazyResultNode;
            if (node == null) return;
            resultsTree.BeginUpdate();
            try { node.LoadChildren(); }
            finally { resultsTree.EndUpdate(); }
        }

        // Keep Tag as the XPath string: navigation, export, and removal use it.
        internal sealed class LazyResultNode : TreeNode
        {
            readonly XmlNode source;
            readonly bool defaultRemoved;
            readonly Dictionary<XmlNode, int> siblingIndices;
            bool loaded;

            internal LazyResultNode(XmlNode source, bool defaultRemoved, Dictionary<XmlNode, int> siblingIndices)
                : base(Main.NodetoText(source))
            {
                this.source = source;
                this.defaultRemoved = defaultRemoved;
                this.siblingIndices = siblingIndices;
                Tag = Main.FindXPath(source, defaultRemoved, siblingIndices);
                if (source.NodeType != XmlNodeType.Attribute &&
                    (source.HasChildNodes || (source.Attributes != null && source.Attributes.Count > 0)))
                    Nodes.Add(new TreeNode("Loading..."));
            }

            internal void LoadChildren()
            {
                if (loaded) return;
                List<TreeNode> children = new List<TreeNode>();
                if (source.Attributes != null)
                    foreach (XmlNode attribute in source.Attributes)
                        children.Add(new LazyResultNode(attribute, defaultRemoved, siblingIndices));
                if (source.NodeType != XmlNodeType.Attribute)
                    foreach (XmlNode child in source.ChildNodes)
                        children.Add(new LazyResultNode(child, defaultRemoved, siblingIndices));
                Nodes.Clear();
                Nodes.AddRange(children.ToArray());
                loaded = true;
            }
        }

        private void btnHideAll_Click(object sender, EventArgs e)
        {
            resultsTree.CollapseAll();
        }

        private void resultsTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (ModifierKeys == Keys.Control && !VerifyResults)
            {
                if (SelectedNodes.Contains(e.Node))
                {
                    SelectedNodes.Remove(e.Node);
                    e.Node.BackColor = resultsTree.BackColor;
                    e.Node.ForeColor = resultsTree.ForeColor;
                    e.Cancel = true;
                }
            }
        }

        private void resultsTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (ModifierKeys == Keys.Control && !VerifyResults)
            {
                SelectedNodes.Add(e.Node);
                e.Node.BackColor = SystemColors.Highlight;
                e.Node.ForeColor = SystemColors.HighlightText;
            }
            if (ModifierKeys != Keys.Control && !VerifyResults)
            {
                foreach (TreeNode Node in SelectedNodes)
                {
                    Node.BackColor = resultsTree.BackColor;
                    Node.ForeColor = resultsTree.ForeColor;
                }
                SelectedNodes.Clear();
                SelectedNodes.Add(e.Node);
                e.Node.BackColor = SystemColors.Highlight;
                e.Node.ForeColor = SystemColors.HighlightText;
            }
            if (e.Node.Tag != null && e.Node.Tag.ToString() != "" && !(e.Node.Tag.ToString().StartsWith("X:")))
            {
                Main.Highlight(e.Node.Tag.ToString());
            }
        }

        private void exportResultsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!VerifyResults)
            {
                ArrayList XPaths = new ArrayList();
                foreach (TreeNode Node in SelectedNodes)
                {
                    if (Node.Tag != null && Node.Tag.ToString() != "")
                        XPaths.Add(Node.Tag.ToString());
                }
                if (XPaths.Count > 0)
                    Main.ExportXML(XPaths);
            }
        }

        private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!VerifyResults)
            {
                ArrayList XPaths = new ArrayList();
                foreach (TreeNode Node in SelectedNodes)
                {
                    if (Node.Tag != null && Node.Tag.ToString() != "")
                        XPaths.Add(Node.Tag.ToString());
                }
                if (XPaths.Count > 0)
                    Main.RemoveNodes(XPaths);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            worker.CancelAsync();
        }

        public delegate void workerProgress(ProgressReporter progress);

        public void ReportProgress(ProgressReporter progress)
        {
            if (progress.strNodeText != null)
            {
                if (progress.strParnetNodeText != null)
                    resultsTree.Nodes.Find(progress.strParnetNodeText, false)[0].Nodes.Add(progress.strNodeText);
                else
                {
                    progress.treeNode = new TreeNode(progress.strNodeText);
                    progress.treeNode.Name = progress.strNodeText;
                    resultsTree.Nodes.Add(progress.treeNode);
                }
            }
            else
            {
                if (progress.strParnetNodeText != null)
                    resultsTree.Nodes.Find(progress.strParnetNodeText, false)[0].Nodes.Add(progress.treeNode);
                else
                {
                    progress.treeNode.Name = progress.treeNode.Text;
                    resultsTree.Nodes.Add(progress.treeNode);
                }
            }
        }

        public void BeginSearch(string[] xpathStrings)
        {
            if (worker.IsBusy)
            {
                pendingQueries = (string[])xpathStrings.Clone();
                worker.CancelAsync();
                bitRestart = true;
            }
            else
            {
                resultsTree.Nodes.Clear();
                SelectedNodes.Clear();
                LastSelectedNode = null;
                VerifyResults = false;
                ShowStopButton();
                worker.RunWorkerAsync(xpathStrings);
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // BackgroundWorker already dispatches ProgressChanged on the UI thread.
            resultsTree.BeginUpdate();
            try { ReportProgress((ProgressReporter)e.UserState); }
            finally { resultsTree.EndUpdate(); }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (resultsTree.Nodes.Count > 0 && resultsTree.Nodes[0].Text == "Processing...")
                resultsTree.Nodes.RemoveAt(0);
            if (e.Error != null) resultsTree.Nodes.Add("Search failed: " + e.Error.Message);
            HideStopButton();
            if (bitRestart)
            {
                bitRestart = false;
                BeginSearch(pendingQueries);
                pendingQueries = null;
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            DoSearch((string[])e.Argument);
        }

        /// <summary>
        /// Perform an XPath search.
        /// </summary>
        /// <param name="xpathStrings">List of strings to search against.</param>
        public void DoSearch(string[] xpathStrings)
        {
            try
            {
                worker.ReportProgress(0, new ProgressReporter("Processing..."));
                XmlDocument XMLdoc = new XmlDocument();
                try
                {
                    IntPtr curScintilla = PluginBase.GetCurrentScintilla();
                    int length = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETLENGTH, 0, 0) + 1;
                    StringBuilder sb = new StringBuilder(length);
                    Win32.SendMessage(curScintilla, SciMsg.SCI_GETTEXT, length, sb);
                    string doc = sb.ToString();

                    if (Main.settings.IgnoreDocType)
                        XMLdoc.XmlResolver = null;
                    XMLdoc.LoadXml(doc.Replace("&", "&amp;"));
                }
                catch (XmlException xex)
                {
                    worker.ReportProgress(0, new ProgressReporter("Document ERROR"));
                    worker.ReportProgress(0, new ProgressReporter(xex.Message, "Document ERROR"));
                    try
                    {
                        IntPtr curScintilla = PluginBase.GetCurrentScintilla();
                        int startPos = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_POSITIONFROMLINE, xex.LineNumber - 1, 0) + xex.LinePosition - 1;
                        Win32.SendMessage(curScintilla, SciMsg.SCI_GOTOPOS, startPos, 0);
                    }
                    catch { }
                    return;
                }

                bool bitDefaultRemoved = false;

                if (Main.settings.defaultNamespace == "")
                {
                    XMLdoc = Main.RemoveDefaultNamespaces(XMLdoc, out bitDefaultRemoved);
                }

                XPathNavigator navigator = XMLdoc.CreateNavigator();
                XmlNamespaceManager xnm = new XmlNamespaceManager(XMLdoc.NameTable);
                Main.DNSCount = 1;
                xnm = Main.GetNameSpaces(XMLdoc.SelectSingleNode("/*"), xnm);
                XPathExpression xpe;
                object result;
                string strres = "";
                System.Xml.XPath.XPathResultType xprt = System.Xml.XPath.XPathResultType.Any;

                int i = 1;
                Dictionary<XmlNode, int> siblingIndices = new Dictionary<XmlNode, int>();

                foreach (string s in xpathStrings)
                {
                    if (worker.CancellationPending) return;
                    string xPath = s;
                    //xPath Comments support.
                    //I know xPath 1.0 does not support these, but it is nice to be able to store comments at least in NPP.
                    while (xPath.Contains("(:") && xPath.Contains(":)"))
                    {
                        int intStart = xPath.IndexOf("(:");
                        int intEnd = xPath.IndexOf(":)", intStart + 2);

                        if (intEnd <= intStart)
                            intEnd = xPath.Length - 2;

                        xPath = xPath.Remove(intStart, intEnd - intStart + 2);
                    }
                    if (xPath != "")
                    {
                        try
                        {
                            xpe = XPathExpression.Compile(xPath, xnm);
                            result = navigator.Evaluate(xpe);
                            xprt = result is XPathNodeIterator ? XPathResultType.NodeSet : xpe.ReturnType;
                            strres = result.ToString();
                        }
                        catch (System.Xml.XPath.XPathException xpx)
                        {
                            worker.ReportProgress(0, new ProgressReporter(i + ": ERROR"));
                            worker.ReportProgress(0, new ProgressReporter(xpx.Message.Replace("'" + xPath + "'", "The xPath statement"), i + ": ERROR"));
                            i++;
                            continue;
                        }
                        if (xprt == System.Xml.XPath.XPathResultType.NodeSet)
                        {
                            XPathNodeIterator xpni = (XPathNodeIterator)result;
                            List<TreeNode> matches = new List<TreeNode>();
                            while (xpni.MoveNext())
                            {
                                if (worker.CancellationPending) return;
                                if (xpni.Current is IHasXmlNode)
                                {
                                    XmlNode Node = ((IHasXmlNode)xpni.Current).GetNode();
                                    matches.Add(new LazyResultNode(Node, bitDefaultRemoved, siblingIndices));
                                }
                            }
                            TreeNode group = new TreeNode(i + ": " + matches.Count + " Hit" + (matches.Count == 1 ? "" : "s"), matches.ToArray());
                            group.Tag = "X:" + xPath;
                            // Publish once per query, with descendants deferred until expanded.
                            worker.ReportProgress(0, new ProgressReporter(group));
                        }
                        else
                        {
                            worker.ReportProgress(0, new ProgressReporter(i + ": " + xprt + ": " + strres));
                        }
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                worker.ReportProgress(0, new ProgressReporter("An unexpected error has occured: " + ex.Message));
            }
        }

        /// <summary>
        /// Gets data for all direct children of Node.
        /// </summary>
        /// <param name="Node">The Parent Node</param>
        /// <returns>A collection of child nodes.</returns>
        internal TreeNode[] GetChildren(XmlNode Node, bool bitDefaultRemoved)
        {
            List<TreeNode> treeNodes = new List<TreeNode>();
            Dictionary<XmlNode, int> indices = new Dictionary<XmlNode, int>();
            if (Node.Attributes != null)
            {
                foreach (XmlNode att in Node.Attributes)
                {
                    if (worker.CancellationPending) break;
                    treeNodes.Add(new LazyResultNode(att, bitDefaultRemoved, indices));
                }
            }
            foreach (XmlNode cNode in Node.ChildNodes)
            {
                if (worker.CancellationPending) break;
                treeNodes.Add(new LazyResultNode(cNode, bitDefaultRemoved, indices));
            }
            return treeNodes.ToArray();
        }

        private void resultsTree_MouseUp(object sender, MouseEventArgs e)
        {
            TreeNode tNode = resultsTree.GetNodeAt(e.Location);
            if (tNode != null && resultsTree.SelectedNode == tNode)
            {
                if (tNode.Tag != null && tNode.Tag.ToString() != "" && !(tNode.Tag.ToString().StartsWith("X:")))
                {
                    Main.Highlight(tNode.Tag.ToString());
                } 
            }
        }
    }

    public class ProgressReporter
    {
        public string strParnetNodeText;
        public string strNodeText;
        public TreeNode treeNode;

        public ProgressReporter(string strText)
        {
            strNodeText = strText;
        }

        public ProgressReporter(string strText, string strParentText)
        {
            strNodeText = strText;
            strParnetNodeText = strParentText;
        }

        public ProgressReporter(TreeNode tNode)
        {
            treeNode = tNode;
        }

        public ProgressReporter(TreeNode tNode, string strParentText)
        {
            treeNode = tNode;
            strParnetNodeText = strParentText;
        }
    }
}
