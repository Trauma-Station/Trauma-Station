// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands;
using Content.Shared.Mind;
using Content.Trauma.Common.Familiar;
using Content.Trauma.Common.Ghost;

namespace Content.Trauma.Shared.Familiar;

public sealed partial class FamiliarSystem : CommonFamiliarSystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityQuery<FamiliarMasterComponent> _query = default!;
    [Dependency] private EntityQuery<MindComponent> _mindQuery = default!;

    [SubscribeLocalEvent]
    private void OnGhostRoleCreatedMind(Entity<FamiliarMasterComponent> ent, ref GhostRoleCreatedMindEvent args)
    {
        CopyMaster(ent.AsNullable(), args.Mind);
    }

    [SubscribeLocalEvent]
    private void OnEquippedHand(Entity<PickupFamiliarComponent> ent, ref GotEquippedHandEvent args)
    {
        SetMaster(ent.Owner, args.User);
        RemCompDeferred(ent, ent.Comp);
    }

    /// <summary>
    /// Make an entity a familiar, setting its master to a given mind or mob.
    /// </summary>
    public override void SetMaster(EntityUid uid, EntityUid master)
    {
        var comp = EnsureComp<FamiliarMasterComponent>(uid);
        var ent = (uid, comp);

        // masters that are also familiars dont get to be masters themselves
        if (CopyMaster(master, ent))
            return;

        if (_mind.GetMind(master) is { } mind)
            master = mind;

        if (comp.Master == master)
            return;

        comp.Master = master;
        comp.MasterName = GetName(master);
        Dirty(uid, comp);

        // if the familiar already has a mind set the master on it as well
        if (_mind.GetMind(uid) is { } famMind)
            CopyMaster(ent, famMind);
    }

    /// <summary>
    /// Copies the master of a <c>src</c> entity to <c>dest</c>.
    /// Returns true if <c>src</c> was a familiar, returns false otherwise.
    /// </summary>
    public bool CopyMaster(Entity<FamiliarMasterComponent?> src, Entity<FamiliarMasterComponent?> dest)
    {
        if (_mind.GetMind(src.Owner) is { } mind)
            src = mind;

        if (!_query.Resolve(src, ref src.Comp, false))
            return false;

        dest.Comp ??= EnsureComp<FamiliarMasterComponent>(dest);

        dest.Comp.Master = src.Comp.Master;
        dest.Comp.MasterName = src.Comp.MasterName;
        Dirty(dest, dest.Comp);
        return true;
    }

    /// <summary>
    /// Get the name of a familiar's master, or null if the entity isn't a familiar.
    /// </summary>
    public string? GetMasterName(EntityUid uid)
    {
        if (_mind.GetMind(uid) is { } mind && GetMasterName(mind) is { } name)
            return name;

        return _query.CompOrNull(uid)?.MasterName;
    }

    private string GetName(EntityUid uid)
        => _mindQuery.CompOrNull(uid)?.CharacterName ?? Name(uid);
}
