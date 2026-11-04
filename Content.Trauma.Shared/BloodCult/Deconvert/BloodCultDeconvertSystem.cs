// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.BloodCult.Runes;
using Content.Trauma.Shared.BloodCult.Spells;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodCult.Deconvert;

public sealed partial class BloodCultDeconvertSystem : EntitySystem
{
    [Dependency] private ISharedChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    private static readonly ProtoId<ReagentPrototype> Holywater = "Holywater";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, PurgeStomachHolywaterEvent>(_body.RefRelayBodyEvent);
    }

    [SubscribeLocalEvent]
    private void OnPurgeHolywater(Entity<StomachComponent> ent, ref BodyRelayedEvent<PurgeStomachHolywaterEvent> args)
    {
        if (!_solution.ResolveSolution(ent.Owner,
                StomachSystem.DefaultSolutionName,
                ref ent.Comp.Solution,
                out var sol))
            return;

        // TODO replace holy water with unholy water in solution when its real
        sol.RemoveReagent(Holywater, 5);
        _solution.UpdateChemicals(ent.Comp.Solution.Value);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<BloodCultDeconvertComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.DeconversionStartTime = _timing.CurTime;
        ent.Comp.DeconversionEndTime = ent.Comp.DeconversionStartTime + ent.Comp.DeconversionTime;
        Dirty(ent);

        _effects.TryApplyEffect(ent, ent.Comp.EffectOnStart);
    }

    [SubscribeLocalEvent]
    private void OnBeforeHarmfulAction(Entity<BloodCultistComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        // Cult dagger purges holy water and reduces deconversion progress
        if (!HasComp<RuneDrawerComponent>(args.Used))
            return;

        args.Cancelled = true;
        ReduceProgress(ent.Owner, args.User);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<BloodCultDeconvertComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var mobState))
        {
            if (now < comp.NextUpdate || mobState.CurrentState == MobState.Dead)
                continue;

            comp.NextUpdate = now + comp.UpdateTime;

            if (now > comp.DeconversionEndTime)
            {
                _popup.PopupEntity(Loc.GetString("blood-cult-deconverted-message-others", ("target", Identity.Entity(uid, EntityManager))), uid, Filter.PvsExcept(uid), true, PopupType.Medium);
                var msg = Loc.GetString("blood-cult-deconverted-message-self");
                if (TryComp<ActorComponent>(uid, out var actor))
                    _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, uid, false, actor.PlayerSession.Channel, Color.Cyan);

                _cult.Deconvert(uid);

                RemCompDeferred(uid, comp);
                continue;
            }

            Dirty(uid, comp);

            _effects.TryApplyEffect(uid, comp.EffectOnProgress);
        }
    }

    public void ReduceProgress(Entity<BloodCultDeconvertComponent?> ent, EntityUid? user)
    {
        var ev = new PurgeStomachHolywaterEvent();
        RaiseLocalEvent(ent, ref ev);

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var filter = Filter.Pvs(ent);
        if (user != null)
            filter.RemovePlayerByAttachedEntity(user.Value);

        _color.RaiseEffect(Color.Blue, new() { ent }, filter);

        ent.Comp.DeconversionEndTime += ent.Comp.ProgressReductionRate;
        if (ent.Comp.DeconversionEndTime - _timing.CurTime < ent.Comp.DeconversionTime)
        {
            Dirty(ent);
            return;
        }

        var name = Identity.Name(ent, EntityManager);
        _popup.PopupEntity("You are no longer being deconverted.", $"{name} is not longer being deconverted.", ent, null, PopupType.Medium);
        RemCompDeferred(ent, ent.Comp);
    }
}
