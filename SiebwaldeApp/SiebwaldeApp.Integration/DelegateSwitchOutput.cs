using System;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Adapts an <see cref="ISwitchOutput"/> from a delegate, so the physical side can be a
    /// simulator store, a real accessory bus, or (while real switches are not wired yet) a
    /// diagnostic sink that deliberately drives nothing.
    /// </summary>
    public sealed class DelegateSwitchOutput : ISwitchOutput
    {
        private readonly Func<int, SwitchPosition, bool> _setPosition;

        public DelegateSwitchOutput(Func<int, SwitchPosition, bool> setPosition, bool isAvailable = true)
        {
            _setPosition = setPosition ?? throw new ArgumentNullException(nameof(setPosition));
            IsAvailable = isAvailable;
        }

        /// <inheritdoc />
        public bool IsAvailable { get; }

        /// <inheritdoc />
        public bool SetPosition(int physicalAddress, SwitchPosition position)
            => _setPosition(physicalAddress, position);
    }
}
