using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder.Data
{
    public class ParentDocument
    {
        public int patientID { get; set; }
        public int documentTypeID { get; set; }
        public string documentTypeDescription { get; set; }
        public int documentId { get; set; }
        public int contextualId { get; set; }
        public int? referralId { get; set; }
        public int? episodeId { get; set; }
        public string documentSummary { get; set; }
        public bool active { get; set; }
    }
}
