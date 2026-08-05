// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Spy;

/// <summary>
/// Added to pda, all spies can view and claim bounties through it
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpyUplinkComponent : Component
{
    public override bool SessionSpecific => true;

    /// <summary>
    /// Mind of whoever owns it, used for examine info
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid OwnerMind;

    [DataField]
    public SoundSpecifier StealStartSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/pshoom.ogg");

    [DataField]
    public SoundSpecifier StealEndSound = new SoundPathSpecifier("/Audio/_Trauma/Effects/wewewew.ogg");
}
