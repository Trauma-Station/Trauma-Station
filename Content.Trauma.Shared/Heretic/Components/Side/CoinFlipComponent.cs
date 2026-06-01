// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CoinFlipComponent : Component
{
    [DataField(required: true)]
    public string FlippingSpriteState;

    [DataField(required: true)]
    public List<CoinSide> Sides;

    [DataField, AutoNetworkedField]
    public CoinSide? CurrentSide;

    [DataField(required: true)]
    public TimeSpan FlipTime;

    [DataField]
    public TimeSpan FlipDelay = TimeSpan.FromMilliseconds(250);

    [DataField, AutoNetworkedField]
    public bool IsFlipping;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan FlipEndTime;

    [DataField]
    public SoundSpecifier FlipSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/coinflip.ogg");

    [DataField]
    public EntityUid? User;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CoinSide
{
    [DataField]
    public LocId Name;

    [DataField]
    public string SpriteState;

    [DataField, NonSerialized]
    public EntityEffect[] UserEffects;
}

[Serializable, NetSerializable]
public enum CoinFlipVisuals : byte
{
    SpriteState,
}

[Serializable, NetSerializable]
public enum CoinFlipKey : byte
{
    Key,
}
