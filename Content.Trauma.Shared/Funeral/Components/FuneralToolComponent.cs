// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Funeral.Components;

/// <summary>
/// Component for bibles to let them check the funeral progress of an entity, and marks them to turn to holy ash when cremated if successful. <see cref="FuneralComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FuneralToolComponent : Component
{
    /// <summary>
    /// Optional whitelist the user has to match to use it.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    /// Which effect to display.
    /// </summary>
    [DataField]
    public EntProtoId EffectProto = "EffectSpark";

    /// <summary>
    /// Which sound effect to play.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultBibleHealSound = new("BibleHeal");

    [DataField]
    public SoundSpecifier? SoundPath = new SoundCollectionSpecifier(DefaultBibleHealSound);
}
