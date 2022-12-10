using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.Helpers;
using CsvHelper;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static CarenotesParentDocumentFinder.Data.PicklistValues;

namespace CarenotesParentDocumentFinder.DocumentProcessors
{
    public class EpisodeDocumentProcessor : IDisposable
    {
        static int _outputFormat = 1;
        static int _pageSize = 100;
        static List<Episode> masterEpisodeList = new List<Episode>();
        private static RestClient _apiClient;
        static List<int> _identifiers;
        private bool disposedValue;
        private static Common _common;

        public EpisodeDocumentProcessor(RestClient restClient, int outputFormat, int pageSize, List<int> identifiers, Common common)
        {
            _apiClient = restClient;
            _outputFormat = outputFormat;
            _pageSize = pageSize;
            _identifiers = identifiers;
            _common = common;
        }

        public void ProcessParentDocumentEpisodes(List<int> patientIdentifiers)
        {
            using (var progress = new ProgressBar())
            {

                int counterPosition = 0;

                foreach (int identifier in patientIdentifiers)
                {

                    progress.Report((double)counterPosition / patientIdentifiers.Count);

                    List<ParentDocument> parentDocuments = _common.GetParentDocuments(identifier);

                    if (_outputFormat == (int)PicklistValues.OutputMethod.Verbose)
                    {
                        Console.WriteLine($"\nRequesting parent documents for patient ID: {identifier}\n");
                    }

                    ListCommunityEpisodeParentDocuments(parentDocuments, identifier);

                    ListInpatientEpisodeParentDocuments(parentDocuments, identifier);

                    counterPosition++;


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

                foreach (MergedEpisodeData episode in mergedEpisodeData)
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

        private void ListInpatientEpisodeParentDocuments(List<ParentDocument> parentDocuments, int patientId)
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

        private void DisplayEpisodeResults()
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

            Console.WriteLine($"\n{_identifiers.Count} patients processed, {masterEpisodeList.Count} documents found.");

        }

        private void WriteEpisodeResultsToFile()
        {
            string filestringtime = DateTime.Now.Ticks.ToString();

            using (var writer = new StreamWriter("C:\\drops\\parent-identifiers-" + filestringtime + ".csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(masterEpisodeList);
            }

            Console.WriteLine($"Parent document identifiers written to: parent-identifiers-{filestringtime}.csv");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~EpisodeDocumentProcessor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
