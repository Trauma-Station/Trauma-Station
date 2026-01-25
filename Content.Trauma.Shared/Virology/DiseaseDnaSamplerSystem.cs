// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Virology;
using Content.Shared.DoAfter;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Shared.Disease;
using Content.Trauma.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Virology;

/// <summary>
/// Handles everything related to the syndicate DNA sampler.
/// </summary>
public sealed class DiseaseDnaSamplerSystem : EntitySystem
{
    [Dependency] private readonly DnaTargetDiseaseSystem _target = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDnaSamplerComponent, BeforeRangedInteractEvent>(OnRangedInteract);
        SubscribeLocalEvent<DiseaseDnaSamplerComponent, DnaSamplerDoAfterEvent>(OnDoAfter);
        Subs.BuiEvents<DiseaseDnaSamplerComponent>(DiseaseDnaSamplerUiKey.Key, subs =>
        {
            subs.Event<DiseaseDnaSamplerCreateMessage>(OnCreateInjector);
        });
    }

    private void OnRangedInteract(Entity<DiseaseDnaSamplerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        args.Handled = true;
        StartSamplingDna(ent, target, args.User);
    }

    private void OnDoAfter(Entity<DiseaseDnaSamplerComponent> ent, ref DnaSamplerDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not {} target)
            return;

        SampleDna(ent, target, args.User);
    }

    private void OnCreateInjector(Entity<DiseaseDnaSamplerComponent> ent, ref DiseaseDnaSamplerCreateMessage args)
    {
        CreateDiseasePen(ent, args.Actor);
    }

    public void StartSamplingDna(Entity<DiseaseDnaSamplerComponent> ent, EntityUid target, EntityUid user)
    {
        // can't sample the device itself, can't sample yourself (ask a buddy to, I cbf to write more popups)
        if (target == ent.Owner || target == user)
            return;

        var delay = ent.Comp.SampleDelay;
        if (HasComp<AwakeMobComponent>(target))
            delay *= ent.Comp.AwakeModifier;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            delay,
            new DnaSamplerDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        var targetIdent = Identity.Entity(target, EntityManager);
        var userIdent = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("disease-dna-sampler-popup-target", ("user", userIdent)), target, target, PopupType.LargeCaution);
        _popup.PopupClient(Loc.GetString("disease-dna-sampler-popup-user", ("target", targetIdent)), target, user, PopupType.Medium);
    }

    public void SampleDna(Entity<DiseaseDnaSamplerComponent> ent, EntityUid target, EntityUid user)
    {
        // currently forensics dnas dont get networked so let server do it
        if (_net.IsClient)
            return;

        ent.Comp.TargetDnas.Clear();
        Dirty(ent);

        var identity = Identity.Entity(target, EntityManager);
        AddSamples(ent, target);
        if (ent.Comp.TargetDnas.Count == 0) // found nothing
        {
            _popup.PopupEntity(Loc.GetString("disease-dna-sampler-failed", ("target", identity)), target, user);
            return;
        }

        _popup.PopupEntity(Loc.GetString("disease-dna-sampler-success", ("target", identity)), target, user);
        _audio.PlayPredicted(ent.Comp.SampleSound, target, user);
    }

    private void AddSamples(Entity<DiseaseDnaSamplerComponent> ent, EntityUid target)
    {
        if (CompOrNull<DnaComponent>(target)?.DNA is {} targetDna)
            ent.Comp.TargetDnas.Add(targetDna);

        if (!TryComp<ForensicsComponent>(target, out var forensics) || forensics.DNAs.Count == 0)
            return;

        foreach (var (dna, _) in forensics.DNAs)
        {
            ent.Comp.TargetDnas.Add(dna);
        }
    }

    public void CreateDiseasePen(Entity<DiseaseDnaSamplerComponent> ent, EntityUid user)
    {
        if (ent.Comp.Disease is not {} proto || ent.Comp.TargetDnas.Count == 0)
            return;

        // spawn and set up the disease's dna target
        var disease = EntityManager.PredictedSpawn(proto);
        _target.AddTargetDnas(disease, ent.Comp.TargetDnas);
        Clear(ent);

        // put the disease in an injector
        var pen = PredictedSpawnAtPosition(ent.Comp.Injector, Transform(ent).Coordinates);
        var comp = Comp<DiseasePenComponent>(pen);
        comp.DiseaseUid = disease;
        Dirty(pen, comp);

        // give it to the user... war crimes await
        _hands.TryPickupAnyHand(user, pen);
    }

    public void Clear(Entity<DiseaseDnaSamplerComponent> ent)
    {
        ent.Comp.Disease = null;
        ent.Comp.TargetDnas.Clear();
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaSamplerDoAfterEvent : SimpleDoAfterEvent;
