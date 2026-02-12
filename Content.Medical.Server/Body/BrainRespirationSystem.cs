using Content.Medical.Shared.Consciousness;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;

namespace Content.Medical.Server.Body;

/// <summary>
/// Handles respirator x brain interaction because the events are in server, fuck you
/// </summary>
// TODO SHITMED: kill this and have low brain oxygenation knock you out instead...
public sealed class BrainRespirationSystem : EntitySystem
{
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConsciousnessComponent, SuffocationEvent>(OnSuffocation);
        SubscribeLocalEvent<ConsciousnessComponent, StopSuffocatingEvent>(OnStopSuffocating);
    }

    private void OnSuffocation(Entity<ConsciousnessComponent> ent, ref SuffocationEvent args)
    {
        if (!TryComp<RespiratorComponent>(ent, out var respirator) ||
            !_consciousness.TryGetNerveSystem(ent, out var brain))
            return;

        var damage = respirator.Damage;
        var total = damage.GetTotal();
        if (!_consciousness.TryGetConsciousnessModifier(ent, brain.Value, out var modifier, "Suffocation"))
        {
            _consciousness.AddConsciousnessModifier(
                ent,
                brain.Value,
                -total,
                identifier: "Suffocation",
                type: ConsciousnessModType.Pain);
        }
        else
        {
            _consciousness.SetConsciousnessModifier(
                ent,
                brain.Value,
                modifier.Value.Change - total,
                identifier: "Suffocation",
                type: ConsciousnessModType.Pain);
        }
    }

    private void OnStopSuffocating(Entity<ConsciousnessComponent> ent, ref StopSuffocatingEvent args)
    {
        if (!TryComp<RespiratorComponent>(ent, out var respirator) ||
            !_consciousness.TryGetNerveSystem(ent, out var brain) ||
            !_consciousness.TryGetConsciousnessModifier(ent, brain.Value, out var modifier, "Suffocation"))
            return;

        var rec = respirator.DamageRecovery;
        var recovery = rec.GetTotal();
        if (modifier.Value.Change < recovery)
        {
            _consciousness.RemoveConsciousnessModifier(ent, brain.Value, "Suffocation");
            return;
        }

        _consciousness.SetConsciousnessModifier(
            ent,
            brain.Value,
            modifier.Value.Change + recovery,
            identifier: "Suffocation",
            type: ConsciousnessModType.Pain);
    }
}
