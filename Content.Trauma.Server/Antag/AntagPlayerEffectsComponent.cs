// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;

namespace Content.Trauma.Server.Antag;

/// <summary>
/// Runs entity effects on players selected by this antag selection rule.
/// </summary>
[RegisterComponent, Access(typeof(AntagPlayerEffectsSystem))]
public sealed partial class AntagPlayerEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
