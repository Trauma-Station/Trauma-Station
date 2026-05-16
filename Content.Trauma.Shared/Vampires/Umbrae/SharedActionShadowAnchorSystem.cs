// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Umbrae;

public abstract partial class SharedActionShadowAnchorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    private static readonly EntProtoId ShadowAnchor = "ShadowAnchor";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionShadowAnchorComponent, ShadowAnchorActionEvent>(OnShadowAnchor);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var eqe = EntityQueryEnumerator<ActiveActionShadowAnchorComponent, ActionShadowAnchorComponent>();
        while (eqe.MoveNext(out var uid, out var active, out var anchor))
        {
            if (now < active.FakeRecallUpdate)
                continue;

            var anchorEnt = anchor.Anchor;
            if (anchorEnt is { } anchorEntity)
            {
                // Spawn clones on top of the anchor and fake a recall
                if (_action.GetAction(uid) is { } action && action.Comp.AttachedEntity is { } attachedEnt)
                {
                    _effects.ApplyEffects(attachedEnt, anchor.EffectsOnFakeRecall);
                    SpawnShadowClone(attachedEnt, _transform.GetMapCoordinates(anchorEntity));
                }
            }

            if (Exists(anchorEnt))
                PredictedQueueDel(anchorEnt);

            anchor.Anchor = null;
            anchor.Casted = false;
            Dirty(uid, anchor);

            RemCompDeferred(uid, active);
        }
    }

    private void OnShadowAnchor(Entity<ActionShadowAnchorComponent> ent, ref ShadowAnchorActionEvent args)
    {
        var user = args.Performer;
        var xform = Transform(user);

        // If the action has already been cast, then just teleport us at the anchor.
        if (ent.Comp.Casted && ent.Comp.Anchor is { } anchorEnt)
        {
            _teleport.Teleport(user, Transform(anchorEnt).Coordinates, user);
            ent.Comp.Casted = false;

            // Remove anything related to the anchor, since we used our recast.
            PredictedQueueDel(anchorEnt);
            RemCompDeferred<ActiveActionShadowAnchorComponent>(ent.Owner);

            ent.Comp.Anchor = null;

            Dirty(ent);

            // We only handle the action on recall, not when making the anchor.
            args.Handled = true;
            return;
        }

        var anchor = PredictedSpawnAtPosition(ShadowAnchor, xform.Coordinates);
        ent.Comp.Anchor = anchor;
        ent.Comp.Casted = !ent.Comp.Casted;
        Dirty(ent);

        var comp = new ActiveActionShadowAnchorComponent();
        comp.FakeRecallUpdate = _timing.CurTime + ent.Comp.FakeRecallDuration;
        AddComp(ent.Owner, comp);
    }

    protected virtual void SpawnShadowClone(EntityUid uid, MapCoordinates coordinates) { }
}
