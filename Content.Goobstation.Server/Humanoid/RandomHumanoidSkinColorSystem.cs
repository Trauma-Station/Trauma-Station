// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Decals;
using Content.Shared.Humanoid;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Humanoid;

public sealed partial class RandomHumanoidSkinColorSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoid = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomHumanoidSkinColorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomHumanoidSkinColorComponent> ent, ref MapInitEvent args)
    {
        _humanoid.SetSkinColor(ent.Owner, _random.Pick(_prototype.Index(ent.Comp.Palette).Colors.Values));
    }
}
