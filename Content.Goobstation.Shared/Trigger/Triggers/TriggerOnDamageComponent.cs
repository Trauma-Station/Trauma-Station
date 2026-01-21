// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Trigger.Triggers;

/// <summary>
/// Triggers when this entity takes damage above a threshold, with the damage origin as the user.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerOnDamageSystem))]
[AutoGenerateComponentState]
public sealed partial class TriggerOnDamageComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Damage done below this threshold is ignored.
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = 5;

    /// <summary>
    /// Probability of triggering from 0-1.
    /// </summary>
    [DataField]
    public float Probability = 1f;
}
