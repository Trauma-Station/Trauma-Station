// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.Heretic.Components.Ghoul;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FleshHereticMindComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Ghouls = new();

    [DataField, AutoNetworkedField]
    public int GhoulLimit = 5;

    [DataField, AutoNetworkedField]
    public DamageSpecifier WormSustainedDamage = new();
}

[Serializable, NetSerializable]
public enum HereticGhoulRecallKey : byte
{
    Key
}
