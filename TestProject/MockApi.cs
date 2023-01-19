using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.Interfaces;
using System;
using System.Collections.Generic;

namespace TestProject
{
    internal class MockApi : IApiClient
    {
        readonly bool _flaky = false;
        readonly bool _unavailable = false;

        public MockApi(bool flaky = false, bool unavailable = false)
        {
            _flaky = flaky;
            _unavailable = unavailable;
        }


        public bool ApiIsAvailable(RestSharp.RestClient apiClient)
        {

            if (_unavailable) return false;

            if (_flaky)
            {
                Random rnd = new Random();
                return rnd.NextDouble() > 0.5;
            }

            return true;
        }

        public List<CommunityEpisode> GetCommunityEpisodeDocuments(RestSharp.RestClient apiClient, int patientId, int pageSize)
        {
            if (_unavailable) return null;

            if (_flaky)
            {
                Random rnd = new Random();
                bool state = rnd.NextDouble() > 0.5;

                if (state) return new List<CommunityEpisode>();
                else return null;
            }


            return new List<CommunityEpisode>();
        }

        public List<InpatientEpisode> GetInpatientEpisodeDocuments(RestSharp.RestClient apiClient, int patientId, int pageSize)
        {
            if (_unavailable) return null;

            if (_flaky)
            {
                Random rnd = new Random();
                bool state = rnd.NextDouble() > 0.5;

                if (state) return new List<InpatientEpisode>();
                else return null;
            }

            return new List<InpatientEpisode>();
        }

        public List<ParentDocument> GetParentDocuments(RestSharp.RestClient apiClient, int patientId, int objectTypeId, int pageSize)
        {
            if (_unavailable) return null;

            if (_flaky)
            {
                Random rnd = new Random();
                bool state = rnd.NextDouble() > 0.5;

                if (state) return new List<ParentDocument>();
                else return null;
            }

            return new List<ParentDocument>();
        }

        public void GetSessionToken(RestSharp.RestClient apiClient)
        {
            // Method intentionally left empty.
        }

        public List<TeamEpisode> GetTeamEpisodeDocuments(RestSharp.RestClient apiClient, int patientId, int pageSize)
        {
            if (_unavailable) return null;

            if (_flaky)
            {
                Random rnd = new Random();
                bool state = rnd.NextDouble() > 0.5;

                if (state) return new List<TeamEpisode>();
                else return null;
            }

            return new List<TeamEpisode>();
        }

        public bool SessionTokenExists()
        {
            if (_unavailable) return false;

            if (_flaky)
            {
                Random rnd = new Random();
                return rnd.NextDouble() > 0.5;
            }

            return true;

        }
    }
}
