using CarenotesParentDocumentFinder.Data;
using RestSharp;
using System;
using System.Collections.Generic;

namespace CarenotesParentDocumentFinder.Interfaces
{
    public interface IApiClient
    {
        bool ApiIsAvailable(RestClient apiClient);
        List<CommunityEpisode> GetCommunityEpisodeDocuments(RestClient apiClient, int patientId, int pageSize);
        List<InpatientEpisode> GetInpatientEpisodeDocuments(RestClient apiClient, int patientId, int pageSize);
        List<ParentDocument> GetParentDocuments(RestClient apiClient, int patientId, int objectTypeId, int pageSize);
        void GetSessionToken(RestClient apiClient);
        List<TeamEpisode> GetTeamEpisodeDocuments(RestClient apiClient, int patientId, int pageSize);
        bool SessionTokenExists();

        TimeSpan ApiResponseTime(RestClient apiClient);
    }
}