using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder.Data
{
    internal class Episode
    {
        public int PatientID { get; set; }
        public int EpisodeID { get; set; }
        public int EpisodeTypeID { get; set; }
        public int? LocationID { get; set; }
        public string LocationDesc { get; set; }
        public int ServiceID { get; set; }
        public int ReferralStatusID { get; set; }
        public int CnDocID { get; set; }
    }
}
