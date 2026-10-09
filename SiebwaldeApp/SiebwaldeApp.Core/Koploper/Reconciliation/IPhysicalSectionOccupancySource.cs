namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// A synchronous source of <see cref="PhysicalSectionOccupancyObservation"/> values. Each
    /// <see cref="Read"/> produces one observation from the current physical amplifier state.
    /// </summary>
    public interface IPhysicalSectionOccupancySource
    {
        PhysicalSectionOccupancyObservation Read();
    }
}
