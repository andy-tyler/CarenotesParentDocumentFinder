using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder.Data
{
    internal class Episode
    {
        public int patientID { get; set; }
        public int episodeID { get; set; }
        public int episodeTypeID { get; set; }
        public int? locationID { get; set; }
        public string locationDesc { get; set; }
        public int serviceID { get; set; }
        public int referralStatusID { get; set; }

        public int cnDocID { get; set; }
    }
}
