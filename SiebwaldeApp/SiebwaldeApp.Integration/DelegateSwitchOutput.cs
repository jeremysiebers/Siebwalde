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
        private readonly Action<int, SwitchPosition> _setPosition;

        public DelegateSwitchOutput(Action<int, SwitchPosition> setPosition)
        {
            _setPosition = setPosition ?? throw new ArgumentNullException(nameof(setPosition));
        }

        /// <inheritdoc />
        public void SetPosition(int physicalAddress, SwitchPosition position)
            => _setPosition(physicalAddress, position);
    }
}
