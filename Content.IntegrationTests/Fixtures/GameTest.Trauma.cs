#nullable enable
using Robust.Shared.Map;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    protected EntityUid SSpawn([ForbidLiteral] EntProtoId id, EntityCoordinates coords)
        => SEntMan.SpawnAtPosition(id, coords);

    protected EntityUid CSpawn([ForbidLiteral] EntProtoId id, EntityCoordinates coords)
        => CEntMan.SpawnAtPosition(id, coords);

    protected void SDel(EntityUid uid)
    {
        SEntMan.DeleteEntity(uid);
    }

    protected void CDel(EntityUid uid)
    {
        CEntMan.DeleteEntity(uid);
    }

    protected EntityPrototype? SPrototype(EntityUid uid)
        => SEntMan.GetComponentOrNull<MetaDataComponent>(uid)?.EntityPrototype;

    protected EntityPrototype? CPrototype(EntityUid uid)
        => CEntMan.GetComponentOrNull<MetaDataComponent>(uid)?.EntityPrototype;

    protected bool SDeleted(EntityUid uid)
        => Deleted(SEntMan, uid);

    protected bool CDeleted(EntityUid uid)
        => Deleted(CEntMan, uid);

    private static bool Deleted(IEntityManager entMan, EntityUid uid)
        => LifeStage(entMan, uid) >= EntityLifeStage.Deleted;

    private static EntityLifeStage LifeStage(IEntityManager entMan, EntityUid uid)
        => entMan.TryGetComponent(uid, out MetaDataComponent? meta)
            ? meta.EntityLifeStage
            : EntityLifeStage.Deleted;

    protected bool SHasComp<T>(EntityUid target)
        where T : IComponent
    {
        return SEntMan.HasComponent<T>(target);
    }

    protected T SEnsureComp<T>(EntityUid target)
        where T : IComponent, new()
    {
        return SEntMan.EnsureComponent<T>(target);
    }

    protected bool CHasComp<T>(EntityUid target)
        where T : IComponent
    {
        return CEntMan.HasComponent<T>(target);
    }

    protected T CEnsureComp<T>(EntityUid target)
        where T : IComponent, new()
    {
        return CEntMan.EnsureComponent<T>(target);
    }

    protected void SRemComp<T>(EntityUid target)
        where T : IComponent, new()
    {
        SEntMan.RemoveComponent<T>(target);
    }

    protected void CRemComp<T>(EntityUid target)
        where T : IComponent, new()
    {
        CEntMan.RemoveComponent<T>(target);
    }

    protected string SPrettyString(EntityUid? uid)
        => uid != null ? SEntMan.ToPrettyString(uid.Value) : string.Empty;

    protected string CPrettyString(EntityUid? uid)
        => uid != null ? CEntMan.ToPrettyString(uid.Value) : string.Empty;
}
