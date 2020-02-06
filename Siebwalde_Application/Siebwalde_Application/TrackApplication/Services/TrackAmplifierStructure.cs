using System.Collections.Generic;

namespace Siebwalde_Application
{
    /// <summary>
    /// Helper class to query information about Trackamplifiers
    /// </summary>
    public static class TrackAmplifierStructure
    {
        /// <summary>
        /// Get the TrackAmplifier contents
        /// </summary>
        /// <returns></returns>
        public static List<TrackAmplifierItem> GetTrackAmplifierData(TrackIOHandle trackIOHandle)
        {
            // Creat empty list
            var items = new List<TrackAmplifierItem>();

            var arryLength = trackIOHandle.trackAmpItems.Count;

            for(int i=0; i< arryLength; i++)
            {
                items.Add(trackIOHandle.trackAmpItems[i]);
            }

            return items;
        }
    }
}
