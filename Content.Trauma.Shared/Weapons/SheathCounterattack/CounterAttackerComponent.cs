using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Weapons.SheathCounterattack;

/// <summary>
/// Added to mobs that can counterattack with sheathed weapons
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CounterAttackerComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon =
        new SpriteSpecifier.Rsi(new ResPath("_Trauma/Interface/Alerts/counterattack.rsi"), "icon");
}
