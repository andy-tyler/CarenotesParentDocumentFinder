using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.DocumentProcessors;
using CsvHelper.Configuration.Attributes;
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

        static int _objectTypeID = 50;

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
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
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
                throw new ArgumentException("No switch has been specified to set the child document type. Re-run with /? for available options.");
            }

            switch(ConfigurationManager.AppSettings["OutputFormat"])
            {
                case "Verbose":
                    _outputFormat = (int)PicklistValues.OutputMethod.Verbose;
                    break;
                case "Tabbed":
                default:
                    _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;
                    break;
            }


            if (_patientIDFilePath == String.Empty)
                _patientIDFilePath = ConfigurationManager.AppSettings["PatientIDFilePath"].ToString();

            if (!int.TryParse(ConfigurationManager.AppSettings["PageSize"], out _pageSize))
                _pageSize = 100;

            
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
                                _patientIDFilePath = startupArgs[2];
                            else
                                throw new ArgumentException("File path not specified or invalid. Check file path parameter (-f) is valid.");
                        }
                        else if (startupArgs[3] == "-f")
                        {
                            if (!String.IsNullOrEmpty(startupArgs[4]))
                                _patientIDFilePath = startupArgs[4];
                            else
                                throw new ArgumentException("File path not specified or invalid. Check file path parameter (-f) is valid.");
                        }

                        if (!string.IsNullOrEmpty(_patientIDFilePath))
                        {
                            _common = new Common(_patientIDFilePath, _apiClient, _objectTypeID, _pageSize);

                            ProcessPatientIDFile();
                        }
                        else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");

                        break;
                    }
                case "/attachments":
                    {

                        if (!int.TryParse(ConfigurationManager.AppSettings["AttachmentsObjectTypeID"], out _objectTypeID))
                            throw new ArgumentException("Attachments object type ID is missing or invalid. Check the AttachmentsObjectTypeID setting in the configuration file.");

                        if (startupArgs[1] == "-f")
                        {
                            if (!String.IsNullOrEmpty(startupArgs[2]))
                            {
                                _patientIDFilePath = startupArgs[2];

                                if (!string.IsNullOrEmpty(_patientIDFilePath))
                                {
                                    Console.WriteLine("Retrieving parent document data for the UDF attachment form.");

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

            List<int> patientIdentifiers = _common.GetPatientIdentifiersFromFile();

            Console.WriteLine("Requesting data from Carenotes...");

            using (EpisodeDocumentProcessor episodeProcessor = new EpisodeDocumentProcessor(_apiClient, _outputFormat, _pageSize, patientIdentifiers, _common))
            {
                episodeProcessor.ProcessParentDocumentEpisodes(patientIdentifiers);
            }

            Console.WriteLine("Download complete.");

            processStopWatch.Stop();

            TimeSpan ts = processStopWatch.Elapsed;

            Console.WriteLine($"\nTotal run time: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds / 10)}");


        }




    }

}


