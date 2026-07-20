// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Medical.Common.Targeting;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Speech;

namespace Content.Goobstation.Shared.Blob;

public abstract partial class SharedBlobMobSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;
    [Dependency] private EntityQuery<BlobMobComponent> _mobQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobSpeakComponent, SpeakAttemptEvent>(OnSpeakAttempt, after: [ typeof(SpeechSystem) ]);
    }

    public virtual void NodePulse(Entity<BlobMobComponent> ent)
    {
        _damage.ChangeDamage(ent.Owner, ent.Comp.HealthOfPulse, targetPart: TargetBodyPart.All);
    }

    [SubscribeLocalEvent]
    private void OnBlobAttackAttempt(Entity<BlobMobComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Cancelled || !_tileQuery.HasComp(args.Target) && !_mobQuery.HasComp(args.Target))
            return;

        _popup.PopupCursor(Loc.GetString("blob-mob-attack-blob"), ent, PopupType.Large);
        args.Cancel();
    }

    private void OnSpeakAttempt(Entity<BlobSpeakComponent> ent, ref SpeakAttemptEvent args)
    {
        if (HasComp<BlobCarrierComponent>(ent))
            return;

        args.Uncancel(); // very sus...
    }
}
