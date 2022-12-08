using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.Helpers;
using CsvHelper;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using static CarenotesParentDocumentFinder.Data.PicklistValues;

namespace CarenotesParentDocumentFinder
{
    internal class Program
    {
        static string _patientIDFilePath = string.Empty;

        static int _pageSize = 100;

        static int _objectTypeID = 50;

        static RestClient _apiClient = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["APIBaseURL"]) { MaxTimeout = -1, UserAgent = "Carenotes Parent Document Finder"});

        static int _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;

        static List<Episode> masterEpisodeList = new List<Episode>();

        static List<int> identifiers;

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

        /// Example: /notes -f "c:\drops\randompatientsample.csv"

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

            Stopwatch processStopWatch = new Stopwatch();

            Console.WriteLine($"Connecting to API hosted at: {ConfigurationManager.AppSettings["APIBaseURL"]}");

            APIClient.GetSessionToken(_apiClient);

            processStopWatch.Start();

            List<int> patientIdentifiers = GetPatientIdentifiersFromFile();

            Console.WriteLine("Requesting data from Carenotes...");

            ProcessParentDocuments(patientIdentifiers);

            Console.SetCursorPosition(0, Console.CursorTop - 1);

            Console.Write(new string(' ', Console.WindowWidth));

            Console.SetCursorPosition(0, Console.CursorTop - 1);

            Console.WriteLine("Download complete.");

            processStopWatch.Stop();

            TimeSpan ts = processStopWatch.Elapsed;

            Console.WriteLine($"\nTotal run time: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds / 10)}");

            DisplayResults();

            WriteResultsToFile();

        }

        private static void ProcessParentDocuments(List<int> patientIdentifiers)
        {
            using (var progress = new ProgressBar())
            {

                int counterPosition = 0;

                foreach (int identifier in patientIdentifiers)
                {

                    progress.Report((double)counterPosition / patientIdentifiers.Count);

                    List<ParentDocument> parentDocuments = GetParentDocuments(identifier);

                    if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                    {
                        Console.WriteLine($"\nRequesting parent documents for patient ID: {identifier}\n");
                    }

                    ListCommunityEpisodeParentDocuments(parentDocuments, identifier);

                    ListInpatientEpisodeParentDocuments(parentDocuments, identifier);

                    counterPosition++;


                }
            }
        }

        static List<int> GetPatientIdentifiersFromFile()
        {
            if (_patientIDFilePath != null)
            {
                string contents = File.ReadAllText(_patientIDFilePath);

                identifiers = contents.Split(',').Select(int.Parse).ToList();

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
                                             Episode_ID = c.episodeID,
                                             Episode_Location_ID = c.locationID,
                                             Episode_Location_Description = c.locationDesc,
                                             Parent_CN_Doc_ID = p.documentId,
                                             Patient_ID = p.patientID,
                                             Service_ID = c.serviceID
                                         }).ToList<MergedEpisodeData>();

                }
            }

            if (mergedEpisodeData.Any())
            {

                foreach(MergedEpisodeData episode in mergedEpisodeData)
                {
                    masterEpisodeList.Add(
                        new Episode 
                        {
                            episodeID = episode.Episode_ID, 
                            episodeTypeID = (int)PicklistValues.EpisodeType.Community, 
                            locationDesc = episode.Episode_Location_Description, 
                            locationID = episode.Episode_Location_ID, 
                            referralStatusID = (int)PicklistValues.ReferralStatus.Accepted,
                            serviceID = episode.Service_ID,
                            cnDocID = episode.Parent_CN_Doc_ID,
                            patientID = episode.Patient_ID
                        });
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
                                             Episode_ID = c.episodeID,
                                             Episode_Location_ID = c.locationID,
                                             Episode_Location_Description = c.locationDesc,
                                             Parent_CN_Doc_ID = p.documentId,
                                             Patient_ID = p.patientID,
                                             Service_ID = c.serviceID
                                         }).ToList<MergedEpisodeData>();

                }
            }

            if (mergedEpisodeData.Any())
            {

                foreach (MergedEpisodeData episode in mergedEpisodeData)
                {
                    masterEpisodeList.Add(
                        new Episode
                        {
                            episodeID = episode.Episode_ID,
                            episodeTypeID = (int)PicklistValues.EpisodeType.Inpatient,
                            locationDesc = episode.Episode_Location_Description,
                            locationID = episode.Episode_Location_ID,
                            referralStatusID = (int)PicklistValues.ReferralStatus.Accepted,
                            serviceID = episode.Service_ID,
                            cnDocID = episode.Parent_CN_Doc_ID,
                            patientID = episode.Patient_ID
                        });
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

        static void DisplayResults()
        {

            if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
            {

                foreach (Episode episode in masterEpisodeList)
                {
                    Console.WriteLine($"Patient ID: {episode.patientID}");
                    Console.WriteLine($"Episode Type: {(EpisodeType)episode.episodeTypeID}");
                    Console.WriteLine($"Episode ID: {episode.episodeID}");
                    Console.WriteLine($"Location ID: {episode.locationID}\n\t");
                    Console.WriteLine($"Location description: {episode.locationDesc}");
                    Console.WriteLine($"Parent CN Doc ID to use for child documents of this episode: {episode.cnDocID}\n");
                }
            }
            else
            {
                Console.WriteLine($"\nEpisode Type\t\tPatient ID\tEpisode ID\tLocation ID\tLocation description\t\t\tCN Doc ID");

                foreach (Episode episode in masterEpisodeList)
                {
                    //Console.WriteLine($"{(EpisodeType)episode.episodeTypeID}\t\t{episode.patientID}\t\t{episode.episodeID}\t\t{episode.locationID}\t\t{episode.locationDesc}\t\t{episode.cnDocID}");
                    Console.WriteLine("{0,-24}{1,-16}{2,-16}{3,-16}{4,-40}{5,-5}", (EpisodeType)episode.episodeTypeID, episode.patientID, episode.episodeID, episode.locationID, episode.locationDesc, episode.cnDocID);
                }
            }

            Console.WriteLine($"\n{identifiers.Count} patients processed, {masterEpisodeList.Count} documents found.");

        }
    
        static void WriteResultsToFile()
        {
            string filestringtime = DateTime.Now.Ticks.ToString();

            using (var writer = new StreamWriter("C:\\drops\\parent-identifiers-" + filestringtime + ".csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(masterEpisodeList);
            }

            Console.WriteLine($"Parent document identifiers written to: parent-identifiers-{filestringtime}.csv");
        }
        
    }

}


