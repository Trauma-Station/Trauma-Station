// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MansusGraspComponent : Component
{
    [DataField]
    public TimeSpan CooldownAfterUse = TimeSpan.FromSeconds(10);

    [DataField]
    public EntityWhitelist Blacklist = new();

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(5f);

    [DataField]
    public float StaminaDamage = 80f;

    [DataField]
    public TimeSpan SpeechTime = TimeSpan.FromSeconds(10f);

    [DataField]
    public TimeSpan AffectedTime = TimeSpan.FromMinutes(5);

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Items/welder.ogg");

    [DataField]
    public LocId? Invocation = "heretic-speech-mansusgrasp";
}
