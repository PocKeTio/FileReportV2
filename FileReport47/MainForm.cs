using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileReport47.Models;
using FileReport47.Services;
using Newtonsoft.Json;

namespace FileReport47
{
    public partial class MainForm : Form
    {
        private readonly FileSearchService _fileSearchService;
        private bool _isSearching;

        public MainForm()
        {
            InitializeComponent();
            _fileSearchService = new FileSearchService();
            progressBar.Style = ProgressBarStyle.Blocks;
        }

        private void btnBrowseSearch_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
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
                dialog.DefaultExt = "csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputPath.Text = dialog.FileName;
                }
            }
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (_isSearching)
                return;

            if (string.IsNullOrWhiteSpace(txtSearchPath.Text) || string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show("Please specify both search and output paths.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(txtSearchPath.Text))
            {
                MessageBox.Show("Search directory does not exist.", "Validation Error", 
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
                    FileFilters = new List<string>(txtFilters.Text.Split(new[] { ';' }, 
                        StringSplitOptions.RemoveEmptyEntries))
                };

                var progress = new Progress<(int matched, int total)>(p =>
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(() => UpdateProgress(p)));
                    }
                    else
                    {
                        UpdateProgress(p);
                    }
                });

                await Task.Run(() => _fileSearchService.SearchFilesAsync(parameters, progress));

                MessageBox.Show("Search completed successfully!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", 
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

        private void UpdateProgress((int matched, int total) progress)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateProgress(progress)));
                return;
            }

            lblProgress.Text = $"Found {progress.matched} matching files out of {progress.total} processed";
            Application.DoEvents();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_isSearching)
            {
                _fileSearchService.CancelSearch();
            }
        }

        private void UpdateControlsState(bool isSearching)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateControlsState(isSearching)));
                return;
            }

            btnSearch.Enabled = !isSearching;
            btnCancel.Enabled = isSearching;
            btnBrowseSearch.Enabled = !isSearching;
            btnBrowseOutput.Enabled = !isSearching;
            txtSearchPath.Enabled = !isSearching;
            txtOutputPath.Enabled = !isSearching;
            txtFilters.Enabled = !isSearching;
            btnSaveSettings.Enabled = !isSearching;
            btnLoadSettings.Enabled = !isSearching;
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.DefaultExt = "json";
                    dialog.Title = "Save Search Settings";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var settings = new SearchParameters
                        {
                            SearchPath = txtSearchPath.Text,
                            OutputPath = txtOutputPath.Text,
                            FileFilters = new List<string>(txtFilters.Text.Split(new[] { ';' },
                                StringSplitOptions.RemoveEmptyEntries))
                        };

                        File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(settings, Formatting.Indented));
                        MessageBox.Show("Settings saved successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadSettings_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.DefaultExt = "json";
                    dialog.Title = "Load Search Settings";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var settings = JsonConvert.DeserializeObject<SearchParameters>(File.ReadAllText(dialog.FileName));
                        txtSearchPath.Text = settings.SearchPath;
                        txtOutputPath.Text = settings.OutputPath;
                        txtFilters.Text = string.Join(";", settings.FileFilters);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
