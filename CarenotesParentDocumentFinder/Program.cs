using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.DocumentProcessors;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CarenotesParentDocumentFinder
{
    static internal class Program
    {
        static string _patientIDFilePath = string.Empty;

        static int _pageSize = 100;

        static RestClient _apiClient = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["APIBaseURL"]) { MaxTimeout = -1, UserAgent = "Carenotes Parent Document Finder"});

        static int _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;

        static Common _common;

        static void Main(string[] args)
        {

            Console.WriteLine("******************************************************");
            Console.WriteLine("Carenotes Parent document reference extract tool v1.0");
            Console.WriteLine("******************************************************");

            if (ApiClient.apiIsAvailable(_apiClient))
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

            Console.WriteLine("\nPress the enter key to exit...");

            ConsoleKeyInfo keyInfo;

            do
            {
                keyInfo = Console.ReadKey(true);
            }
            while (keyInfo.Key != ConsoleKey.Enter);

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
                default:
                    _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;
                    break;
            }

            if (!int.TryParse(ConfigurationManager.AppSettings["PageSize"], out _pageSize)) _pageSize = 100;

            SetCSVFilePath(startupArgs);

            Console.WriteLine($"Connecting to API hosted at: {ConfigurationManager.AppSettings["APIBaseURL"]}");

            switch (startupArgs[0])
            {
                case "/?":
                    {
                        break;
                    }
                case "/notes":
                    {

                        // TODO: Add to other document types...

                        if (!string.IsNullOrEmpty(_patientIDFilePath))
                        {

                            int _objectTypeID = 50;

                            bool.TryParse(ConfigurationManager.AppSettings["RecursiveSearch"], out bool recursiveSearchEnabled);

                            if (recursiveSearchEnabled && !_patientIDFilePath.EndsWith(".csv"))
                            {
                                List<FileInfo> fileList = getFileList(_patientIDFilePath);

                                for(int i = 0; i< fileList.Count; i++)
                                {
                                    _patientIDFilePath = fileList[i].FullName;

                                    _common = new Common(_patientIDFilePath, _apiClient, _objectTypeID, _pageSize);

                                    Console.WriteLine($"\nProcessing file {i+1} of {fileList.Count} - {fileList[i].FullName}.");

                                    ProcessPatientIDFile();
                                }
                            }
                            else
                            {
                                _common = new Common(_patientIDFilePath, _apiClient, _objectTypeID, _pageSize);

                                Console.WriteLine($"\nProcessing file {_patientIDFilePath}.");

                                ProcessPatientIDFile();
                            }

                        }
                        else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");

                        break;
                    }
                case "/attachments":
                    {


                        if (!string.IsNullOrEmpty(_patientIDFilePath))
                        {
                            int.TryParse(ConfigurationManager.AppSettings["AttachmentsObjectTypeID"], out int _objectTypeID);

                            if (_objectTypeID != -1)
                            {

                                _common = new Common(_patientIDFilePath, _apiClient, _objectTypeID, _pageSize);

                                ProcessPatientIDFile();
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

            Console.WriteLine("\nProcessing complete.");

        }

        static void ProcessPatientIDFile()
        {

            Stopwatch processStopWatch = new Stopwatch();

            loginToApi();

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

            if(string.IsNullOrEmpty(_patientIDFilePath))
            {
                throw new ArgumentException("File path flag not set.");
            }


            bool.TryParse(ConfigurationManager.AppSettings["RecursiveSearch"], out bool recursiveSearchEnabled);

            if (recursiveSearchEnabled && !_patientIDFilePath.EndsWith(".csv"))
            {
                if (Directory.Exists(_patientIDFilePath))
                {
                    return;
                }
                    
                throw new DirectoryNotFoundException("Specified directory does not exist or is incorrect.");
            }


            if (File.Exists(_patientIDFilePath))
            {
                return;
            }
                
            throw new FileNotFoundException("Specified CSV file does not exist or is incorrect.");



        }

        static List<FileInfo> getFileList(string filePath)
        {
            List<FileInfo> fileList = new List<FileInfo>();

            DirectoryInfo directoryInfo = new DirectoryInfo(filePath);

            IEnumerable<FileInfo> files = directoryInfo.GetFiles();

            IEnumerable<FileInfo> fileInfoQuery = from file in files
                                                  where file.Extension == ".csv"
                                                  orderby file.Name
                                                  select file;

            foreach (FileInfo fileInfo in fileInfoQuery) 
                fileList.Add(fileInfo);

            return fileList;
        }

        static bool loginToApi()
        {
            if (ApiClient.SessionTokenExists()) return true;

            ApiClient.GetSessionToken(_apiClient);
            
            return true;

        }

    }

}


