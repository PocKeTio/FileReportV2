using System;

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
    }
}
