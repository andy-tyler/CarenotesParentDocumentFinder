using CarenotesParentDocumentFinder.Data;
using CsvHelper;
using CsvHelper.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace CarenotesParentDocumentFinder
{
    public class Common
    {
        private readonly string _patientIDFilePath;

        private readonly RestClient _apiClient;

        static List<int> identifiers = new List<int>();

        private readonly int _objectTypeID;

        private readonly int _pageSize;

        public Common(string patientIDFilePath, RestClient restClient, int objectTypeID, int pageSize)
        {
            this._patientIDFilePath = patientIDFilePath;
            this._apiClient = restClient;
            this._objectTypeID = objectTypeID;
            this._pageSize = pageSize;
        }

        public List<int> GetPatientIdentifiersFromFile()
        {
            if (_patientIDFilePath != null)
            {

                var config = new CsvConfiguration(CultureInfo.InvariantCulture){ HasHeaderRecord = false };

                using (var reader = new StreamReader(_patientIDFilePath))
                using (var csv = new CsvReader(reader, config))
                {

                    csv.Read();
                    
                    while (csv.Read())
                    {
                        identifiers.Add(csv.GetRecord<int>());
                    }
                }

                return identifiers;

            }
            else
            {
                throw new ConfigurationErrorsException("File path for patient identifier CSV is null or missing.");
            }

        }

        public List<ParentDocument> GetParentDocuments(int patientID)
        {

            List<ParentDocument> parentDocuments = new List<ParentDocument>();

            parentDocuments.AddRange(ApiClient.GetParentDocuments(_apiClient, patientID, _objectTypeID, _pageSize));

            return parentDocuments;

        }

        public int GetObjectTypeID()
        {
            return _objectTypeID;
        }
    }
}
