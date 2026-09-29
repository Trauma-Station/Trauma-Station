// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Effect that adds temporary components to the target entity.
/// </summary>
public sealed partial class GrantTemporatyComponentEffect : EntityEffectBase<GrantTemporatyComponentEffect>
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField(required: true)]
    public TimeSpan LifeSpan = default!;
}