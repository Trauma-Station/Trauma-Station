// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class UnfathomableCurioShieldComponent : BaseSpriteOverlayComponent
{
    public override Enum Key { get; set; } = CurioShieldKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "unfathomable_shield");

    [DataField]
    public override Color Color { get; set; } = Color.LimeGreen;

    public override bool Unshaded { get; set; } = false;

    [DataField, AutoNetworkedField]
    public override bool Active { get; set; }

    [DataField]
    public TimeSpan ActivateDelay = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan ActivateTime;

    [DataField]
    public SoundSpecifier RechargeSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f)
    };

    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/_Goobstation/Wizard/mm_hit.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f)
    };
}

public enum CurioShieldKey : byte
{
    Key,
}
