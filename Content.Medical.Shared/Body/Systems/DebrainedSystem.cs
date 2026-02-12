// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

// TODO SHITMED: kill this and HeartSystem fucking aids
/// <summary>
///     This system handles behavior on entities when they lose their head or their brains are removed.
///     MindComponent fuckery should still be mainly handled on BrainSystem as usual.
/// </summary>
public sealed class DebrainedSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrainedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DebrainedComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<DebrainedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<DebrainedComponent, StandAttemptEvent>(OnStandAttempt);
    }

    private void OnComponentInit(EntityUid uid, DebrainedComponent _, ComponentInit args)
    {
        EnsureComp<DelayedDeathComponent>(uid);
        EnsureComp<StunnedComponent>(uid);
        _standing.Down(uid);
    }

    private void OnComponentRemove(EntityUid uid, DebrainedComponent _, ComponentRemove args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        RemComp<DelayedDeathComponent>(uid);
        RemComp<StunnedComponent>(uid);
        if (_body.GetOrgan(uid, Heart) != null)
            RemComp<DelayedDeathComponent>(uid);
    }

    private void OnSpeakAttempt(EntityUid uid, DebrainedComponent _, SpeakAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
        args.Cancel();
    }

    private void OnStandAttempt(EntityUid uid, DebrainedComponent _, StandAttemptEvent args)
    {
        args.Cancel();
    }
}
