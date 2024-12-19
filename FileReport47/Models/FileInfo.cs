using System;
using System.IO;

namespace FileReport47.Models
{
    public class FileInformation
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModifiedTime { get; set; }

        public string ToCsvLine()
        {
            return $"\"{FileName}\",\"{FilePath}\",{FileSize},{CreationTime:yyyy-MM-dd HH:mm:ss},{LastModifiedTime:yyyy-MM-dd HH:mm:ss}";
        }

        public static string GetCsvHeader()
        {
            return "FileName,FilePath,FileSize,CreationTime,LastModifiedTime";
        }

        public override string ToString()
        {
            string sizeStr = FileSize < 1024 ? $"{FileSize} B" :
                            FileSize < 1024 * 1024 ? $"{FileSize / 1024:N1} KB" :
                            FileSize < 1024 * 1024 * 1024 ? $"{FileSize / (1024 * 1024):N1} MB" :
                            $"{FileSize / (1024 * 1024 * 1024):N1} GB";

            return $"{FileName} ({sizeStr}) - Modified: {LastModifiedTime:yyyy-MM-dd HH:mm:ss}";
        }
    }
}
