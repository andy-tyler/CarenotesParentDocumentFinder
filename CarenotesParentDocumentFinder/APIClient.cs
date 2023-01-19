using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using static CarenotesParentDocumentFinder.Data.PicklistValues;

namespace CarenotesParentDocumentFinder
{
    public class ApiClient : IApiClient
    {
        string _apiSessionToken;

        int _totalPages = -1;

        public void GetSessionToken(RestClient apiClient)
        {

            Console.WriteLine();

            Console.Write("Carenotes username: ");

            string userName = Console.ReadLine();

            SecureString securePassword = GetPassword();

            Console.WriteLine();

            RestRequest request = new RestRequest("session.json", Method.Post);

            request.AddParameter("UserName", userName);

            request.AddParameter("Password", new NetworkCredential("", securePassword).Password);

            var response = apiClient.ExecutePost(request);

            CheckResponseStatus(response);
        }

        private SecureString GetPassword()
        {
            Console.Write("Carenotes password: ");

            SecureString password = new SecureString();
            ConsoleKeyInfo keyInfo;

            do
            {
                keyInfo = Console.ReadKey(true);
                // Skip if Backspace or Enter is Pressed
                if (keyInfo.Key != ConsoleKey.Backspace && keyInfo.Key != ConsoleKey.Enter)
                {
                    password.AppendChar(keyInfo.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        // Remove last charcter if Backspace is Pressed
                        Console.Write("\b \b");
                        password.RemoveAt(password.Length - 1);
                    }
                }
            }
            // Stops Getting Password Once Enter is Pressed
            while (keyInfo.Key != ConsoleKey.Enter);

            password.MakeReadOnly();

            SecureString securePassword = password;

            return securePassword;
        }

        private void CheckResponseStatus(RestResponse response)
        {
            if (response.IsSuccessful)
            {

                JToken tok = JObject.Parse(response.Content.ToString());

                _apiSessionToken = tok.SelectToken("sessionId").ToString();
            }
            else
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new UriFormatException("API not found at specified URI, check URI in configuration file.");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException("API credentials supplied are invalid.");

                throw new WebException($"Unable to obtain session token from API: {response.ErrorMessage}");
            }
        }

        public bool ApiIsAvailable(RestClient apiClient)
        {

            RestRequest request = new RestRequest("ping", Method.Get);

            var response = apiClient.ExecuteGet(request);

            return (response.IsSuccessful);

        }

