// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class UnfathomableCurioShieldComponent : Component
{
    [DataField]
    public Color Color = Color.LimeGreen;

    [DataField]
    public TimeSpan ActivateDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan FadeTime = TimeSpan.FromMilliseconds(500);

    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Time when shield did or will activate
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan ActivateTime;

    /// <summary>
    /// Time when shield did deactivate
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan DeactivateTime;

    [DataField]
    public float SlowdownRadius = 1.5f;

    [DataField]
    public float BulletSlowdown = 0.02f;

    [DataField]
    public EntityWhitelist BulletWhitelist = new()
    {
        Components = new[] { "Projectile" },
    };

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
