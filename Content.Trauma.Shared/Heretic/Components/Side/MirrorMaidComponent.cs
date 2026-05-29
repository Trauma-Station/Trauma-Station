// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class MirrorMaidComponent : Component
{
    [DataField]
    public DamageSpecifier ExamineDamage = new()
    {
        DamageDict =
        {
            { "Blunt", 15 },
        }
    };

    [DataField]
    public TimeSpan ExamineDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId ExamineStatus = "ExaminedMirrorMaidStatusEffect";

    [DataField]
    public SoundSpecifier? ExamineSound = new SoundPathSpecifier("/Audio/_Goobstation/Wizard/ghost2.ogg");
}
