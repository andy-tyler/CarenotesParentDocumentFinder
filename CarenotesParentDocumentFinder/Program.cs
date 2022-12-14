using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.DocumentProcessors;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace CarenotesParentDocumentFinder
{
    internal class Program
    {
        static string _patientIDFilePath = string.Empty;

        static int _pageSize = 100;

        static int _objectTypeID = -1;

        static RestClient _apiClient = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["APIBaseURL"]) { MaxTimeout = -1, UserAgent = "Carenotes Parent Document Finder"});

        static int _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;

        static Common _common;

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
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (UriFormatException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (FormatException ex)
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
                throw new ArgumentException("No switch has been specified to set the child document type. Re-run with /? for available options.");
            }

            switch (ConfigurationManager.AppSettings["OutputFormat"])
            {
                case "Verbose":
                    _outputFormat = (int)PicklistValues.OutputMethod.Verbose;
                    break;
                case "Tabbed":
                default:
                    _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;
                    break;
            }

            if (!int.TryParse(ConfigurationManager.AppSettings["PageSize"], out _pageSize)) _pageSize = 100;

            int recursiveSwitchIndex = Array.IndexOf(startupArgs,"-r");

            if (recursiveSwitchIndex != -1)
            {
                if (Directory.Exists(startupArgs[recursiveSwitchIndex + 1]))
                {
                    Console.WriteLine($"Processing all CSV files in directory {startupArgs[recursiveSwitchIndex + 1]}\n");

                    string[] submissionFileCandidates = Directory.GetFiles(startupArgs[recursiveSwitchIndex + 1], "*.csv\n");


                    Stopwatch processStopWatch = new Stopwatch();

                    Console.WriteLine($"Connecting to API hosted at: {ConfigurationManager.AppSettings["APIBaseURL"]}");

                    APIClient.GetSessionToken(_apiClient);

                    processStopWatch.Start();

                    Console.WriteLine($"{submissionFileCandidates.Length} files found for processing in {startupArgs[recursiveSwitchIndex + 1]}");

                    foreach (string submissionFile in submissionFileCandidates)
                    {
                        Console.WriteLine($"\nProcessing file {submissionFile}");

                        _objectTypeID = 50;

                        using (Common cmn = new Common(submissionFile, _apiClient, _objectTypeID, _pageSize))
                        {
                            List<int> patientIdentifiers = cmn.GetPatientIdentifiersFromFile();

                            if (cmn.GetObjectTypeID() == 50 && patientIdentifiers.Count > 0)
                            {
                                // Get available episode documents that can parent a clinical note document.
                                using (EpisodeDocumentProcessor episodeProcessor = new EpisodeDocumentProcessor(_apiClient, _outputFormat, _pageSize, patientIdentifiers, cmn))
                                {
                                    episodeProcessor.ProcessParentDocumentEpisodes(patientIdentifiers);
                                }
                            }
                        }

                    }

                    processStopWatch.Stop();

                    TimeSpan ts = processStopWatch.Elapsed;

                    if (processStopWatch.Elapsed.TotalSeconds > 1)
                        Console.WriteLine($"\nTotal run time: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds / 10)}");

                }
                else
                {
                    throw new DirectoryNotFoundException("Directory not found to process CSV files.");
                }


            }
            else
            {
                SetCSVFilePath(startupArgs);

                switch (startupArgs[0])
                {
                    case "/?":
                        {
                            break;
                        }
                    case "/notes":
                        {

                            if (!string.IsNullOrEmpty(_patientIDFilePath))
                            {

                                _objectTypeID = 50;

                                _common = new Common(_patientIDFilePath, _apiClient, _objectTypeID, _pageSize);

                            }
                            else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");

                            break;
                        }
                    case "/attachments":
                        {

                            if (!string.IsNullOrEmpty(_patientIDFilePath))
                            {
                                int.TryParse(ConfigurationManager.AppSettings["AttachmentsObjectTypeID"], out _objectTypeID);

                                if (_objectTypeID != -1)
                                {

                                    _common = new Common(_patientIDFilePath, _apiClient, _objectTypeID, _pageSize);

                                }
                                else
                                {
                                    throw new ArgumentException("Attachment UDF object type ID missing or invalid. Check value specified in configuration file is specified and valid.");
                                }

                            }
                            else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");

                            break;
                        }
                    default:
                        {
                            throw new ArgumentException("Invalid command switch specified. Re-run with the /? switch for available options.");
                        }

                }

                ProcessPatientIDFile();
            }

            Console.WriteLine("\nProcessing complete.");

        }

        static void ProcessPatientIDFile()
        {

            Stopwatch processStopWatch = new Stopwatch();

            Console.WriteLine($"Connecting to API hosted at: {ConfigurationManager.AppSettings["APIBaseURL"]}");

            APIClient.GetSessionToken(_apiClient);

            processStopWatch.Start();

            List<int> patientIdentifiers = _common.GetPatientIdentifiersFromFile();

            Console.WriteLine("Requesting data from Carenotes...");

            if (_common.GetObjectTypeID() == 50)
            {
                // Get available episode documents that can parent a clinical note document.
                using (EpisodeDocumentProcessor episodeProcessor = new EpisodeDocumentProcessor(_apiClient, _outputFormat, _pageSize, patientIdentifiers, _common))
                {
                    episodeProcessor.ProcessParentDocumentEpisodes(patientIdentifiers);
                }
            }


            Console.WriteLine("Download complete.");

            processStopWatch.Stop();

            TimeSpan ts = processStopWatch.Elapsed;

            Console.WriteLine($"\nTotal run time: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds / 10)}");

        }

        static void SetCSVFilePath(string[] startupArgs)
        {
            _patientIDFilePath = ConfigurationManager.AppSettings["PatientIDFilePath"].ToString();

            int fileFlagSet = Array.IndexOf(startupArgs,"-f");

            if (fileFlagSet != -1)
            {

                if (!String.IsNullOrEmpty(startupArgs[fileFlagSet]))
                    _patientIDFilePath = startupArgs[fileFlagSet + 1];

                if (String.IsNullOrEmpty(_patientIDFilePath))
                {
                    throw new ArgumentException("File path not specified or invalid. Check file path parameter (-f) is valid.");

                }
            }

            if (string.IsNullOrEmpty(_patientIDFilePath))
            {
                throw new ArgumentException("File path flag not set.");
            }

            if (!File.Exists(_patientIDFilePath))
                throw new FileNotFoundException("Specified CSV file does not exist or is incorrect.");

        }

    }

}


