// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Blob;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Goobstation.Shared.Blob.Events;
using Content.Server.Actions;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Shared.Gibbing;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobCarrierSystem : SharedBlobCarrierSystem
{
    [Dependency] private BlobObserverSystem _observer = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private GhostRoleSystem _ghost = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private ActionsSystem _action = default!;
    [Dependency] private CommonLanguageSystem _language = default!;

    private static readonly EntProtoId ActionTransformToBlob = "ActionTransformToBlob";

    [SubscribeLocalEvent]
    private void OnRemove(Entity<BlobCarrierComponent> ent, ref ComponentShutdown args)
    {
        _language.UpdateEntityLanguages(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnMindAdded(Entity<BlobCarrierComponent> ent, ref MindAddedMessage args)
    {
        ent.Comp.HasMind = true;
    }

    [SubscribeLocalEvent]
    private void OnMindRemove(Entity<BlobCarrierComponent> ent, ref MindRemovedMessage args)
    {
        ent.Comp.HasMind = false;
    }

    [SubscribeLocalEvent]
    private void OnTransformToBlob(Entity<BlobCarrierComponent> uid, ref TransformToBlobActionEvent args)
        => TransformToBlob(uid);

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BlobCarrierComponent> ent, ref MapInitEvent args)
    {
        _language.UpdateEntityLanguages(ent.Owner);
        _action.AddAction(ent.Owner, ref ent.Comp.TransformToBlob, ActionTransformToBlob);

        if (HasComp<ActorComponent>(ent))
            return;

        var ghostRole = EnsureComp<GhostRoleComponent>(ent);
        EnsureComp<GhostTakeoverAvailableComponent>(ent);
        ghostRole.RoleName = Loc.GetString("blob-carrier-role-name");
        ghostRole.RoleDescription = Loc.GetString("blob-carrier-role-desc");
        ghostRole.RoleRules = Loc.GetString("blob-carrier-role-rules");
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<BlobCarrierComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            TransformToBlob(ent);
        }
    }

    protected override void TransformToBlob(Entity<BlobCarrierComponent> ent)
    {
        var xform = Transform(ent);
        if (!HasComp<MapGridComponent>(xform.GridUid))
            return;

        var core = Spawn(ent.Comp.CoreBlobPrototype, xform.Coordinates);
        if (_mind.TryGetMind(ent, out _, out var mind) && mind.UserId is { } userId)
        {
            var ghostRoleComp = Comp<GhostRoleComponent>(core);

            // Unfortunately we have to manually turn this off so we don't need to make more prototypes.
            _ghost.UnregisterGhostRole((core, ghostRoleComp));

            if (!TryComp<BlobCoreComponent>(core, out var coreComp))
                return;

            _observer.CreateBlobObserver((core, coreComp), userId);
        }

        _gibbing.Gib(ent);
    }
}
