namespace CarenotesParentDocumentFinder.Data
{
    public class Episode
    {
        public int PatientID { get; set; }
        public int EpisodeID { get; set; }
        public int EpisodeTypeID { get; set; }
        public int? LocationID { get; set; }
        public string LocationDesc { get; set; }
        public int ServiceID { get; set; }
        public int ReferralStatusID { get; set; }
        public int CnDocID { get; set; }
        public int? ReferralID { get; set; }
    }
}