        public List<ParentDocument> GetParentDocuments(RestClient apiClient, int patientId, int objectTypeId, int pageSize)
        {
            try
            {

                List<ParentDocument> parentDocuments = new List<ParentDocument>();

                int currentPageNumber = 1;

                RestRequest request = new RestRequest($"parent-documents?PatientId={patientId}&DocumentTypeId={objectTypeId}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                request.AddHeader("X-Session-Id", _apiSessionToken);

                var response = apiClient.ExecuteGet(request);

                if (response.IsSuccessful)
                {
                    parentDocuments.AddRange(ParseParentDocumentJson(response.Content, patientId));


                    if (_totalPages > 1)
                    {
                        currentPageNumber++;

                        while (currentPageNumber <= _totalPages)
                        {

                            request = new RestRequest($"parent-documents?PatientId={patientId}&DocumentTypeId={objectTypeId}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                            request.AddHeader("X-Session-Id", _apiSessionToken);

                            response = apiClient.ExecuteGet(request);

                            if (response.IsSuccessful)
                            {
                                parentDocuments.AddRange(ParseParentDocumentJson(response.Content, patientId));
                            }

                            currentPageNumber++;

                        }

                    }

                    return parentDocuments;
                }
                else
                {
                    throw new WebException($"API request was unsucessful: {response.ErrorException.Message}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ParentDocument>();
            }
        }

        private List<ParentDocument> ParseParentDocumentJson(string responseContent, int patientId)
        {

            List<ParentDocument> parentDocuments = new List<ParentDocument>();
            JObject json = JObject.Parse(responseContent);
            int parentDocumentsCount = json.SelectToken("parentDocuments").Count();

            _totalPages = (int)json.SelectToken("pageDetails.totalPages");

            for (int i = 0; i < parentDocumentsCount; i++)
            {
                parentDocuments.Add(new ParentDocument()
                {
                    PatientID = patientId,

                    DocumentTypeID = (int)json.SelectToken("parentDocuments[" + i + "].documentTypeId"),

                    DocumentTypeDescription = (string)json.SelectToken("parentDocuments[" + i + "].documentTypeDescription"),

                    DocumentId = (int)json.SelectToken("parentDocuments[" + i + "].documentId"),

                    ContextualId = (int)json.SelectToken("parentDocuments[" + i + "].contextualId"),

                    ReferralId = (int?)json.SelectToken("parentDocuments[" + i + "].referralId"),

                    EpisodeId = (int?)json.SelectToken("parentDocuments[" + i + "].episodeId"),

                    DocumentSummary = (string)json.SelectToken("parentDocuments[" + i + "].documentSummary"),

                    Active = (bool)json.SelectToken("parentDocuments[" + i + "].active")
                });
            }

            return parentDocuments;

        }

        public List<CommunityEpisode> GetCommunityEpisodeDocuments(RestClient apiClient, int patientId, int pageSize)
        {
            List<CommunityEpisode> communityEpisodes = new List<CommunityEpisode>();

            int currentPageNumber = 1;

            RestRequest request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID={(int)EpisodeType.Community}&ReferralStatusId={(int)ReferralStatus.Accepted}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

            request.AddHeader("X-Session-Id", _apiSessionToken);

            var response = apiClient.ExecuteGet(request);


            if (response.IsSuccessful)
            {
                communityEpisodes.AddRange(ParseCommunityEpisodeJson(response.Content));

                if (_totalPages > 1)
                {
                    currentPageNumber++;

                    while (currentPageNumber <= _totalPages)
                    {

                        pageSize = 1;

                        request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID={(int)EpisodeType.Community}&ReferralStatusId={(int)ReferralStatus.Accepted}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                        response = apiClient.ExecuteGet(request);

                        if (response.IsSuccessful)
                        {
                            communityEpisodes.AddRange(ParseCommunityEpisodeJson(response.Content));
                        }

                        currentPageNumber++;

                    }

                }

                return communityEpisodes;
            }
            else
            {
                throw new WebException($"API request was unsucessful: {response.ErrorException.Message}");
            }
        }

        private List<CommunityEpisode> ParseCommunityEpisodeJson(string responseContent)
        {

            List<CommunityEpisode> communityEpisodes = new List<CommunityEpisode>();

            JObject json = JObject.Parse(responseContent);
            int communityEpisodeCount = json.SelectToken("communityEpisodeDetails").Count();
            json.TryGetValue("pageDetails.totalPages", StringComparison.InvariantCulture, out JToken nx);

            if (nx != null)
                _totalPages = (int)json.SelectToken("pageDetails.totalPages");

            for (int i = 0; i < communityEpisodeCount; i++)
            {
                communityEpisodes.Add(new CommunityEpisode()
                {
                    EpisodeID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].episodeID"),

                    EpisodeTypeID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].episodeTypeID"),

                    LocationID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].locationID"),

                    ReferralStatusID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].referralStatusID"),

                    ServiceID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].serviceID"),

                    LocationDesc = (string)json.SelectToken("communityEpisodeDetails[" + i + "].locationDesc")

                });
            }

