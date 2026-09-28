// <Trauma>
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Systems;
using Content.Shared.Magic.Components;
using Content.Trauma.Common.Wizard;
// </Trauma>
using Content.Shared.Speech.Components;
using Content.Shared.Actions.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeakOnActionSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private DamageableSystem _damageable = default!;
    // </Trauma>
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<SpeakOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // If we can't speak, we can't speak.
        if (!HasComp<SpeechComponent>(user) || !_actionBlocker.CanSpeak(user))
            return;

        if (!random.Prob(ent.Comp.SpeakChance))
            return;

        var randomSentence = GetDialogue(ent.Comp.DialogueDataset, random) ?? ent.Comp.Sentence;
        // <Trauma> - allow replacing sentence via speech variable and magic
        if (TryComp(ent, out MagicComponent? magic))
        {
            var invocationEv = new GetSpellInvocationEvent(magic.School, args.Performer);
            RaiseLocalEvent(args.Performer, invocationEv);
            if (invocationEv.Invocation.HasValue)
                randomSentence = invocationEv.Invocation;
            if (invocationEv.ToHeal.GetTotal() > FixedPoint2.Zero)
            {
                _damageable.ChangeDamage(args.Performer,
                    -invocationEv.ToHeal,
                    true,
                    false,
                    targetPart: TargetBodyPart.All,
                    splitDamage: SplitDamageBehavior.SplitEnsureAll);
            }
        }
        // </Trauma>

        if (!string.IsNullOrWhiteSpace(randomSentence))
            _chat.TrySendInGameICMessage(user, Loc.GetString(randomSentence), ent.Comp.ChatType, false); // Trauma - use ent.Comp.ChatType
    }

    private string? GetDialogue(ProtoId<LocalizedDatasetPrototype>? dialogue, IRobustRandom random)
    {
        if (!ProtoMan.TryIndex(dialogue, out var proto))
            return null;

        return random.Pick(proto.Values);
    }
}
