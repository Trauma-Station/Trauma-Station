using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Lollypop;

[RegisterComponent, NetworkedComponent, Access(typeof(LollypopSystem))]
public sealed partial class LollypopComponent : Component
{
    [DataField]
    public SlotFlags CheckSlot = SlotFlags.MASK;

    [DataField]
    public TimeSpan BiteInterval = TimeSpan.FromSeconds(3);
}
