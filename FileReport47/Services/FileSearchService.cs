using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileReport47.Models;

namespace FileReport47.Services
{
    public class FileSearchService
    {
        private CancellationTokenSource _cancellationTokenSource;
        public event Action<int, int> ProgressUpdated;
        
        public async Task SearchFilesAsync(SearchParameters parameters, IProgress<(int, int)> progress)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var foundFiles = new List<FileInformation>();
            var processedFiles = 0;
            var matchedFiles = 0;

            try
            {
                var directories = new Queue<string>();
                directories.Enqueue(parameters.SearchPath);

                while (directories.Count > 0 && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    string currentDir = directories.Dequeue();
                    
                    try
                    {
                        // Énumération des sous-répertoires
                        foreach (var dir in Directory.EnumerateDirectories(currentDir))
                        {
                            try
                            {
                                directories.Enqueue(dir);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Continue si un répertoire n'est pas accessible
                                continue;
                            }
                            catch (Exception)
                            {
                                // Continue pour toute autre erreur de répertoire
                                continue;
                            }
                        }

                        // Énumération des fichiers
                        foreach (var file in Directory.EnumerateFiles(currentDir))
                        {
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                                break;

                            processedFiles++;

                            try
                            {
                                var fileName = Path.GetFileName(file);
                                if (!MatchesAnyFilter(fileName, parameters.FileFilters))
                                    continue;

                                var fileInfo = new FileInfo(file);
                                var fileInformation = new FileInformation
                                {
                                    FileName = fileName,
                                    FilePath = file,
                                    FileSize = fileInfo.Length,
                                    CreationTime = fileInfo.CreationTime,
                                    LastModifiedTime = fileInfo.LastWriteTime
                                };

                                foundFiles.Add(fileInformation);
                                matchedFiles++;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                continue;
                            }
                            catch (IOException)
                            {
                                continue;
                            }

                            progress?.Report((matchedFiles, processedFiles));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Continue si le répertoire n'est pas accessible
                        continue;
                    }
                    catch (Exception)
                    {
                        // Continue pour toute autre erreur
                        continue;
                    }
                }

                // Write results to CSV
                if (foundFiles.Any() && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await WriteToCsvAsync(foundFiles, parameters.OutputPath);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool MatchesAnyFilter(string fileName, List<string> filters)
        {
            if (filters == null || filters.Count == 0)
                return true;

            return filters.Any(filter => 
                filter.Contains("*") || filter.Contains("?") 
                    ? IsWildcardMatch(fileName, filter) 
                    : fileName.Equals(filter, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsWildcardMatch(string fileName, string pattern)
        {
            string regexPattern = "^" + pattern
                .Replace(".", "\\.")
                .Replace("*", ".*")
                .Replace("?", ".") + "$";

            return System.Text.RegularExpressions.Regex.IsMatch(
                fileName, 
                regexPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private async Task WriteToCsvAsync(List<FileInformation> files, string outputPath)
        {
            // Remplacer les motifs de date dans le chemin de sortie
            string finalPath = ReplaceDatePatterns(outputPath);

            // Créer le répertoire de sortie s'il n'existe pas
            string directory = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(finalPath, false))
            {
                await writer.WriteLineAsync(FileInformation.GetCsvHeader());
                foreach (var file in files)
                {
                    await writer.WriteLineAsync(file.ToCsvLine());
                }
            }
        }

        private string ReplaceDatePatterns(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Récupérer la date actuelle
            DateTime now = DateTime.Now;

            // Remplacer tous les motifs de date possibles
            string result = path;
            
            // Chercher tous les motifs entre accolades
            while (true)
            {
                int start = result.IndexOf('{');
                if (start == -1) break;

                int end = result.IndexOf('}', start);
                if (end == -1) break;

                string pattern = result.Substring(start + 1, end - start - 1);
                string replacement;
                try
                {
                    replacement = now.ToString(pattern);
                }
                catch (FormatException)
                {
                    // Si le format n'est pas valide, on laisse le motif tel quel
                    replacement = "{" + pattern + "}";
                }

                result = result.Substring(0, start) + replacement + result.Substring(end + 1);
            }

            return result;
        }

        public void CancelSearch()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
