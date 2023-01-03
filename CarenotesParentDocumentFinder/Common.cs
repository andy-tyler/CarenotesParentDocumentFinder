using CarenotesParentDocumentFinder.Data;
using CsvHelper;
using CsvHelper.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CarenotesParentDocumentFinder
{
    public class Common : IDisposable
    {
        private static string _patientIDFilePath = string.Empty;

        private static RestClient _apiClient;

        static List<int> identifiers = new List<int>();

        private static int _objectTypeID;

        private static int _pageSize;
        private bool disposedValue;

        public Common(string patientIDFilePath, RestClient restClient, int objectTypeID, int pageSize)
        {
            _patientIDFilePath = patientIDFilePath;
            _apiClient = restClient;
            _objectTypeID = objectTypeID;
            _pageSize = pageSize;
        }

        public List<int> GetPatientIdentifiersFromFile()
        {
            if (_patientIDFilePath != null)
            {

                var config = new CsvConfiguration(CultureInfo.InvariantCulture){ HasHeaderRecord = false };

                using (var reader = new StreamReader(_patientIDFilePath))
                using (var csv = new CsvReader(reader, config))
                {
                    var records = new List<int>();



                    while (csv.Read())
                    {
                        if (csv.Parser.Record.Length == 1)
                        {
                            identifiers.Add(csv.GetRecord<int>());
                        }
                        else
                        {
                            Console.WriteLine("ERROR: CSV file format invalid. Check that submission file only contains a single patient ID field per row.");
                            return identifiers;
                        }

                    }

                    return identifiers;
                }
            }
            else
            {
                throw new ArgumentNullException("File path for patient identifier CSV is null or missing.");
            }

        }

        public List<ParentDocument> GetParentDocuments(int patientID)
        {

            List<ParentDocument> parentDocuments = new List<ParentDocument>();

            parentDocuments.AddRange(APIClient.GetParentDocuments(_apiClient, patientID, _objectTypeID, _pageSize));

            return parentDocuments;

        }

        public int GetObjectTypeID()
        {
            return _objectTypeID;
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
        // ~Common()
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
