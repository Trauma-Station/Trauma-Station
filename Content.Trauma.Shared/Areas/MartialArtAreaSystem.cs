// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Common.MartialArts;

namespace Content.Trauma.Shared.Areas;

public sealed class MartialArtAreaSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;

    private EntityQuery<MartialArtAreaComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<MartialArtAreaComponent>();

        SubscribeLocalEvent<TransformComponent, CanDoCQCEvent>(OnCanDoCQC);
    }

    private void OnCanDoCQC(Entity<TransformComponent> ent, ref CanDoCQCEvent args)
    {
        args.Handled |= _area.GetArea(ent.Comp.Coordinates) is {} area &&
            _query.TryComp(area, out var comp) &&
            comp.Form == args.Form;
    }
}
