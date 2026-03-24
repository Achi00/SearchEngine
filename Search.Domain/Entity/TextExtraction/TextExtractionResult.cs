using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search.Domain.Entity.TextExtraction
{
    public class TextExtractionResult
    {
        // string text
        public string FullText { get; set; } = string.Empty;
        // placement of text on image
        public List<TextRegion> Regions { get; set; } = [];
    }
}
