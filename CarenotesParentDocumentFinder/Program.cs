using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.DocumentProcessors;
using CarenotesParentDocumentFinder.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace CarenotesParentDocumentFinder
{
    static internal class Program
    {

        static string _patientIDFilePath = string.Empty;

        static int _pageSize = 100;

        static readonly RestClient _restClient = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["APIBaseURL"]) { Timeout = -1, UserAgent = "Carenotes Parent Document Finder"});

        static int _outputFormat = (int)PicklistValues.OutputMethod.Tabbed;

        static ICommon _common;

        static IApiClient _apiClient;

        static readonly TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();

        static TelemetryClient telemetryClient;

        static void Main(string[] args)
        {
            InitialiseApplicationInsightsTelemetryClient();

            _apiClient = new ApiClient(telemetryClient);

            Console.WriteLine("******************************************************");
            Console.WriteLine("Carenotes Parent document reference extract tool v1.0");
            Console.WriteLine("******************************************************");

            if (args.Length > 0 && args[0] == "/?")
            {
                telemetryClient.TrackEvent("RequestHelp");
                DisplayHelpText();
            }
            else
            {

                if (_apiClient.ApiIsAvailable(_restClient))
                {
                    _apiClient.ApiResponseTime(_restClient);

                    telemetryClient.TrackAvailability(new AvailabilityTelemetry { Name = "Carenotes API", Success = true, Duration = _apiClient.ApiResponseTime(_restClient), RunLocation = "Application" });

                    ProcessingBootstrapper(args);
                }
                else
                {
                    telemetryClient.TrackAvailability(new AvailabilityTelemetry { Name = "Carenotes API", Success = false, RunLocation = "Application" });

                    Console.WriteLine($"The API service at {ConfigurationManager.AppSettings["APIBaseURL"]} is not available, please contact AHC Support for further assistance.");
                }
            }

            Console.WriteLine("\nPress the enter key to exit...");

            ConsoleKeyInfo keyInfo;

            do
            {
                keyInfo = Console.ReadKey(true);
            }
            while (keyInfo.Key != ConsoleKey.Enter);

            telemetryClient.TrackEvent("ApplicationEnd");

            telemetryClient.Flush();
        }

        private static void ProcessingBootstrapper(string[] args)
        {
            try
            {
                if (args.Length == 0)
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

                SetCSVFilePath(args);

                Console.WriteLine($"Connecting to API hosted at: {ConfigurationManager.AppSettings["APIBaseURL"]}");

                RequestApiSessionToken();

                switch (args[0])
                {
                    case "/notes":
                        {
                            telemetryClient.TrackEvent("ProcessNotes");
                            telemetryClient.TrackTrace("Retrieving parent document details for clinical notes.");
                            ProcessNotes();

                            break;
                        }
                    case "/attachments":
                        {
                            telemetryClient.TrackEvent("ProcessAttachments");
                            telemetryClient.TrackTrace("Retrieving parent document details for attachments.");
                            ProcessAttachments();

                            break;
                        }
                    default:
                        {
                            telemetryClient.TrackEvent("InvalidStartRequest");
                            telemetryClient.TrackException(new ArgumentException("Invalid command switch specified on application startup."));
                            throw new ArgumentException("Invalid command switch specified. Re-run with the /? switch for available options.");
                        }
                }

                Console.WriteLine("\nProcessing complete.");
                telemetryClient.TrackTrace("Processing complete.");

            }
            catch (FileNotFoundException ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex, SeverityLevel = SeverityLevel.Error, Timestamp = DateTime.Now });
                Console.WriteLine(ex.Message);
            }
            catch (ArgumentException ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex, SeverityLevel = SeverityLevel.Error, Timestamp = DateTime.Now });
                Console.WriteLine(ex.Message);
            }
            catch (UriFormatException ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex, SeverityLevel = SeverityLevel.Error, Timestamp = DateTime.Now });
                Console.WriteLine(ex.Message);
            }
            catch (WebException ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex, SeverityLevel = SeverityLevel.Error, Timestamp = DateTime.Now });
                Console.WriteLine(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex, SeverityLevel = SeverityLevel.Error, Timestamp = DateTime.Now, Message = "API Authorisation failure." });
                Console.WriteLine(ex.Message);
            }
            catch (AggregateException ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex.Flatten(), SeverityLevel = SeverityLevel.Critical, Timestamp = DateTime.Now, Message = "API Authorisation failure." });
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(new ExceptionTelemetry() { Exception = ex, SeverityLevel = SeverityLevel.Error, Timestamp = DateTime.Now });
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message.ToString());
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }

        private static void InitialiseApplicationInsightsTelemetryClient()
        {
            configuration.ConnectionString = "InstrumentationKey=b433954c-2f41-404b-87a3-f2be44feaed4;IngestionEndpoint=https://uksouth-1.in.applicationinsights.azure.com/;LiveEndpoint=https://uksouth.livediagnostics.monitor.azure.com/";

            telemetryClient = new TelemetryClient(configuration);

            telemetryClient.TrackEvent("ApplicationStart");
            telemetryClient.TrackTrace("Application started");
        }

        private static void ProcessAttachments()
        {
            if (!string.IsNullOrEmpty(_patientIDFilePath))
            {
                int.TryParse(ConfigurationManager.AppSettings["AttachmentsObjectTypeID"], out int _objectTypeID);

                if (_objectTypeID != -1)
                {

                    if (RecursiveSearchEnabled())
                    {
                        ProcessRecursiveFileList(_objectTypeID);
                    }
                    else
                    {
                        _common = new Common(_patientIDFilePath, _restClient, _objectTypeID, _pageSize, _apiClient);

                        ProcessPatientIDFile();

                    }
                }
                else
                {
                    throw new ArgumentException("Attachment UDF object type ID missing or invalid. Check value specified in configuration file is specified and valid.");
                }

            }
            else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");
        }

        private static void ProcessNotes()
        {
            if (!string.IsNullOrEmpty(_patientIDFilePath))
            {

                int _objectTypeID = 50;

                if (RecursiveSearchEnabled())
                {
                    ProcessRecursiveFileList(_objectTypeID);
                }
                else
                {
                    _common = new Common(_patientIDFilePath, _restClient, _objectTypeID, _pageSize, _apiClient);

                    Console.WriteLine($"\nProcessing file {_patientIDFilePath}.");

                    ProcessPatientIDFile();

                }

            }
            else throw new FileNotFoundException("CSV file not specified or not found. Check file path is set in configuration file or specify a command line parameter.");
        }

        static void ProcessPatientIDFile()
        {

            Stopwatch processStopWatch = new Stopwatch();

            processStopWatch.Start();

            DateTime processStarted = DateTime.UtcNow;

            RequestApiSessionToken();

            List<int> patientIdentifiers = _common.GetPatientIdentifiersFromFile();

            var sample = new MetricTelemetry
            {
                Name = "IdentiferFileIDCount",
                Sum = patientIdentifiers.Count
            };

            telemetryClient.TrackMetric(sample);

            Console.WriteLine("Requesting data from Carenotes...");

            if (_common.GetObjectTypeID() == 50)
            {
                // Get available episode documents that can parent a clinical note document.
                using (IEpisodeDocumentProcessor episodeProcessor = new EpisodeDocumentProcessor(_restClient, _outputFormat, _pageSize, patientIdentifiers, _common, _apiClient, telemetryClient))
                {
                    episodeProcessor.ProcessParentDocumentEpisodes(patientIdentifiers);
                }
            }


            Console.WriteLine("Download complete.");

            processStopWatch.Stop();

            TimeSpan ts = processStopWatch.Elapsed;

            Console.WriteLine($"\nTotal run time: {String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds / 10)}");

            telemetryClient.TrackDependency("StartTime", "Duration", "Result", processStarted, processStopWatch.Elapsed, true);

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

        static List<FileInfo> GetFileList(string filePath)
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

        static void RequestApiSessionToken()
        {

            if (_apiClient.SessionTokenExists()) return;

            _apiClient.GetSessionToken(_restClient);

        }

        static bool RecursiveSearchEnabled()
        {
            bool.TryParse(ConfigurationManager.AppSettings["RecursiveSearch"], out bool recursiveSearchEnabled);

            if (recursiveSearchEnabled && !_patientIDFilePath.EndsWith(".csv"))
            {
                return true;
            }

            return false;
        }

        static void DisplayHelpText()
        {

            Console.WriteLine("This utility requires a command line argument to define what document type should be processed.\n");
            Console.WriteLine("This version supports the following document types:\n");
            Console.WriteLine("\t/notes\n\t/attachments\n");
            Console.WriteLine("If no additional parameters are defined then the file path defined in the configuration file will be used.\n");
            Console.WriteLine("Example: CarenotesParentDocumentFinder.exe /notes\n");
            Console.WriteLine("An optional parameter can be included to define the file path to override the configuration file setting:\n");
            Console.WriteLine("Example: CarenotesParentDocumentFinder.exe /notes -f \"c:\\drops\\patientidentifiers.csv\"");
        }

        static void ProcessRecursiveFileList(int _objectTypeID)
        {
            List<FileInfo> fileList = GetFileList(_patientIDFilePath);

            var sample = new MetricTelemetry
            {
                Name = "ProcessingBatchFileCount",
                Sum = fileList.Count
            };

            telemetryClient.TrackMetric(sample);

            for (int i = 0; i < fileList.Count; i++)
            {
                _patientIDFilePath = fileList[i].FullName;

                _common = new Common(_patientIDFilePath, _restClient, _objectTypeID, _pageSize, _apiClient);

                telemetryClient.TrackEvent("ProcessFile");

                Console.WriteLine($"\nProcessing file {i + 1} of {fileList.Count} - {fileList[i].FullName}.");

                ProcessPatientIDFile();
            }
        }

    }

}


