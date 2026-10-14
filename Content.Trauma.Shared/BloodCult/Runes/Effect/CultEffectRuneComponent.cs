// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.BloodCult.Runes.Barrier;

/// <summary>
/// Applies entity effects to this rune when invoked by a cultist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultEffectRuneComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