            return communityEpisodes;

        }

        public List<InpatientEpisode> GetInpatientEpisodeDocuments(RestClient apiClient, int patientId, int pageSize)
        {

            List<InpatientEpisode> inpatientEpisodes = new List<InpatientEpisode>();

            int currentPageNumber = 1;

            RestRequest request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID={(int)EpisodeType.Inpatient}&ReferralStatusId={(int)ReferralStatus.Accepted}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

            request.AddHeader("X-Session-Id", _apiSessionToken);

            var response = apiClient.ExecuteGet(request);

            if (response.IsSuccessful)
            {
                inpatientEpisodes.AddRange(ParseInpatientEpisodeJson(response.Content));

                if (_totalPages > 1)
                {
                    currentPageNumber++;

                    while (currentPageNumber <= _totalPages)
                    {

                        pageSize = 1;

                        request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID={(int)EpisodeType.Inpatient}&ReferralStatusId={(int)ReferralStatus.Accepted}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                        response = apiClient.ExecuteGet(request);

                        if (response.IsSuccessful)
                        {
                            inpatientEpisodes.AddRange(ParseInpatientEpisodeJson(response.Content));
                        }

                        currentPageNumber++;

                    }

                }

                return inpatientEpisodes;
            }
            else
            {
                throw new WebException($"API request was unsucessful: {response.ErrorException.Message}");
            }


        }

        private List<InpatientEpisode> ParseInpatientEpisodeJson(string responseContent)
        {
            List<InpatientEpisode> inpatientEpisodes = new List<InpatientEpisode>();

            JObject json = JObject.Parse(responseContent);
            int inpatientEpisodeCount = json.SelectToken("inpatientEpisodeDetails").Count();
            json.TryGetValue("pageDetails.totalPages", StringComparison.InvariantCulture, out JToken nx);

            if (nx != null)
                _totalPages = (int)json.SelectToken("pageDetails.totalPages");

            for (int i = 0; i < inpatientEpisodeCount; i++)
            {
                inpatientEpisodes.Add(new InpatientEpisode()
                {
                    EpisodeID = (int)json.SelectToken("inpatientEpisodeDetails[" + i + "].episodeID"),

                    EpisodeTypeID = (int)json.SelectToken("inpatientEpisodeDetails[" + i + "].episodeTypeID"),

                    LocationID = (int?)json.SelectToken("inpatientEpisodeDetails[" + i + "].locationID"),

                    ReferralStatusID = (int)json.SelectToken("inpatientEpisodeDetails[" + i + "].referralStatusID"),

                    ServiceID = (int)json.SelectToken("inpatientEpisodeDetails[" + i + "].serviceID"),

                    LocationDesc = (string)json.SelectToken("inpatientEpisodeDetails[" + i + "].locationDesc")

                });
            }

            return inpatientEpisodes;

        }

        public List<TeamEpisode> GetTeamEpisodeDocuments(RestClient apiClient, int patientId, int pageSize)
        {
            List<TeamEpisode> inpatientEpisodes = new List<TeamEpisode>();

            int currentPageNumber = 1;

            RestRequest request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID={(int)EpisodeType.Team}&ReferralStatusId={(int)ReferralStatus.Accepted}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

            request.AddHeader("X-Session-Id", _apiSessionToken);

            var response = apiClient.ExecuteGet(request);

            if (response.IsSuccessful)
            {
                inpatientEpisodes.AddRange(ParseTeamEpisodeJson(response.Content));

                if (_totalPages > 1)
                {
                    currentPageNumber++;

                    while (currentPageNumber <= _totalPages)
                    {

                        pageSize = 1;

                        request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID={(int)EpisodeType.Team}&ReferralStatusId={(int)ReferralStatus.Accepted}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                        response = apiClient.ExecuteGet(request);

                        if (response.IsSuccessful)
                        {
                            inpatientEpisodes.AddRange(ParseTeamEpisodeJson(response.Content));
                        }

                        currentPageNumber++;

                    }

                }

                return inpatientEpisodes;
            }
            else
            {
                throw new WebException($"API request was unsucessful: {response.ErrorException.Message}");
            }

        }

        private List<TeamEpisode> ParseTeamEpisodeJson(string responseContent)
        {
            List<TeamEpisode> teamEpisode = new List<TeamEpisode>();

            JObject json = JObject.Parse(responseContent);
            int teamEpisodeCount = json.SelectToken("teamEpisodeDetails").Count();
            json.TryGetValue("pageDetails.totalPages", StringComparison.InvariantCulture, out JToken nx);

            if (nx != null)
                _totalPages = (int)json.SelectToken("pageDetails.totalPages");

            for (int i = 0; i < teamEpisodeCount; i++)
            {
                teamEpisode.Add(new TeamEpisode()
                {
                    EpisodeID = (int)json.SelectToken("teamEpisodeDetails[" + i + "].episodeID"),

                    EpisodeTypeID = (int)json.SelectToken("teamEpisodeDetails[" + i + "].episodeTypeID"),

                    LocationID = (int?)json.SelectToken("teamEpisodeDetails[" + i + "].locationID"),

                    ReferralStatusID = (int)json.SelectToken("teamEpisodeDetails[" + i + "].referralStatusID"),

                    ServiceID = (int)json.SelectToken("teamEpisodeDetails[" + i + "].serviceID"),

                    LocationDesc = (string)json.SelectToken("teamEpisodeDetails[" + i + "].locationDesc")

                });
            }

            return teamEpisode;
        }

        public bool SessionTokenExists()
        {
            if (string.IsNullOrEmpty(_apiSessionToken))
                return false;

            return true;
        }

    }
}