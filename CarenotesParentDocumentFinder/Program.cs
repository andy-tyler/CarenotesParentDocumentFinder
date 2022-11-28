using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CarenotesParentDocumentFinder
{
    internal class Program
    {
        static string _patientIDFilePath = string.Empty;

        static string _username = string.Empty;

        static int _pageSize = 100;

        static int _objectTypeID = 50;

        static RestClient _apiClient = new RestClient(new RestClientOptions("http://ahc-demo-cons.adastra.co.uk/api/integration/") { MaxTimeout = -1});

        static void Main(string[] args)
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine("Carenotes Parent document reference extract tool v1.0");
            Console.WriteLine("******************************************************");

            //GetPatientIdentifiersFromFile();

            ProcessStartupParameters(args);
        }

        static void ProcessStartupParameters(string[] startupArgs)
        {
            try
            {
                if (startupArgs.Length == 0)
                {
                    Console.WriteLine("No switch has been specified to set the child document type. Re-run with /? for available options.");
                }

                switch (startupArgs[0])
                {
                    case "/?":
                        {
                            break;
                        }
                    case "/notes":
                        {
                            if (startupArgs[3] == "-f")
                            {
                                if (!String.IsNullOrEmpty(startupArgs[4]))
                                {
                                    _patientIDFilePath = startupArgs[4];
                                    ProcessPatientIDFile();
                                }
                                else
                                    throw new ArgumentException("File path not specified or invalid. Check file path parameter (-f) is valid.");
                            }
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException("Invalid command switch specified. Re-run with the /? switch for available options.");
                        }
                }

                Console.WriteLine("Processing complete, press any key to exit...");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception encountered:\n {ex.Message}");
            }
        }

        static void ProcessPatientIDFile()
        {

            //GetSessionToken();

            APIClient.GetSessionToken();


            // 1. Get patient ID's from customer supplied CSV file.

            List<int> patientIdentifiers = GetPatientIdentifiersFromFile();

            // 2. Retrieve parent documents for each patient ID and load into a list.


            foreach (int identifier in patientIdentifiers)
            {
                List<ParentDocument> parentDocuments = GetParentDocuments(identifier);

                ListParentDocumentDetails(parentDocuments, identifier);


            }


        }

        static List<int> GetPatientIdentifiersFromFile()
        {
            string contents = File.ReadAllText(@"C:\Drops\patients.csv");

            List<int> identifiers = contents.Split(',').Select(int.Parse).ToList();

            return identifiers;
        }

        static List<ParentDocument> GetParentDocuments(int patientID)
        {

            List<ParentDocument> parentDocuments = APIClient.GetParentDocuments(_apiClient, patientID, _objectTypeID, _pageSize);

            return parentDocuments;

        }

        static void ListParentDocumentDetails(List<ParentDocument> parentDocuments, int patientId)
        {

            var episodeIds = (from ce in parentDocuments
                              where ce.patientID == patientId
                              where ce.documentTypeID == 52
                              select ce.episodeId).Distinct().ToList();

            List<CommunityEpisode> communityEpisodes = new List<CommunityEpisode>();

            List<MergedEpisodeData> mergedEpisodeData = new List<MergedEpisodeData>();

            foreach (int? episodeId in episodeIds)
            {
                if (episodeId != null)
                {

                    communityEpisodes = APIClient.GetCommunityEpisodeDocuments(_apiClient, patientId, _pageSize);

                    mergedEpisodeData = (from c in communityEpisodes
                                         join p in parentDocuments
                                         on c.episodeID equals p.contextualId
                                         select new MergedEpisodeData
                                         {
                                             Contextual_ID = p.contextualId,
                                             Community_Episode_ID = c.episodeID,
                                             Community_Episode_Location_ID = c.locationID,
                                             Community_Episode_Location_Description = c.locationDesc,
                                             Parent_CN_Doc_ID = p.documentId,
                                             Patient_ID = p.patientID
                                         }).ToList<MergedEpisodeData>();

                }
            }

            if (mergedEpisodeData.Any())
            {
                Console.WriteLine($"\tActive community episodes found for patient ID: {patientId}\n");

                foreach (MergedEpisodeData item in mergedEpisodeData)
                {
                    Console.WriteLine($"\tPatient ID: {item.Patient_ID}\n\tEpisode ID: {item.Community_Episode_ID}\n\tLocation ID: {item.Community_Episode_Location_ID}\n\tLocation description: {item.Community_Episode_Location_Description}\n\tParent CN Doc ID to use for child documents of this episode: {item.Parent_CN_Doc_ID}\n");
                }
            }
            else
            {
                Console.WriteLine($"\tNo active community episodes were found patient ID: {patientId}\n");
            }

        }


    }

}


