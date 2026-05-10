// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.HolographicProjector;

public sealed class GenericFieldSystem : EntitySystem
{
    [Dependency] private readonly GenericFieldGeneratorSystem _generator = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenericFieldComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var field, out var damageable))
        {
            if (_timing.CurTime < field.RegenTimer) continue;

            field.RegenTimer = field.RegenTime + _timing.CurTime;
            _damageable.HealEvenly((uid, damageable), field.RegenRate);
        }
    }

    private void OnShutdown(Entity<GenericFieldComponent> field, ref ComponentShutdown args)
    {
        if (field.Comp.SourceGen is {} source)
            _generator.FieldDestroyed(source);
    }
}
