namespace SiebwaldeApp
{
    public interface IAmplifierInitializersBaseClass
    {
        // name of the class
        string Name { get; set; }
        // execution part of the class
        (uint, string) Execute(ReceivedMessage receivedMessage);
    }
}
