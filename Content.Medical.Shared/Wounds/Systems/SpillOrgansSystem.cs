using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds.Components;

namespace Content.Medical.Shared.Wounds.Systems;

public sealed partial class SpillOrgansSystem : EntitySystem
{
    [Dependency] private readonly BodyPartSystem _part = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpillOrgansComponent, WoundAddedEvent>(OnStartup);
    }

    public void OnStartup(Entity<SpillOrgansComponent> ent, ref WoundAddedEvent args)
    {
        var target = args.RootWoundable.Owner;
        if (!HasComp<BodyPartComponent>(target))
            return;

        foreach (var (_, part) in _part.GetPartOrgans(target))
        {
            if (!HasComp<InternalOrganComponent>(part))
                continue;

            _part.RemoveOrgan(target, part.Owner);
        }
    }
}
