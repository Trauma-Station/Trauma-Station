// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Server.ClockworkCult.Components;

[RegisterComponent, Access(typeof(ClockworkCultRuleSystem))]
public sealed partial class ClockworkCultRuleComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Cultists = [];

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Misc/ratvar_reveal.ogg");
}
