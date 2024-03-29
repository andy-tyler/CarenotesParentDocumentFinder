﻿using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.Helpers;
using CarenotesParentDocumentFinder.Interfaces;
using CsvHelper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CarenotesParentDocumentFinder.Data.PicklistValues;

namespace CarenotesParentDocumentFinder.DocumentProcessors
{
    public class EpisodeDocumentProcessor : IEpisodeDocumentProcessor
    {
        readonly int _outputFormat;
        readonly int _pageSize;
        readonly List<Episode> masterEpisodeList = new List<Episode>();
        private readonly RestClient _restClient;
        readonly List<int> _identifiers;
        private bool disposedValue;
        readonly private ICommon _common;
        private readonly IApiClient _apiClient;
        private readonly TelemetryClient _telemetryClient;

        public EpisodeDocumentProcessor(RestClient restClient, int outputFormat, int pageSize, List<int> identifiers, ICommon common, IApiClient apiClient, TelemetryClient telemetryClient)
        {
            this._restClient = restClient;
            this._outputFormat = outputFormat;
            this._pageSize = pageSize;
            this._identifiers = identifiers;
            this._common = common;
            this._apiClient = apiClient;
            this._telemetryClient = telemetryClient;
        }

        public void ProcessParentDocumentEpisodes(List<int> patientIdentifiers)
        {
            using (var progress = new ProgressBar())
            {
                int counterPosition = 0;

                var parallelOptions = new ParallelOptions{ MaxDegreeOfParallelism = System.Environment.ProcessorCount};

                List<Exception> ae = new List<Exception>();

                try
                {
                    Parallel.ForEach(patientIdentifiers, parallelOptions, identifier =>
                {
                    progress.Report((double)counterPosition / patientIdentifiers.Count);

                    List<ParentDocument> parentDocuments = _common.GetParentDocuments(identifier);

                    if (parentDocuments.Count > 0)
                    {

                        ListCommunityEpisodeParentDocuments(parentDocuments, identifier);

                        ListInpatientEpisodeParentDocuments(parentDocuments, identifier);

                        ListTeamEpisodeParentDocuments(parentDocuments, identifier);
                    }
                    counterPosition++;
                }
                );
                }
                catch (Exception ex)
                {
                    ae.Add(ex);
                }

                if (ae.Count > 0)
                {
                    throw new AggregateException("Errors encountered when processing community episodes.", ae);
                }

            }

            Console.SetCursorPosition(0, Console.CursorTop - 1);

            Console.Write(new string(' ', Console.WindowWidth));

            Console.SetCursorPosition(0, Console.CursorTop - 1);

            DisplayEpisodeResults();

            WriteEpisodeResultsToFile();
        }

