using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;

namespace CarenotesParentDocumentFinder
{
    public static class APIClient
    {
        static string _apiSessionToken = string.Empty;

        static SecureString _securePassword;

        static string _username = string.Empty;

        static int _totalPages = -1;

        public static List<ParentDocument> GetParentDocuments(RestClient apiClient, int patientId, int objectTypeId, int pageSize)
        {
            try
            {

                List<ParentDocument> parentDocuments = new List<ParentDocument>();

                int currentPageNumber = 1;

                RestRequest request = new RestRequest($"parent-documents?PatientId={patientId}&DocumentTypeId={objectTypeId}&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                request.AddHeader("X-Session-Id", _apiSessionToken);

                Console.WriteLine($"Requesting parent documents for patient ID: {patientId}");

                using (var progress = new ProgressBar())
                {

                    var response = apiClient.ExecuteGet(request);

                    if (response.IsSuccessful)
                    {
                        parentDocuments.AddRange(ParseParentDocumentJson(response.Content, patientId));


                        if (_totalPages > 1)
                        {
                            currentPageNumber++;

                            progress.Report((double)currentPageNumber / _totalPages);

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

                        progress.Report((double)currentPageNumber / _totalPages);

                        return parentDocuments;
                    }
                    else
                    {
                        throw new Exception($"API request was unsucessful: {response.ErrorException.Message}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static List<ParentDocument> ParseParentDocumentJson(string responseContent, int patientId)
        {
            try
            {

                List<ParentDocument> parentDocuments = new List<ParentDocument>();

                JObject json = JObject.Parse(responseContent);

                var output = JsonConvert.DeserializeObject<object>(json.ToString());

                int parentDocumentsCount = -1;

                parentDocumentsCount = json.SelectToken("parentDocuments").Count();

                _totalPages = (int)json.SelectToken("pageDetails.totalPages");

                for (int i = 0; i < parentDocumentsCount; i++)
                {
                    parentDocuments.Add(new ParentDocument()
                    {
                        patientID = patientId,

                        documentTypeID = (int)json.SelectToken("parentDocuments[" + i + "].documentTypeId"),

                        documentTypeDescription = (string)json.SelectToken("parentDocuments[" + i + "].documentTypeDescription"),

                        documentId = (int)json.SelectToken("parentDocuments[" + i + "].documentId"),

                        contextualId = (int)json.SelectToken("parentDocuments[" + i + "].contextualId"),

                        referralId = (int?)json.SelectToken("parentDocuments[" + i + "].referralId"),

                        episodeId = (int?)json.SelectToken("parentDocuments[" + i + "].episodeId"),

                        documentSummary = (string)json.SelectToken("parentDocuments[" + i + "].documentSummary"),

                        active = (bool)json.SelectToken("parentDocuments[" + i + "].active")
                    });
                }

                return parentDocuments;
            }
            catch (Exception ex)
            {
                string n1 = ex.StackTrace;
                throw;
            }

        }

        public static List<CommunityEpisode> GetCommunityEpisodeDocuments(RestClient apiClient, int patientId, int pageSize)
        {
            List<CommunityEpisode> communityEpisodes = new List<CommunityEpisode>();

            int currentPageNumber = 1;

            RestRequest request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID=1&ReferralStatusId=1&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

            request.AddHeader("X-Session-Id", _apiSessionToken);

            using (var progress = new ProgressBar())
            {

                var response = apiClient.ExecuteGet(request);


                if (response.IsSuccessful)
                {
                    communityEpisodes.AddRange(ParseCommunityEpisodeJson(response.Content, patientId));

                    if (_totalPages > 1)
                    {
                        currentPageNumber++;

                        progress.Report((double)currentPageNumber / _totalPages);

                        while (currentPageNumber <= _totalPages)
                        {

                            pageSize = 1;

                            request = new RestRequest($"episodes.json?PatientId={patientId}&episodeTypeID=1&ReferralStatusId=1&PageIndex={currentPageNumber}&PageSize={pageSize}", Method.Get);

                            response = apiClient.ExecuteGet(request);

                            if (response.IsSuccessful)
                            {
                                communityEpisodes.AddRange(ParseCommunityEpisodeJson(response.Content, patientId));
                            }

                            currentPageNumber++;

                        }

                    }

                    progress.Report((double)currentPageNumber / _totalPages);

                    return communityEpisodes;
                }
                else
                {
                    throw new Exception($"API request was unsucessful: {response.ErrorException.Message}");
                }
            }
        }

        public static List<CommunityEpisode> ParseCommunityEpisodeJson(string responseContent, int patientId)
        {


            List<CommunityEpisode> communityEpisodes = new List<CommunityEpisode>();

            JObject json = JObject.Parse(responseContent);

            var output = JsonConvert.DeserializeObject<object>(json.ToString());

            int communityEpisodeCount = -1;

            communityEpisodeCount = json.SelectToken("communityEpisodeDetails").Count();

            JToken nx;

            json.TryGetValue("pageDetails.totalPages", StringComparison.InvariantCulture, out nx);

            if (nx != null)
                _totalPages = (int)json.SelectToken("pageDetails.totalPages");

            for (int i = 0; i < communityEpisodeCount; i++)
            {
                communityEpisodes.Add(new CommunityEpisode()
                {
                    episodeID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].episodeID"),

                    episodeTypeID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].episodeTypeID"),

                    locationID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].locationID"),

                    referralStatusID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].referralStatusID"),

                    serviceID = (int)json.SelectToken("communityEpisodeDetails[" + i + "].serviceID"),

                    locationDesc = (string)json.SelectToken("communityEpisodeDetails[" + i + "].locationDesc")

                });
            }

            return communityEpisodes;

        }

        public static void GetSessionToken()
        {

            var options = new RestClientOptions("http://ahc-demo-cons.adastra.co.uk/api/integrationn/") { MaxTimeout = -1};

            var apiClient = new RestClient(options);

            Console.WriteLine();

            Console.Write("Carenotes username: ");

            string userName = Console.ReadLine();

            Console.Write("Carenotes password: ");

            SecureString password = new SecureString();


            while (true)
            {
                ConsoleKeyInfo consoleKeyInfo;
                do
                {
                    consoleKeyInfo = Console.ReadKey(true);
                    if (consoleKeyInfo.Key != ConsoleKey.Enter)
                    {
                        if (consoleKeyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
                        {
                            Console.Write("\b \b");
                            password.RemoveAt(password.Length - 1);
                        }
                    }
                    else
                        goto label_6;
                }
                while (char.IsControl(consoleKeyInfo.KeyChar));
                Console.Write("*");
                password.AppendChar(consoleKeyInfo.KeyChar);
            }

        label_6:
            password.MakeReadOnly();

            _username = userName;

            _securePassword = password;

            Console.WriteLine();


            NetworkCredential creds = new NetworkCredential(string.Empty, _securePassword);

            RestRequest request = new RestRequest("Session.json", Method.Post);

            request.AddParameter("UserName", _username);

            request.AddParameter("Password", new NetworkCredential("", _securePassword).Password);

            var response = apiClient.ExecutePost(request);

            Console.WriteLine();

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

                throw new Exception($"Unable to obtain session token from API: {response.ErrorMessage}");
            }
        }
    }
}