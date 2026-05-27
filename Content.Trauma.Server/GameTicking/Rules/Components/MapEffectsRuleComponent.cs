// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component that runs entity effects on the first station's map entity.
/// </summary>
[RegisterComponent, Access(typeof(MapEffectsRuleSystem))]
public sealed partial class MapEffectsRuleComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
