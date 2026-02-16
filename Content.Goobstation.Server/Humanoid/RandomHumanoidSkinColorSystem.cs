using Content.Shared.Decals;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Humanoid;

public sealed class RandomHumanoidSkinColorSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomHumanoidSkinColorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomHumanoidSkinColorComponent> ent, ref MapInitEvent args)
    {
        _humanoid.SetSkinColor(ent.Owner, _random.Pick(_prototype.Index(ent.Comp.Palette).Colors.Values));
    }
}
