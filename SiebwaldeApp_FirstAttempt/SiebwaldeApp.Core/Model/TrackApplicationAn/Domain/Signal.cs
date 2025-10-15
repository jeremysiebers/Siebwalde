// File: SiebwaldeApp.Core/Domain/Signal.cs
namespace SiebwaldeApp.Core
{
    public class Signal
    {
        public string Name { get; }
        public SignalAspect Aspect { get; private set; } = SignalAspect.Red;

        public Signal(string name) => Name = name;

        public void Set(SignalAspect aspect) => Aspect = aspect;
    }
}
