using CarenotesParentDocumentFinder.Data;
using System.Collections.Generic;

namespace CarenotesParentDocumentFinder.Interfaces
{
    public interface ICommon
    {
        int GetObjectTypeID();
        List<ParentDocument> GetParentDocuments(int patientID);
        List<int> GetPatientIdentifiersFromFile();
    }
}