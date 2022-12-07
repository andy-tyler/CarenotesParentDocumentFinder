using CarenotesParentDocumentFinder.Data;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace CarenotesParentDocumentFinder
{
    internal class Program
    {
        static string _patientIDFilePath = string.Empty;

        static int _pageSize = 100;

        static int _objectTypeID = 50;

        static RestClient _apiClient = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["APIBaseURL"]) { MaxTimeout = -1, UserAgent = "Carenotes Parent Document Finder"});

        static int _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;

        static void Main(string[] args)
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine("Carenotes Parent document reference extract tool v1.0");
            Console.WriteLine("******************************************************");

            if (APIClient.apiIsAvailable(_apiClient))
            {
                try
                {
                    ProcessStartupParameters(args);

                }
                catch (UriFormatException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message.ToString());
                    Console.WriteLine(ex.StackTrace.ToString());
                }
            }
            else
            {
                Console.WriteLine($"The API service at {ConfigurationManager.AppSettings["APIBaseURL"]} is not available, please contact AHC Support for further assistance.");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();

        }

        /// <summary>
        /// Processes startup parameters passed in at point of execution.
        /// Example: /notes -f "c:\drops\randompatientsample.csv"
        /// </summary>
        /// <param name="startupArgs"></param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        static void ProcessStartupParameters(string[] startupArgs)
        {
            if (startupArgs.Length == 0)
            {
                Console.WriteLine("No switch has been specified to set the child document type. Re-run with /? for available options.");
            }

            if (ConfigurationManager.AppSettings["OutputFormat"] == "Tabbed")
            {
                _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;
            }

            if (ConfigurationManager.AppSettings["OutputFormat"] == "Verbose")
            {
                _outputFormat = (int)PicklistValues.OutputMethod.Verbose;
            }


            if (_patientIDFilePath == String.Empty)
            {
                _patientIDFilePath = ConfigurationManager.AppSettings["PatientIDFilePath"].ToString();

            }

            if (!int.TryParse(ConfigurationManager.AppSettings["PageSize"], out _pageSize))
            {
                _pageSize = 100;
            }

            switch (startupArgs[0])
            {
                case "/?":
                    {
                        break;
                    }
                case "/notes":
                    {
                        if (startupArgs[1] == "-f")
                        {
                            if (!String.IsNullOrEmpty(startupArgs[2]))
                            {
                                _patientIDFilePath = startupArgs[2];

                                if (!string.IsNullOrEmpty(_patientIDFilePath))
                                {
                                    ProcessPatientIDFile();
                                }
                                else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");

                                break;
                            }
                            else
                                throw new ArgumentException("File path not specified or invalid. Check file path parameter (-f) is valid.");
                        }
                        if (startupArgs[3] == "-f")
                        {
                            if (!String.IsNullOrEmpty(startupArgs[4]))
                            {
                                _patientIDFilePath = startupArgs[4];

                                if (!string.IsNullOrEmpty(_patientIDFilePath))
                                {
                                    ProcessPatientIDFile();
                                }
                                else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");

                                break;
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

            Console.WriteLine("\nProcessing complete.");

        }

        static void ProcessPatientIDFile()
        {

            Console.WriteLine($"Connecting to API hosted at: {ConfigurationManager.AppSettings["APIBaseURL"]}");

            APIClient.GetSessionToken(_apiClient);

            // 1. Get patient ID's from customer supplied CSV file.

            List<int> patientIdentifiers = GetPatientIdentifiersFromFile();

            // 2. Retrieve parent documents for each patient ID and load into a list.
            
            if (_outputFormat == (int)PicklistValues.OutputMethod.Tabbed)
            {
                Console.WriteLine($"Episode Type\t\tPatient ID\tEpisode ID\tLocation ID\tLocation description\t\tCN Doc ID");
            }

            foreach (int identifier in patientIdentifiers)
            {
                List<ParentDocument> parentDocuments = GetParentDocuments(identifier);

                if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                {
                    Console.WriteLine($"\nRequesting parent documents for patient ID: {identifier}\n");
                }

                ListCommunityEpisodeParentDocuments(parentDocuments, identifier);

                ListInpatientEpisodeParentDocuments(parentDocuments, identifier);

            }

        }

        static List<int> GetPatientIdentifiersFromFile()
        {
            if (_patientIDFilePath != null)
            {
                string contents = File.ReadAllText(_patientIDFilePath);

                List<int> identifiers = contents.Split(',').Select(int.Parse).ToList();

                return identifiers;
            }
            else
            {
                throw new ArgumentNullException("File path for patient identifier CSV is null or missing.");
            }
        }

        static List<ParentDocument> GetParentDocuments(int patientID)
        {

            List<ParentDocument> parentDocuments = APIClient.GetParentDocuments(_apiClient, patientID, _objectTypeID, _pageSize);

            return parentDocuments;

        }

        static void ListCommunityEpisodeParentDocuments(List<ParentDocument> parentDocuments, int patientId)
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

                if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                {
                    Console.WriteLine($"\tActive community episodes found for patient ID: {patientId}\n");

                    foreach (MergedEpisodeData item in mergedEpisodeData)
                    {
                        Console.WriteLine($"\tPatient ID: {item.Patient_ID}\n\tEpisode ID: {item.Community_Episode_ID}\n\tLocation ID: {item.Community_Episode_Location_ID}\n\tLocation description: {item.Community_Episode_Location_Description}\n\tParent CN Doc ID to use for child documents of this episode: {item.Parent_CN_Doc_ID}\n");
                    }
                }
                else
                {
                    foreach (MergedEpisodeData item in mergedEpisodeData)
                    {
                        Console.WriteLine($"Community Episode\t{item.Patient_ID}\t\t{item.Community_Episode_ID}\t\t{item.Community_Episode_Location_ID}\t\t{item.Community_Episode_Location_Description}\t\t\t{item.Parent_CN_Doc_ID}");
                    }
                }

            }
            else
            {
                if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                {
                    Console.WriteLine($"\tNo active community episodes were found patient ID: {patientId}\n");
                }

            }

        }

        static void ListInpatientEpisodeParentDocuments(List<ParentDocument> parentDocuments, int patientId)
        {

            var episodeIds = (from ce in parentDocuments
                              where ce.patientID == patientId
                              where ce.documentTypeID == 75
                              select ce.episodeId).Distinct().ToList();

            List<InpatientEpisode> inpatientEpisodes = new List<InpatientEpisode>();

            List<MergedEpisodeData> mergedEpisodeData = new List<MergedEpisodeData>();

            foreach (int? episodeId in episodeIds)
            {
                if (episodeId != null)
                {

                    inpatientEpisodes = APIClient.GetInpatientEpisodeDocuments(_apiClient, patientId, _pageSize);

                    mergedEpisodeData = (from c in inpatientEpisodes
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

                if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                {
                    Console.WriteLine($"\tActive inpatient episodes found for patient ID: {patientId}\n");

                    foreach (MergedEpisodeData item in mergedEpisodeData)
                    {
                        Console.WriteLine($"\tPatient ID: {item.Patient_ID}\n\tEpisode ID: {item.Community_Episode_ID}\n\tLocation ID: {item.Community_Episode_Location_ID}\n\tLocation description: {item.Community_Episode_Location_Description}\n\tParent CN Doc ID to use for child documents of this episode: {item.Parent_CN_Doc_ID}\n");
                    }
                }
                else
                {
                    foreach (MergedEpisodeData item in mergedEpisodeData)
                    {
                        Console.WriteLine($"Inpatient episode\t{item.Patient_ID}\t\t{item.Community_Episode_ID}\t\t{item.Community_Episode_Location_ID}\t\t{item.Community_Episode_Location_Description}\t\t\t{item.Parent_CN_Doc_ID}");
                    }
                }

            }
            else
            {
                if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                {
                    Console.WriteLine($"\tNo active inpatient episodes were found patient ID: {patientId}\n");
                }
            }

        }
    }

}


