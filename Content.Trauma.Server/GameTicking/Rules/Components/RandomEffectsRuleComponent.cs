// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component that runs entity effects on its station periodically.
/// </summary>
[RegisterComponent, Access(typeof(RandomEffectsRuleSystem))]
[AutoGenerateComponentPause]
public sealed partial class RandomEffectsRuleComponent : Component
{
    [DataField(required: true)]
    public TimeSpan MinTime;

    [DataField(required: true)]
    public TimeSpan MaxTime;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextEffect;

    /// <summary>
    /// The station picked to apply effects to every time it updates.
    /// </summary>
    [DataField]
    public EntityUid Station;
}
