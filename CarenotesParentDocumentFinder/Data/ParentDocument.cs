using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder.Data
{
    public class ParentDocument
    {
        public int PatientID { get; set; }
        public int DocumentTypeID { get; set; }
        public string DocumentTypeDescription { get; set; }
        public int DocumentId { get; set; }
        public int ContextualId { get; set; }
        public int? ReferralId { get; set; }
        public int? EpisodeId { get; set; }
        public string DocumentSummary { get; set; }
        public bool Active { get; set; }
    }
}
