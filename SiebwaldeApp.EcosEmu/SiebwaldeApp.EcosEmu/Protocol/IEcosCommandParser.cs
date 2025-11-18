
namespace SiebwaldeApp.EcosEmu
{
    public interface IEcosCommandParser
    {
        EcosCommand? Parse(string line);
    }
}
