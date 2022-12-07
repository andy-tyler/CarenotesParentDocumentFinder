namespace CarenotesParentDocumentFinder.Data
{

    public static class PicklistValues
    {
        public enum ObjectType 
        { 
            CommunityEpisode = 52,
            DayHospitalEpisode = 58,
            InpatientEpisode = 75,
            OutpatientEpisode = 93,
            TeamEpisode = 110 
        }

        public enum EpisodeType 
        { 
            Community = 1, 
            DayHospital = 2, 
            Inpatient = 3, 
            Outpatient = 4, 
            Team = 5 
        }

        public enum ReferralStatus 
        {
            Waiting = 0,
            Accepted = 1,
            Discharged = 2,
            Rejected = 3 
        }

        public enum OutputMethod
        {
            Tabbed = 0,
            Verbose = 1
        }
    }
}