        private void ListCommunityEpisodeParentDocuments(List<ParentDocument> parentDocuments, int patientId)
        {

            var episodeIds = (from ce in parentDocuments
                              where ce.PatientID == patientId
                              where ce.DocumentTypeID == 52
                              select ce.EpisodeId).Distinct().ToList();

            List<CommunityEpisode> communityEpisodes;

            List<MergedEpisodeData> mergedCommunityEpisodeData = new List<MergedEpisodeData>();

            var parallelOptions = new ParallelOptions{ MaxDegreeOfParallelism = System.Environment.ProcessorCount};

            List<Exception> ae = new List<Exception>();

            try
            {
                Parallel.ForEach(episodeIds, parallelOptions, episodeId =>
                {
                    if (episodeId != null)
                    {

                        communityEpisodes = _apiClient.GetCommunityEpisodeDocuments(_restClient, patientId, _pageSize);

                        mergedCommunityEpisodeData = GetCommunityEpisodeData(parentDocuments, communityEpisodes);

                    }

                });
            }
            catch (Exception ex)
            {
                ae.Add(ex);
            }

            if (ae.Count > 0)
            {
                throw new AggregateException("Errors encountered when processing community episodes.", ae);
            }



            if (mergedCommunityEpisodeData.Any())
            {

                foreach (MergedEpisodeData episode in mergedCommunityEpisodeData)
                {
                    AddCommunityEpisodes(episode);
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

        private void AddCommunityEpisodes(MergedEpisodeData episode)
        {
            masterEpisodeList.Add(
                new Episode
                {
                    EpisodeID = episode.Episode_ID,
                    EpisodeTypeID = (int)PicklistValues.EpisodeType.Community,
                    LocationDesc = episode.Episode_Location_Description,
                    LocationID = episode.Episode_Location_ID,
                    ReferralStatusID = (int)PicklistValues.ReferralStatus.Accepted,
                    ServiceID = episode.Service_ID,
                    CnDocID = episode.Parent_CN_Doc_ID,
                    PatientID = episode.Patient_ID,
                    ReferralID = episode.Referral_ID
                });
        }

        private static List<MergedEpisodeData> GetCommunityEpisodeData(List<ParentDocument> parentDocuments, List<CommunityEpisode> communityEpisodes)
        {
            return (from c in communityEpisodes
                    join p in parentDocuments
                    on c.EpisodeID equals p.ContextualId
                    select new MergedEpisodeData
                    {
                        Contextual_ID = p.ContextualId,
                        Episode_ID = c.EpisodeID,
                        Episode_Location_ID = c.LocationID,
                        Episode_Location_Description = c.LocationDesc,
                        Parent_CN_Doc_ID = p.DocumentId,
                        Patient_ID = p.PatientID,
                        Service_ID = c.ServiceID,
                        Referral_ID = p.ReferralId
                    }).ToList<MergedEpisodeData>();
        }

        private void ListInpatientEpisodeParentDocuments(List<ParentDocument> parentDocuments, int patientId)
        {

            var episodeIds = (from ce in parentDocuments
                              where ce.PatientID == patientId
                              where ce.DocumentTypeID == 75
                              select ce.EpisodeId).Distinct().ToList();

            List<InpatientEpisode> inpatientEpisodes = new List<InpatientEpisode>();

            List<MergedEpisodeData> mergedInpatientEpisodeData = new List<MergedEpisodeData>();

            var parallelOptions = new ParallelOptions{ MaxDegreeOfParallelism = System.Environment.ProcessorCount};

            List<Exception> ae = new List<Exception>();

            try
            {

                Parallel.ForEach(episodeIds, parallelOptions, episodeId =>
            {
                if (episodeId != null)
                {

                    inpatientEpisodes = _apiClient.GetInpatientEpisodeDocuments(_restClient, patientId, _pageSize);

                    mergedInpatientEpisodeData = (from c in inpatientEpisodes
                                                  join p in parentDocuments
                                                  on c.EpisodeID equals p.ContextualId
                                                  select new MergedEpisodeData
                                                  {
                                                      Contextual_ID = p.ContextualId,
                                                      Episode_ID = c.EpisodeID,
                                                      Episode_Location_ID = c.LocationID,
                                                      Episode_Location_Description = c.LocationDesc,
                                                      Parent_CN_Doc_ID = p.DocumentId,
                                                      Patient_ID = p.PatientID,
                                                      Service_ID = c.ServiceID,
                                                      Referral_ID = p.ReferralId
                                                  }).ToList<MergedEpisodeData>();

                }

            });

            }
            catch (Exception ex)
            {
                ae.Add(ex);
            }

            if (ae.Count > 0)
            {
                throw new AggregateException("Errors encountered when processing community episodes.", ae);
            }

            if (mergedInpatientEpisodeData.Any())
            {

                foreach (MergedEpisodeData episode in mergedInpatientEpisodeData)
                {
                    masterEpisodeList.Add(
                        new Episode
                        {
                            EpisodeID = episode.Episode_ID,
                            EpisodeTypeID = (int)PicklistValues.EpisodeType.Inpatient,
                            LocationDesc = episode.Episode_Location_Description,
                            LocationID = episode.Episode_Location_ID,
                            ReferralStatusID = (int)PicklistValues.ReferralStatus.Accepted,
                            ServiceID = episode.Service_ID,
                            CnDocID = episode.Parent_CN_Doc_ID,
                            PatientID = episode.Patient_ID,
                            ReferralID = episode.Referral_ID
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

        private void ListTeamEpisodeParentDocuments(List<ParentDocument> parentDocuments, int patientId)
        {

            var episodeIds = (from ce in parentDocuments
                              where ce.PatientID == patientId
                              where ce.DocumentTypeID == 110
                              select ce.EpisodeId).Distinct().ToList();

            List<TeamEpisode> teamEpisodes = new List<TeamEpisode>();

            List<MergedEpisodeData> mergedTeamEpisodeData = new List<MergedEpisodeData>();

            var parallelOptions = new ParallelOptions{ MaxDegreeOfParallelism = System.Environment.ProcessorCount};

            List<Exception> ae = new List<Exception>();

            try
            {

                Parallel.ForEach(episodeIds, parallelOptions, episodeId =>
            {
                if (episodeId != null)
                {

                    teamEpisodes = _apiClient.GetTeamEpisodeDocuments(_restClient, patientId, _pageSize);

                    mergedTeamEpisodeData = (from c in teamEpisodes
                                             join p in parentDocuments
                                             on c.EpisodeID equals p.ContextualId
                                             select new MergedEpisodeData
                                             {
                                                 Contextual_ID = p.ContextualId,
                                                 Episode_ID = c.EpisodeID,
                                                 Episode_Location_ID = c.LocationID,
                                                 Episode_Location_Description = c.LocationDesc,
                                                 Parent_CN_Doc_ID = p.DocumentId,
                                                 Patient_ID = p.PatientID,
                                                 Service_ID = c.ServiceID,
                                                 Referral_ID = (int)p.ReferralId
                                             }).ToList<MergedEpisodeData>();

                }

            });

            }
            catch (Exception ex)
            {
                ae.Add(ex);
            }

            if (ae.Count > 0)
            {
                throw new AggregateException("Errors encountered when processing community episodes.", ae);
            }

            if (mergedTeamEpisodeData.Any())
            {

                foreach (MergedEpisodeData episode in mergedTeamEpisodeData)
                {
                    masterEpisodeList.Add(
                        new Episode
                        {
                            EpisodeID = episode.Episode_ID,
                            EpisodeTypeID = (int)PicklistValues.EpisodeType.Team,
                            LocationDesc = episode.Episode_Location_Description,
                            LocationID = episode.Episode_Location_ID,
                            ReferralStatusID = (int)PicklistValues.ReferralStatus.Accepted,
                            ServiceID = episode.Service_ID,
                            CnDocID = episode.Parent_CN_Doc_ID,
                            PatientID = episode.Patient_ID,
                            ReferralID = episode.Referral_ID
                        });
                }


            }
            else
            {
                if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                {
                    Console.WriteLine($"\tNo active team episodes were found patient ID: {patientId}\n");
                }
            }
        }

        private void DisplayEpisodeResults()
        {

            if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
            {

                foreach (Episode episode in masterEpisodeList)
                {
                    Console.WriteLine($"Patient ID: {episode.PatientID}");
                    Console.WriteLine($"Referral ID: {episode.ReferralID}");
                    Console.WriteLine($"Episode Type: {(EpisodeType)episode.EpisodeTypeID}");
                    Console.WriteLine($"Episode ID: {episode.EpisodeID}");
                    Console.WriteLine($"Location ID: {episode.LocationID}\n\t");
                    Console.WriteLine($"Location description: {episode.LocationDesc}");
                    Console.WriteLine($"Parent CN Doc ID to use for child documents of this episode: {episode.CnDocID}\n");
                }
            }
            else
            {
                Console.WriteLine($"\nEpisode Type\t\tPatient ID\tReferral ID\tEpisode ID\tLocation ID\tLocation description\t\t\tCN Doc ID");

                foreach (Episode episode in masterEpisodeList)
                {
                    Console.WriteLine($"{(EpisodeType)episode.EpisodeTypeID,-24}{episode.PatientID,-16}{episode.ReferralID,-16}{episode.EpisodeID,-16}{episode.LocationID,-16}{episode.LocationDesc,-40}{episode.CnDocID,-5}");
                }
            }

            var identifiersSample = new MetricTelemetry();
            identifiersSample.Name = "PatientsProcessed";
            identifiersSample.Sum = _identifiers.Count;
            _telemetryClient.TrackMetric(identifiersSample);

            var sample = new MetricTelemetry();
            sample.Name = "DocumentsFound";
            sample.Sum = masterEpisodeList.Count;
            _telemetryClient.TrackMetric(sample);

            Console.WriteLine($"\n{_identifiers.Count} patients processed, {masterEpisodeList.Count} documents found.");

        }

        private void WriteEpisodeResultsToFile()
        {
            string filestringtime = DateTime.Now.Ticks.ToString();

            DirectoryInfo di = Directory.CreateDirectory(ConfigurationManager.AppSettings["ProcessingResultsPath"]);

            using (var writer = new StreamWriter(di.FullName + "\\parent-identifiers-" + filestringtime + ".csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(masterEpisodeList);
            }

            Console.WriteLine($"\nParent document identifiers written to: " + di.FullName + "\\parent-identifiers-" + filestringtime + ".csv");

            _telemetryClient.TrackEvent("WriteResultsFile");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
