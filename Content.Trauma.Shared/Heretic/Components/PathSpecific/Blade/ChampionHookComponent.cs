// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChampionHookComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public bool IsEnabled;

    [DataField, AutoNetworkedField]
    public EntityUid? HookedMob;

    [DataField, AutoNetworkedField]
    public EntityUid? Weapon;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/blood3.ogg");

    [DataField]
    public float OffhandAttackSpeedBuff = 0.35f;

    [DataField]
    public float MovespeedBuff = 0.5f;

    [DataField]
    public DamageSpecifier ExtraDamage = new()
    {
        DamageDict =
        {
            { "Slash", 20 },
        },
    };
}
