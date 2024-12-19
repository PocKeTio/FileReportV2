using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FileReport47.Models
{
    [DataContract]
    public class SearchParameters
    {
        [DataMember]
        public string SearchPath { get; set; }
        
        [DataMember]
        public string OutputPath { get; set; }
        
        [DataMember]
        public List<string> FileFilters { get; set; } = new List<string>();
    }
}
