using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FileReport47.Models;

namespace FileReport47.Services
{
    public class FileSearchService
    {
        private CancellationTokenSource _cancellationTokenSource;
        private const int WRITE_BUFFER_SIZE = 100; // Écrire tous les X fichiers
        private StreamWriter _writer;
        private ConcurrentQueue<FileInformation> _writeBuffer;
        private object _writeLock = new object();
        private volatile bool _isWriting = true;

        public async Task<bool> SearchFilesAsync(SearchParameters parameters, IProgress<(int matched, int total, FileInformation lastFile)> progress)
        {
            int totalFiles = 0;
            int matchedFiles = 0;
            _writeBuffer = new ConcurrentQueue<FileInformation>();
            _isWriting = true;

            _cancellationTokenSource = new CancellationTokenSource();
            string outputPath = ReplaceDatePatterns(parameters.OutputPath);

            try
            {
                // Créer le répertoire de sortie s'il n'existe pas
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Ouvrir le fichier en mode append
                _writer = new StreamWriter(outputPath, false);
                await _writer.WriteLineAsync(FileInformation.GetCsvHeader());

                // Démarrer une tâche d'écriture en arrière-plan
                var writeTask = Task.Run(async () =>
                {
                    while (_isWriting)
                    {
                        try
                        {
                            await FlushBufferAsync();
                            await Task.Delay(100); // Attendre un peu avant la prochaine écriture
                        }
                        catch
                        {
                            // Ignorer les erreurs pendant l'écriture
                        }
                    }
                });

                // Démarrer la recherche
                var searchTask = Task.Run(() =>
                {
                    SearchDirectory(parameters.SearchPath, parameters.FileFilters,
                        ref totalFiles, ref matchedFiles, progress, _cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);

                // Attendre que la recherche soit terminée
                await searchTask;

                // Arrêter la tâche d'écriture et écrire les derniers fichiers
                _isWriting = false;
                await writeTask;
                await FlushBufferAsync();

                return !_cancellationTokenSource.Token.IsCancellationRequested;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _isWriting = false;
                if (_writer != null)
                {
                    try
                    {
                        await _writer.FlushAsync();
                        _writer.Dispose();
                    }
                    catch
                    {
                        // Ignorer les erreurs pendant la fermeture
                    }
                }
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task FlushBufferAsync()
        {
            if (_writer == null || _writeBuffer.IsEmpty) return;

            var filesToWrite = new List<FileInformation>();
            while (_writeBuffer.TryDequeue(out var file))
            {
                filesToWrite.Add(file);
            }

            if (filesToWrite.Count > 0)
            {
                lock (_writeLock)
                {
                    foreach (var file in filesToWrite)
                    {
                        _writer.WriteLine(file.ToCsvLine());
                    }
                }
                await _writer.FlushAsync();
            }
        }

        private void SearchDirectory(string directory, List<string> filters,
            ref int totalFiles, ref int matchedFilesCount, IProgress<(int matched, int total, FileInformation lastFile)> progress, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                // Énumération des sous-répertoires
                foreach (var dir in Directory.EnumerateDirectories(directory))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    try
                    {
                        SearchDirectory(dir, filters, ref totalFiles, ref matchedFilesCount, progress, cancellationToken);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                // Énumération des fichiers
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    totalFiles++;

                    try
                    {
                        var fileName = Path.GetFileName(file);
                        if (!MatchesAnyFilter(fileName, filters))
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

                        _writeBuffer.Enqueue(fileInformation);
                        matchedFilesCount++;
                        progress?.Report((matchedFilesCount, totalFiles, fileInformation));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (Exception)
            {
                return;
            }
        }

        private string ReplaceDatePatterns(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            DateTime now = DateTime.Now;
            string result = path;
            
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
                    replacement = "{" + pattern + "}";
                }

                result = result.Substring(0, start) + replacement + result.Substring(end + 1);
            }

            return result;
        }

        private bool MatchesAnyFilter(string fileName, List<string> filters)
        {
            if (filters == null || filters.Count == 0)
                return true;

            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter))
                    continue;

                var pattern = filter.Trim().Replace(".", "\\.").Replace("*", ".*").Replace("?", ".");
                if (System.Text.RegularExpressions.Regex.IsMatch(fileName, "^" + pattern + "$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        public void CancelSearch()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
