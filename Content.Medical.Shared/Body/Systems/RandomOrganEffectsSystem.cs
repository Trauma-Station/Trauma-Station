// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Body;

public sealed partial class RandomOrganEffectsSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomOrganEffectsComponent, OrganGotInsertedEvent>(OnInserted);
    }

    private void OnInserted(Entity<RandomOrganEffectsComponent> ent, ref OrganGotInsertedEvent args)
    {
        SetNextUpdate(ent);
    }

    private void SetNextUpdate(Entity<RandomOrganEffectsComponent> ent)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        var comp = ent.Comp;
        var randomSeconds = rand.NextDouble() * (comp.MaxActivationTime.TotalSeconds - comp.MinActivationTime.TotalSeconds);
        var randomSpan = comp.MinActivationTime + TimeSpan.FromSeconds(randomSeconds);
        comp.NextUpdate = _timing.CurTime + randomSpan;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RandomOrganEffectsComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            if (_body.GetBody(uid) is not {} body) // do nothing when not installed
                continue;

            // don't predict effects for other players
            // it can be done but it's an unreasonable expectation for entity effects to play nicely with it
            if (_net.IsClient && body != _player.LocalEntity)
                continue;

            _effects.ApplyEffects(body, comp.Effects);
            SetNextUpdate((uid, comp));
        }
    }
}
