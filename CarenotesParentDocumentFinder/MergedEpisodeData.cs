using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder
{
    public class MergedEpisodeData
    {
        public int Contextual_ID { get; set; }
        public int Community_Episode_ID { get; set; }
        public int Community_Episode_Location_ID { get; set; }
        public string Community_Episode_Location_Description { get; set; }
        public int Parent_CN_Doc_ID { get; set; }

        public int? Patient_ID { get; set; }
    }
}
