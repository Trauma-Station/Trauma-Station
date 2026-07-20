// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Silicon.DeadStartupButton;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeadStartupButtonComponent : Component
{
    [DataField]
    public LocId VerbText = "dead-startup-button-verb";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Arcade/newgame.ogg");

    [DataField]
    public SoundSpecifier ButtonSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public TimeSpan StartupDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier BuzzSound = new SoundCollectionSpecifier("buzzes")
    {
        Params = new()
        {
            Variation = 0.05f
        }
    };

    [DataField]
    public int VerbPriority = 1;
}
