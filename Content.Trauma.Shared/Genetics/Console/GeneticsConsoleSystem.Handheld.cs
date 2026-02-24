// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private void InitializeHandheld()
    {
        SubscribeLocalEvent<HandheldGeneticsScannerComponent, MapInitEvent>(OnHandheldInit);
        SubscribeLocalEvent<HandheldGeneticsScannerComponent, AfterInteractEvent>(OnHandheldInteract);
        SubscribeLocalEvent<HandheldGeneticsScannerComponent, LinkDoAfterEvent>(OnLinkDoAfter);

        SubscribeLocalEvent<LinkedToGeneticScannerComponent, ComponentShutdown>(OnLinkedShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LinkedToGeneticScannerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var xform = Transform(uid);
            var map = xform.MapUid;
            var pos = _transform.GetWorldPosition(xform);
            comp.Scanners.RemoveAll(scanner =>
            {
                var scannerXform = Transform(scanner);
                if (scannerXform.MapUid != map)
                {
                    SetScannedMob(scanner, null);
                    return true;
                }

                var scannerPos = _transform.GetWorldPosition(scannerXform);
                var dist2 = (pos - scannerPos).LengthSquared();
                if (dist2 < comp.RangeSquared) // still in range keep it
                    return false;

                SetScannedMob(scanner, null);
                return true;
            });

            if (comp.Scanners.Count == 0)
                RemCompDeferred(uid, comp);
        }
    }

    private void OnHandheldInit(Entity<HandheldGeneticsScannerComponent> ent, ref MapInitEvent args)
    {
        // it's the scanner itself
        SetScanner(ent.Owner, ent.Owner);
    }

    private void OnHandheldInteract(Entity<HandheldGeneticsScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not {} target || !_mutation.CanMutate(target))
            return;

        var user = args.User;
        var userIdentity = Identity.Entity(user, EntityManager);
        var targetIdentity = Identity.Entity(target, EntityManager);
        var you = Loc.GetString("genetics-console-linking-you", ("scanner", ent), ("user", userIdentity));
        var others = Loc.GetString("genetics-console-linking-others", ("scanner", ent), ("user", userIdentity), ("target", targetIdentity));
        _popup.PopupPredicted(you, others, user, target);
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.LinkTime,
            new LinkDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnLinkDoAfter(Entity<HandheldGeneticsScannerComponent> ent, ref LinkDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not {} target)
            return;

        if (GetScannedMob(ent.Owner) is {} oldMob)
            Unlink(oldMob, ent.Owner);

        var user = args.User;
        _popup.PopupClient(Loc.GetString("genetics-console-linked"), user, user);
        SetScannedMob(ent.Owner, target);

        var linked = EnsureComp<LinkedToGeneticScannerComponent>(target);
        linked.Scanners.Add(ent.Owner);
        Dirty(target, linked);

        _adminLog.Add(LogType.Genetics, LogImpact.Low, $"{ToPrettyString(user)} linked {ToPrettyString(target)} to {ToPrettyString(ent)}");
        _ui.TryOpenUi(ent.Owner, GeneticsConsoleUiKey.Key, user);
    }

    private void OnLinkedShutdown(Entity<LinkedToGeneticScannerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var scanner in ent.Comp.Scanners)
        {
            SetScannedMob(scanner, null);
        }
        ent.Comp.Scanners.Clear();
    }

    private void Unlink(Entity<LinkedToGeneticScannerComponent?> ent, EntityUid scanner)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Scanners.RemoveAll(uid => uid == scanner);
        if (ent.Comp.Scanners.Count == 0)
            RemComp(ent, ent.Comp);
        else
            Dirty(ent, ent.Comp);
    }
}

[Serializable, NetSerializable]
public sealed partial class LinkDoAfterEvent : SimpleDoAfterEvent;
