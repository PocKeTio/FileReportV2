using System;
using System.Windows.Forms;
using System.Diagnostics;
using FileReport47.Models;

namespace FileReport47
{
    public partial class ResultsForm : Form
    {
        public ResultsForm()
        {
            InitializeComponent();
        }

        public void AddResult(FileInformation fileInfo)
        {
            if (lstResults.InvokeRequired)
            {
                lstResults.BeginInvoke(new Action(() =>
                {
                    lstResults.Items.Add(fileInfo);
                    lstResults.TopIndex = lstResults.Items.Count - 1;
                }));
            }
            else
            {
                lstResults.Items.Add(fileInfo);
                lstResults.TopIndex = lstResults.Items.Count - 1;
            }
        }

        private void lstResults_DoubleClick(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem is FileInformation fileInfo)
            {
                try
                {
                    string argument = "/select, \"" + fileInfo.FilePath + "\"";
                    Process.Start("explorer.exe", argument);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error opening file location: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
