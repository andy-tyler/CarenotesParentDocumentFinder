using CarenotesParentDocumentFinder.Data;
using CsvHelper;
using CsvHelper.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CarenotesParentDocumentFinder.Data.PicklistValues;

namespace CarenotesParentDocumentFinder
{
    public class Common
    {
        private static string _patientIDFilePath = string.Empty;

        private static RestClient _apiClient;

        static List<int> identifiers = new List<int>();

        private static int _objectTypeID;

        private static int _pageSize;

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
                throw new ArgumentNullException("File path for patient identifier CSV is null or missing.");
            }

        }

        public List<ParentDocument> GetParentDocuments(int patientID)
        {

            List<ParentDocument> parentDocuments = APIClient.GetParentDocuments(_apiClient, patientID, _objectTypeID, _pageSize);

            return parentDocuments;

        }

        public int GetObjectTypeID()
        {
            return _objectTypeID;
        }
    }
}
