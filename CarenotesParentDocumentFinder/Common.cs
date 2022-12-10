using CarenotesParentDocumentFinder.Data;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarenotesParentDocumentFinder
{
    public class Common
    {
        private static string _patientIDFilePath = string.Empty;

        private static RestClient _apiClient;

        static List<int> identifiers;

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
                string contents = File.ReadAllText(_patientIDFilePath);

                identifiers = contents.Split(',').Select(int.Parse).ToList();

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
    }
}
