using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Config
{
    public class SequenceDto
    {
        public string ResolutionNumber { get; set; } = string.Empty;
        public string ResolutionDate { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public int FromNumber { get; set; } 
        public int ToNumber { get; set; } 
        public DateTime? ValidDateFrom { get; set; }
        public DateTime? ValidDateTo { get; set; } 
        public string TechnicalKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;



       
       
      
        
    }
}
