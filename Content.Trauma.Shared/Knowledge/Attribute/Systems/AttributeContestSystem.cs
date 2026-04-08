using Content.Shared.Popups;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.Miscellanious.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Attribute.Systems;

public sealed partial class AttributeContestSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ActiveMeleeResolveEvent>(ResolveAttack);
    }

    private void ResolveAttack(Entity<KnowledgeHolderComponent> ent, ref ActiveMeleeResolveEvent args)
    {
        var attacker = ent;
        var defender = args.Defender;

        var evAttackMod = new GetAttackModifierEvent();
        RaiseLocalEvent(attacker, ref evAttackMod);

        var evDefenseMod = new GetDefenseModifierEvent();
        RaiseLocalEvent(defender, ref evDefenseMod);

        var evDefenseDice = new GetDefenseDice(8);
        RaiseLocalEvent(defender, ref evDefenseDice);

        var evOpposedContest = new OnAttributeOpposedContest(defender, 20, evAttackMod.Mod, evDefenseDice.Dice, evDefenseMod.Mod);
        RaiseLocalEvent(attacker, ref evOpposedContest);

        // Makes it harder to defend after being hit, the beatdown is gonna be brutal if you're surrounded.
        if (!TryComp<DefenseTierdownComponent>(defender, out var defenseDown))
            defenseDown = AddComp<DefenseTierdownComponent>(defender);
        defenseDown.Mod += 1.0f;
        Dirty(defender, defenseDown);

        if (evOpposedContest.Failed)
        {
            if (evOpposedContest.CriticallySucceededOpposed)
            {
                var queued = AddComp<QueuedParryComponent>(defender); // Defender gets a free strike.
                queued.TimeToHit = _timing.CurTime + TimeSpan.FromSeconds(1); // Hit next second.
                queued.Target = ent;
                Dirty(defender, queued);
                _popup.PopupClient("You've shown an opening!", ent, ent, PopupType.Small);
                _popup.PopupEntity("The opponent has shown an opening, prepare for an attack!", defender, defender, PopupType.Small);
            }
            else
            {
                _popup.PopupClient("You've been parried.", ent, ent, PopupType.Small);
                _popup.PopupEntity("You've successfully defended against an opponent.", defender, defender, PopupType.Small);
            }
            args.Cancelled = true;
            return;
        }
        if (evOpposedContest.CriticallySucceededUser)
        {
            args.Damage *= 3; // Replace with critical hit function?
            _popup.PopupClient("Good strike!", ent, ent, PopupType.Small);
        }
    }
}
