// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Whetstone;

[RegisterComponent, NetworkedComponent, Access(typeof(WhetstoneSystem))]
[AutoGenerateComponentState]
public sealed partial class WhetstoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Uses = 1;

    [DataField]
    public DamageSpecifier DamageIncrease = new()
    {
        DamageDict = new()
        {
            { "Slash", 4 }
        }
    };

    [DataField]
    public float MaximumIncrease = 25;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier SharpenAudio = new SoundPathSpecifier("/Audio/Items/sheath.ogg");
}
