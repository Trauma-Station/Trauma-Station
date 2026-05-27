// <Trauma>
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Systems;
using Content.Shared.Magic.Components;
using Content.Trauma.Common.Wizard;
// </Trauma>
using Content.Server.Chat.Systems;
using Content.Shared.Actions.Events;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Muting;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// As soon as the chat refactor moves to Shared
/// the logic here can move to the shared <see cref="SharedSpeakOnActionSystem"/>
/// </summary>
public sealed partial class SpeakOnActionSystem : SharedSpeakOnActionSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!; // Goob

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<SpeakOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;

        // If we can't speak, we can't speak
        if (!HasComp<SpeechComponent>(user) || HasComp<MutedComponent>(user))
            return;

        // Goob. TODO: Remove Aviu from this plane of existence for whatever has occured here.
        var speech = ent.Comp.Sentence;

        if (TryComp(ent, out MagicComponent? magic))
        {
            var invocationEv = new GetSpellInvocationEvent(magic.School, args.Performer);
            RaiseLocalEvent(args.Performer, invocationEv);
            if (invocationEv.Invocation.HasValue)
                speech = invocationEv.Invocation;
            if (invocationEv.ToHeal.GetTotal() > FixedPoint2.Zero)
            {
                _damageable.TryChangeDamage(args.Performer,
                    -invocationEv.ToHeal,
                    true,
                    false,
                    targetPart: TargetBodyPart.All,
                    splitDamage: SplitDamageBehavior.SplitEnsureAll); // Shitmed Change
            }
        }

        if (string.IsNullOrWhiteSpace(speech))
            return;

        _chat.TrySendInGameICMessage(user, Loc.GetString(speech), ent.Comp.ChatType, false); // Trauma - use ent.Comp.ChatType
    }
}
