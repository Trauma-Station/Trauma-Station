// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Spells;

/// <summary>
/// Mind component that holds spells for cultists.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BloodCultSpellsComponent : Component
{
    [DataField]
    public TimeSpan SpellCreationTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ActiveSpells = new();

    /// <summary>
    /// Actions that you can create.
    /// </summary>
    [DataField]
    public List<EntProtoId> AvailableActions = new()
    {
        "ActionBloodCultStun",
        "ActionBloodCultTeleport",
        "ActionBloodCultEmp",
        "ActionBloodCultShadowShackles",
        "ActionBloodCultTwistedConstruction",
        "ActionBloodCultSummonCombatEquipment",
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
