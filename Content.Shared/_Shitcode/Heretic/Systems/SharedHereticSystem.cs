using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Common.Conversion;
using Content.Goobstation.Common.Heretic;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract class SharedHereticSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private EntityQuery<HereticComponent> _hereticQuery;
    private EntityQuery<GhoulComponent> _ghoulQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, BeforeConversionEvent>(OnConversionAttempt);

        _hereticQuery = GetEntityQuery<HereticComponent>();
        _ghoulQuery = GetEntityQuery<GhoulComponent>();
    }

    private void OnConversionAttempt(Entity<MindContainerComponent> ent, ref BeforeConversionEvent args)
    {
        if (TryGetHereticComponent(ent.AsNullable(), out _, out _))
            args.Blocked = true;
    }

    public bool TryGetHereticComponent(
        Entity<MindContainerComponent?> ent,
        [NotNullWhen(true)] out HereticComponent? heretic,
        out EntityUid mind)
    {
        heretic = null;
        return _mind.TryGetMind(ent, out mind, out _, ent.Comp) && _hereticQuery.TryComp(mind, out heretic);
    }

    public bool IsHereticOrGhoul(EntityUid uid)
    {
        return _ghoulQuery.HasComp(uid) || TryGetHereticComponent(uid, out _, out _);
    }
}
