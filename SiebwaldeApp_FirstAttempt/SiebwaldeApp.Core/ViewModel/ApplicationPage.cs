namespace SiebwaldeApp.Core
{
    /// <summary>
    /// A page of the application
    /// </summary>
    public enum ApplicationPage
    {
        /// <summary>
        /// The initial login page
        /// </summary>
        Siebwalde = 0,

        TrackControl = 1,

        FiddleYardControl = 2,

        YardControl = 3,

        CityControl = 4,

        /*------------- Siebwalde ---------------------*/

        SiebwaldeInit = 10,

        SiebwaldeSettings = 11,


        /*------------- TrackControl ------------------*/

        TrackPageInit = 20,

        TrackAmplifier = 21,

        StationSettings = 22
    }
}
