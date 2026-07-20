// <Trauma>
using Robust.Shared.GameStates;
// </Trauma>
using Content.Shared.Damage;

namespace Content.Shared.Wieldable.Components;

[RegisterComponent, Access(typeof(SharedWieldableSystem))]
[NetworkedComponent, AutoGenerateComponentState] // Trauma - networked it holy
public sealed partial class IncreaseDamageOnWieldComponent : Component
{
    [DataField("damage", required: true)]
    [Access(Other = AccessPermissions.ReadExecute)]
    [AutoNetworkedField] // Trauma
    public DamageSpecifier BonusDamage = default!;
}
