// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Converte items that pass the whitelist into papers after a doafter
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EldritchPaperConverterComponent : Component
{
    [DataField]
    public EntProtoId<EldritchPaperComponent> PaperProto = "EldritchPaper";

    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier ConvertSound = new SoundPathSpecifier("/Audio/_Goobstation/Items/handling/paper_use.ogg");

    [DataField]
    public SpriteSpecifier VerbIcon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/bureaucracy.rsi"), "paper");
}
