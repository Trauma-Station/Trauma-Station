// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Combo component for martial arts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanPerformComboComponent : Component
{
    [DataField]
    public EntityUid? CurrentTarget;

    [DataField]
    public int LastAttacksLimit = 4;

    [DataField, AutoNetworkedField]
    public List<ComboAttackType> LastAttacks = new();

    [DataField]
    public List<ComboAttackType>? LastAttacksSaved = new();

    [ViewVariables]
    public List<ComboPrototype> AllowedCombos = new();

    [DataField]
    public List<ProtoId<ComboPrototype>> RoundstartCombos = new();

    [DataField]
    public TimeSpan ResetTime = TimeSpan.Zero;
}
