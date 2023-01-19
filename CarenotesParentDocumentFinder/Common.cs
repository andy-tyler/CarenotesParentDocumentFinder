using CarenotesParentDocumentFinder.Data;
using CarenotesParentDocumentFinder.Interfaces;
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
    public class Common : ICommon
    {
        private readonly string _patientIDFilePath;

        private readonly RestClient _restClient;

        static readonly List<int> identifiers = new List<int>();

        private readonly int _objectTypeID;

        private readonly int _pageSize;

        private readonly IApiClient _apiClient;

        public Common(string patientIDFilePath, RestClient restClient, int objectTypeID, int pageSize, IApiClient apiClient)
        {
            this._patientIDFilePath = patientIDFilePath;
            this._restClient = restClient;
            this._objectTypeID = objectTypeID;
            this._pageSize = pageSize;
            this._apiClient = apiClient;
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

            parentDocuments.AddRange(_apiClient.GetParentDocuments(_restClient, patientID, _objectTypeID, _pageSize));

            return parentDocuments;

        }

        public int GetObjectTypeID()
        {
            return _objectTypeID;
        }
    }
}
