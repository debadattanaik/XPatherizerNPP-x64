using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace XPatherizerNPP
{
    public partial class XPathSearchForm : Form
    {
        public bool HasBeenShown;
        readonly Timer autoSearchTimer = new Timer();

        public XPathSearchForm()
        {
            InitializeComponent();
            autoSearchTimer.Interval = 350;
            autoSearchTimer.Tick += delegate { autoSearchTimer.Stop(); if (Main.settings.AutoSearch) BeginSearch(); };
            Disposed += delegate { autoSearchTimer.Dispose(); };
            HasBeenShown = false;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            BeginSearch();
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSearch_Click(sender, e);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtXPath.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtXPath.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtXPath.Paste();
        }

        private void txtXPath_TextChanged(object sender, EventArgs e)
        {
            autoSearchTimer.Stop();
            if (Main.settings.AutoSearch)
            {
                autoSearchTimer.Start();
            }
        }

        public void BeginSearch()
        {
            autoSearchTimer.Stop();
            //Only search if the forms have been shown at least once.
            if (HasBeenShown && Main.frmXPathResults.HasBeenShown)
            {
                string[] xpathStrings;

                if (txtXPath.SelectedText.Length > 0)
                {
                    xpathStrings = txtXPath.SelectedText.Split('\n');
                }
                else
                    xpathStrings = txtXPath.Lines;

                Main.frmXPathResults.BeginSearch(xpathStrings);
            }
        }
    }
}
