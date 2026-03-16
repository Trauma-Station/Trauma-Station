using Content.Shared._Shitcode.Heretic.SpriteOverlay;
using Content.Shared._Shitcode.Heretic.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Shitcode.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SacramentsOfPowerComponent : BaseSpriteOverlayComponent
{
    [DataField, AutoNetworkedField]
    public SacramentsState State = SacramentsState.Opening;

    [DataField]
    public TimeSpan StateUpdateAt;

    [DataField]
    public float DamageReturnRatio = 0.65f;

    [DataField]
    public float StaminaDamageReturnRatio = 1f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/ark_deathrattle.ogg");

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/piano_hit.ogg");

    [DataField]
    public TimeSpan ActivationTime = TimeSpan.FromSeconds(0.8);

    [DataField]
    public TimeSpan DeactivationTime = TimeSpan.FromSeconds(0.9);

    [DataField]
    public TimeSpan EffectTime = TimeSpan.FromSeconds(5);

    [DataField]
    public Dictionary<SacramentsState, string> SpriteStates = new()
    {
        { SacramentsState.Opening, "eye_open" },
        { SacramentsState.Open, "eye_pulse" },
        { SacramentsState.Closing, "eye_close" },
    };

    public override Enum Key { get; set; } = SacramentsKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "eye_open");
}
