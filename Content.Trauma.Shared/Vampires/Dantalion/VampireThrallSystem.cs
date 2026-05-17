// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// This handles anything related to Dantalion's thralling.
/// </summary>
public sealed partial class VampireThrallSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    // [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityQuery<VampireThrallsComponent> _thrallsQuery = default!;
    [Dependency] private EntityQuery<CollectiveMindComponent> _collectiveMindQuery = default!;

    private static readonly ProtoId<CollectiveMindPrototype> DantalionMind = "Dantalion";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireThrallsComponent, DanEnthrallActionEvent>(OnEnthrall);

        SubscribeLocalEvent<VampireThrallComponent, GlareAttemptEvent>(OnGlare);
        SubscribeLocalEvent<VampireThrallComponent, BloodsuckingAttemptEvent>(OnBloodsucking);
        SubscribeLocalEvent<VampireThrallComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnEnthrall(Entity<VampireThrallsComponent> ent, ref DanEnthrallActionEvent args)
    {
        var user = ent.Owner;
        var target = args.Target;
        var cap = ent.Comp.ThrallCap;

        /*if (!_mind.TryGetMind(target, out _, out _))
        {
            _popup.PopupClient("The target has no mind!", user, PopupType.MediumCaution);
            return;
        } TODO: Uncomment when done testing */

        if (ent.Comp.Thralls.Count == cap)
        {
            _popup.PopupClient($"You can't have more than {cap} thralls!", user, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Thralls.Add(target);
        Dirty(ent);

        _popup.PopupClient("You gain a new thrall!", user, PopupType.Medium);

        var comp = EnsureComp<VampireThrallComponent>(target);
        comp.Vampire = user;
        Dirty(target, comp);

        // Holy shit, make an api for it bruh
        EnsureComp<CollectiveMindComponent>(target).Channels.Add(DantalionMind);

        args.Handled = true;
    }


    #region Thrall
    private void OnGlare(Entity<VampireThrallComponent> ent, ref GlareAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnBloodsucking(Entity<VampireThrallComponent> ent, ref BloodsuckingAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnShutdown(Entity<VampireThrallComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        // Remove ourselves from the vampire
        var vampire = ent.Comp.Vampire;
        if (!_thrallsQuery.TryComp(vampire, out var thralls))
            return;

        thralls.Thralls.Remove(ent.Owner);
        Dirty(vampire, thralls);

        _popup.PopupClient("You are fred from enthrallment!", ent.Owner, PopupType.Large);

        // Notify the vampire that they lost a thrall
        _popup.PopupEntity("You feel like you lost a follower!", vampire, PopupType.LargeCaution);

        if (!_collectiveMindQuery.TryComp(ent.Owner, out var collectiveMind))
            return;

        collectiveMind.Channels.Remove(DantalionMind);
    }
    #endregion

    #region Public Api

    /// <summary>
    /// Adjusts the amount of thralls this vampire can have.
    /// </summary>
    public void AdjustThrallCap(Entity<VampireThrallsComponent?> ent, int amount)
    {
        if (!_thrallsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.ThrallCap += amount;
        Dirty(ent);
    }

    #endregion
}
