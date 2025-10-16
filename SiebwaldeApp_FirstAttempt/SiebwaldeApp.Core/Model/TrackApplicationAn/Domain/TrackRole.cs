// SiebwaldeApp.Core/Domain/TrackRole.cs
namespace SiebwaldeApp.Core
{
    public enum TrackRole
    {
        PassengerOnly,
        FreightAllowed,   // outer station tracks that accept passenger, allow freight if configured
        MiddleFreight     // the middle track intended for freight priority
    }
}
