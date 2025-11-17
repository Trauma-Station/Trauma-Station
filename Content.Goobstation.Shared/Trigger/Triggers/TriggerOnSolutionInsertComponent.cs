// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Trigger.Triggers;

/// <summary>
/// Triggers if this entity has a valid quantity of a specific reagent is added to a solution in any desecndant of a inserted entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnSolutionInsertComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The reagent to look for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// If non-null, won't trigger if the found amount is below this.
    /// </summary>
    [DataField]
    public float? MinAmount;

    /// <summary>
    /// If non-null, won't trigger if the found amount is above this.
    /// </summary>
    [DataField]
    public float? MaxAmount;
}
