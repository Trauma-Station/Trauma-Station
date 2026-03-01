// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Combo component for martial arts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanPerformComboComponent : Component
{
    /// <summary>
    /// Gets hit target.
    /// </summary>
    [DataField]
    public EntityUid? CurrentTarget;

    /// <summary>
    /// Combo memory size.
    /// </summary>
    [DataField]
    public int LastAttacksLimit = 4;

    /// <summary>
    /// Move storage. Stores the last attacks performed, used for combo performing and checking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ComboAttackType> LastAttacks = new();

    /// <summary>
    /// Move storage.
    /// </summary>
    [DataField]
    public List<ComboAttackType>? LastAttacksSaved = new();

    /// <summary>
    /// Current combo list.
    /// </summary>
    [DataField]
    public List<ComboPrototype> AllowedCombos = new();

    /// <summary>
    /// Combo storage from yaml.
    /// </summary>
    [DataField]
    public List<ProtoId<ComboPrototype>> RoundstartCombos = new();

    /// <summary>
    /// Time since last hit.
    /// </summary>
    [DataField]
    public TimeSpan ResetTime = TimeSpan.Zero;

    /// <summary>
    /// Momentum counter.
    /// </summary>
    [DataField]
    public int Momentum;
}
