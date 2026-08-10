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

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeakOnActionSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private DamageableSystem _damageable = default!;
    // </Trauma>
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    [SubscribeLocalEvent]
    private void OnActionPerformed(Entity<SpeakOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;

        // If we can't speak, we can't speak.
        if (!HasComp<SpeechComponent>(user) || !_actionBlocker.CanSpeak(user))
            return;

        // <Trauma> - allow replacing sentence via speech variable and magic
        var speech = ent.Comp.Sentence;
        if (TryComp(ent, out MagicComponent? magic))
        {
            var invocationEv = new GetSpellInvocationEvent(magic.School, args.Performer);
            RaiseLocalEvent(args.Performer, invocationEv);
            if (invocationEv.Invocation.HasValue)
                speech = invocationEv.Invocation;
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
        if (string.IsNullOrWhiteSpace(speech))
        // </Trauma>
            return;

        _chat.TrySendInGameICMessage(user, Loc.GetString(speech), ent.Comp.ChatType, false); // Trauma - use speech and ent.Comp.ChatType
    }
}
