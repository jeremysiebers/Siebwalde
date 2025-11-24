
namespace SiebwaldeApp.EcosEmu
{
    public interface IBlockPositionProvider
    {
        /// <summary>
        /// Geeft laatste bekende blokpositie van een specifieke locomotief.
        /// </summary>
        int? TryGetBlockForLoc(int loc);

        /// <summary>
        /// Event trigged wanneer Koploper meldt dat een loc een nieuw blok binnenrijdt.
        /// </summary>
        event Action<int, int>? BlockEntered;
    }
}
