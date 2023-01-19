using System;
using System.Collections.Generic;

namespace CarenotesParentDocumentFinder.Interfaces
{
    public interface IEpisodeDocumentProcessor : IDisposable
    {
        void ProcessParentDocumentEpisodes(List<int> patientIdentifiers);
    }
}