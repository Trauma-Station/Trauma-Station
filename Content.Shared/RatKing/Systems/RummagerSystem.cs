using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.RatKing.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing; // Goob

namespace Content.Shared.RatKing.Systems;

public sealed class RummagerSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable =  default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // Goob
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RummageableComponent, ComponentInit>(OnComponentInit); // Goobstation
        SubscribeLocalEvent<RummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<RummageableComponent, RummageDoAfterEvent>(OnDoAfterComplete);
    }

    // Goobstation
    public void OnComponentInit(EntityUid uid, RummageableComponent component, ComponentInit args)
    {
        component.NextLoot = _timing.CurTime;
        Dirty(uid, component);
    }

    private void OnGetVerb(Entity<RummageableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<RummagerComponent>(args.User) || ent.Comp.Looted)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rat-king-rummage-text"),
            Priority = 0,
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    ent.Comp.RummageDuration,
                    new RummageDoAfterEvent(),
                    ent,
                    ent)
                {
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    DistanceThreshold = 2f
                });
            }
        });
    }

    private void OnDoAfterComplete(Entity<RummageableComponent> ent, ref RummageDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.Looted)
            return;

        // <DeltaV> - Rummaging an object updates the looting cooldown rather than a "previously looted" check.
        // Note that the "Looted" boolean can still be checked (by mappers/admins)
        // to disable rummaging on the object indefinitely, but rummaging will no
        // longer permanently prevent future rummaging.
        var now = _timing.CurTime;
        if (now < ent.Comp.NextLoot)
            return;

        ent.Comp.NextLoot = now + ent.Comp.RummageCooldown;
        // </DeltaV>
        Dirty(ent, ent.Comp);
        _audio.PlayPredicted(ent.Comp.Sound, ent, args.User);

        if (_net.IsClient)
            return;

        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        var coordinates = Transform(ent).Coordinates;

        foreach (var spawn in spawns)
        {
            Spawn(spawn, coordinates);
        }
    }
}

/// <summary>
/// DoAfter event for rummaging through a container with RummageableComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RummageDoAfterEvent : SimpleDoAfterEvent;
