using CarenotesParentDocumentFinder;
using CarenotesParentDocumentFinder.Data;
using System;
using System.Collections.Generic;

namespace TestProject
{
    internal class MockCommon : ICommon
    {
        public int GetObjectTypeID()
        {
            Random rnd = new Random();
            return rnd.Next();
        }

        public List<ParentDocument> GetParentDocuments(int patientID)
        {
            return new List<ParentDocument>()
            {
                new ParentDocument()
                {
                    Active = true, ContextualId = 0, DocumentId = 0, DocumentSummary = "", DocumentTypeDescription = "", DocumentTypeID = 0, EpisodeId = 0, PatientID = 0, ReferralId = 0
                }};
        }

        public List<int> GetPatientIdentifiersFromFile()
        {
            return new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        }
    }
}
