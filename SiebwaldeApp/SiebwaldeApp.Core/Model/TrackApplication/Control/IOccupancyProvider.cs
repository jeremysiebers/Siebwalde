namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Reports whether a block is currently occupied.
    /// Implementations: the real one reads amplifier occupancy; tests use a fake.
    /// </summary>
    public interface IOccupancyProvider
    {
        bool IsBlockOccupied(int block);
    }
}
