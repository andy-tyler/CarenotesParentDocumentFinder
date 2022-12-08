using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder.Data
{
    public class MergedEpisodeData
    {
        public int Patient_ID { get; set; }
        public int Contextual_ID { get; set; }
        public int Episode_ID { get; set; }
        public int Episode_Location_ID { get; set; }
        public string Episode_Location_Description { get; set; }
        public int Parent_CN_Doc_ID { get; set; }
        public int Service_ID { get; set; }

    }
}
