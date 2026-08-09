using Content.Shared.Popups;
using Content.Trauma.Shared.Magic.Demonologist;
using Content.Trauma.Shared.Magic.Demonologist.Components;
using Content.Trauma.Shared.Magic.Demonologist.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Magic.Demonologist;

public sealed partial class DemonPortalSystem : SharedDemonologistSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DemonPortalComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var portal, out var xform))
        {
            if (portal.Demons.Count == 0)
                continue;

            // Wait until the portal is ready to summon another demon.
            if (now < portal.NextSpawnTime)
                continue;

            Spawn(portal.Demons[_random.Next(portal.Demons.Count)], xform.Coordinates);

            portal.DemonsSpawned++;

            if (portal.DemonsSpawned >= portal.MaxDemons)
            {
                QueueDel(uid);
                continue;
            }

            // Set the next time the portal can summon a demon.
            portal.NextSpawnTime += portal.SpawnInterval;
        }
    }

    [SubscribeLocalEvent]
    private void OnSummonPortal(Entity<DemonologistComponent> ent, ref SummonDemonPortalSpellEvent args)
    {
        var portal = Spawn("DemonologistRitualCircle", Transform(ent.Owner).Coordinates);

        if (!TryComp<DemonPortalComponent>(portal, out var portalComp))
            return;

        // Set the time when the portal can first summon a demon.
        portalComp.NextSpawnTime = _timing.CurTime + portalComp.SpawnInterval;

        _popup.PopupEntity(Loc.GetString("demonologist-portal-gathering"), ent.Owner, ent.Owner);

        args.Handled = true;
    }
}
