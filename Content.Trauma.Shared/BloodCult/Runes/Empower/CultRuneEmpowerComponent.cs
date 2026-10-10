// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Runes.Empower;

/// <summary>
/// Gives the user <c>BloodCultEmpoweredComponent</c> and allows to choose spells.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneEmpowerComponent : Component
{
    [DataField]
    public TimeSpan SpellCreationTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Spells that you can get with empower rune.
    /// </summary>
    [DataField]
    public List<EntProtoId> AvailableActions = new()
    {
        "ActionBloodCultStun",
        "ActionBloodCultTeleport",
        "ActionBloodCultEmp",
        "ActionBloodCultShadowShackles",
        "ActionBloodCultTwistedConstruction",
        "ActionBloodCultSummonRitualDagger",
        "ActionBloodCultBloodRites"
    };
}

[Serializable, NetSerializable]
public enum CultSpellsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class CultSpellSelectedMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
