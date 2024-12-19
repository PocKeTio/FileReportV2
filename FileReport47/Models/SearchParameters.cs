using System;
using System.Collections.Generic;

namespace FileReport47.Models
{
    [Serializable]
    public class SearchParameters
    {
        public string SearchPath { get; set; }
        public string OutputPath { get; set; }
        public List<string> FileFilters { get; set; } = new List<string>();
    }
}
