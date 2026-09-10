using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

public sealed partial class BasicEntityAmmoProviderComponent
{
    [DataField]
    public ProtoId<WeightedRandomEntityPrototype>? Prototypes;
}
