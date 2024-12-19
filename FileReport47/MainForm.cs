using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using FileReport47.Services;
using System.IO;
using System.Diagnostics;
using FileReport47.Models;

namespace FileReport47
{
    public partial class MainForm : Form
    {
        private readonly FileSearchService _fileSearchService;
        private bool _isSearching;
        private ResultsForm _resultsForm;

        public MainForm()
        {
            InitializeComponent();
            _fileSearchService = new FileSearchService();
        }

        private void chkShowResults_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowResults.Checked)
            {
                if (_resultsForm == null || _resultsForm.IsDisposed)
                {
                    _resultsForm = new ResultsForm();
                    _resultsForm.FormClosing += (s, args) => chkShowResults.Checked = false;
                }
                _resultsForm.Show();
            }
            else
            {
                _resultsForm?.Close();
            }
        }

        private void UpdateProgress((int matched, int total) progress)
        {
            lblProgress.Text = $"Found {progress.matched} files out of {progress.total} scanned...";
        }

        private void UpdateControlsState(bool searching)
        {
            txtSearchPath.Enabled = !searching;
            txtOutputPath.Enabled = !searching;
            txtFilters.Enabled = !searching;
            btnBrowseSearch.Enabled = !searching;
            btnBrowseOutput.Enabled = !searching;
            btnSearch.Enabled = !searching;
            btnCancel.Enabled = searching;
            chkShowResults.Enabled = !searching;
        }

        private void btnBrowseSearch_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select search directory";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtSearchPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.DefaultExt = "csv";
                dialog.AddExtension = true;
                dialog.Title = "Select output file";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtOutputPath.Text = dialog.FileName;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_isSearching)
            {
                _fileSearchService.CancelSearch();
                btnCancel.Enabled = false;
            }
        }

        public void LoadSettingsAndSearch(string settingsPath)
        {
            if (!File.Exists(settingsPath))
            {
                MessageBox.Show(this, $"Error: Settings file not found: {settingsPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(SearchParameters));
                SearchParameters settings;
                using (var reader = System.Xml.XmlReader.Create(settingsPath))
                {
                    settings = (SearchParameters)serializer.ReadObject(reader);
                }

                txtSearchPath.Text = settings.SearchPath;
                txtOutputPath.Text = settings.OutputPath;
                txtFilters.Text = string.Join(";", settings.FileFilters);

                btnSearch.PerformClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (_isSearching)
                return;

            if (string.IsNullOrWhiteSpace(txtSearchPath.Text) || string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show(this, "Please specify both search and output paths.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(txtSearchPath.Text))
            {
                MessageBox.Show(this, "Search directory does not exist.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _isSearching = true;
                UpdateControlsState(true);
                progressBar.Style = ProgressBarStyle.Marquee;
                lblProgress.Text = "Searching...";

                var parameters = new SearchParameters
                {
                    SearchPath = txtSearchPath.Text,
                    OutputPath = txtOutputPath.Text,
                    FileFilters = new System.Collections.Generic.List<string>(txtFilters.Text.Split(new[] { ';' },
                        StringSplitOptions.RemoveEmptyEntries))
                };

                var progress = new Progress<(int matched, int total, FileInformation lastFile)>(p =>
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            UpdateProgress((p.matched, p.total));
                            if (chkShowResults.Checked && _resultsForm != null && !_resultsForm.IsDisposed && p.lastFile != null)
                            {
                                _resultsForm.AddResult(p.lastFile);
                            }
                        }));
                    }
                    else
                    {
                        UpdateProgress((p.matched, p.total));
                        if (chkShowResults.Checked && _resultsForm != null && !_resultsForm.IsDisposed && p.lastFile != null)
                        {
                            _resultsForm.AddResult(p.lastFile);
                        }
                    }
                });

                bool completed = await _fileSearchService.SearchFilesAsync(parameters, progress);

                if (completed)
                {
                    MessageBox.Show(this, "Search completed successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "Search was cancelled.", "Cancelled",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isSearching = false;
                UpdateControlsState(false);
                progressBar.Style = ProgressBarStyle.Blocks;
                lblProgress.Text = "Ready";
            }
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.DefaultExt = "xml";
                dialog.AddExtension = true;
                dialog.Title = "Save Settings";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var settings = new SearchParameters
                        {
                            SearchPath = txtSearchPath.Text,
                            OutputPath = txtOutputPath.Text,
                            FileFilters = new System.Collections.Generic.List<string>(txtFilters.Text.Split(new[] { ';' },
                                StringSplitOptions.RemoveEmptyEntries))
                        };

                        var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(SearchParameters));
                        using (var writer = System.Xml.XmlWriter.Create(dialog.FileName))
                        {
                            serializer.WriteObject(writer, settings);
                        }

                        MessageBox.Show(this, "Settings saved successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error saving settings: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnLoadSettings_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.DefaultExt = "xml";
                dialog.Title = "Load Settings";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(SearchParameters));
                        SearchParameters settings;
                        using (var reader = System.Xml.XmlReader.Create(dialog.FileName))
                        {
                            settings = (SearchParameters)serializer.ReadObject(reader);
                        }

                        txtSearchPath.Text = settings.SearchPath;
                        txtOutputPath.Text = settings.OutputPath;
                        txtFilters.Text = string.Join(";", settings.FileFilters);

                        MessageBox.Show(this, "Settings loaded successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error loading settings: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
