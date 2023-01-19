using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.DocumentProcessors;
using CarenotesParentDocumentFinder.Interfaces;
using NUnit.Framework;
using System.Collections.Generic;

namespace TestProject
{
    public class DataObjectTests
    {

        [Test]
        public void CanInitialiseEpisode()
        {
            Episode testEpisode = new Episode() { CnDocID = 1234, EpisodeID = 1, EpisodeTypeID = 1, LocationDesc = "Location A", LocationID = 1, PatientID = 122, ReferralID = 1, ReferralStatusID = 0, ServiceID = 1 };
            
            Assert.NotNull(testEpisode);
            
            Assert.Pass();
        }

        public void CanRequestInpatientEpisode()
        {
            IApiClient mockClient = new MockApi();

            IMockCommon

            List<int> mockIdentifiers = new List<int>();

            EpisodeDocumentProcessor episodeDocumentProcessor = new EpisodeDocumentProcessor(mockClient, 1, 100, mockIdentifiers, 
        }


    }
}