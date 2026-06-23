using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Access.Systems;

namespace Content.Shared._Orion.Bitrunning.Systems;

public sealed class BitrunningPointsSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    private EntityQuery<BitrunningPointsComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<BitrunningPointsComponent>();
    }

    public Entity<BitrunningPointsComponent?>? GetPointComp(EntityUid user)
    {
        if (TryComp<BitrunningPointsComponent>(user, out var comp))
            return (user, comp);

        return TryFindIdCard(user);
    }

    public Entity<BitrunningPointsComponent?>? TryFindIdCard(EntityUid user)
    {
        if (!_idCard.TryFindIdCard(user, out var idCard))
            return null;

        if (!_query.TryComp(idCard, out var comp))
            return null;

        return (idCard, comp);
    }

    public bool AddPoints(Entity<BitrunningPointsComponent?> ent, uint amount)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        if (amount > uint.MaxValue - ent.Comp.Points)
            ent.Comp.Points = uint.MaxValue;
        else
            ent.Comp.Points += amount;

        Dirty(ent);
        return true;
    }

    public bool RemovePoints(Entity<BitrunningPointsComponent?> ent, uint amount)
    {
        if (!_query.Resolve(ent, ref ent.Comp) || amount > ent.Comp.Points)
            return false;

        ent.Comp.Points -= amount;
        Dirty(ent);
        return true;
    }
}
