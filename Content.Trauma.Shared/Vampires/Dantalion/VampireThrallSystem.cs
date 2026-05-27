// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// This handles anything related to Dantalion's thralling.
/// </summary>
public sealed partial class VampireThrallSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private EntityQuery<VampireThrallsComponent> _thrallsQuery = default!;
    [Dependency] private EntityQuery<CollectiveMindComponent> _collectiveMindQuery = default!;

    private static readonly ProtoId<CollectiveMindPrototype> DantalionMind = "Dantalion";
    private static readonly EntProtoId<MindRoleComponent> ThrallMindRole = "MindRoleVampireThrall";

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

        if (!_mind.TryGetMind(target, out var mindId, out _))
        {
            _popup.PopupClient("The target has no mind!", user, user, PopupType.MediumCaution);
            return;
        }

        if (ent.Comp.Thralls.Count == cap)
        {
            _popup.PopupClient($"You can't have more than {cap} thralls!", user, user, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Thralls.Add(target);
        Dirty(ent);

        _popup.PopupClient("You gain a new thrall!", user, user, PopupType.Medium);

        var comp = EnsureComp<VampireThrallComponent>(target);
        comp.Vampire = user;
        Dirty(target, comp);

        // Holy shit, make an api for it bruh
        EnsureComp<CollectiveMindComponent>(target).Channels.Add(DantalionMind);

        _role.MindAddRole(mindId, ThrallMindRole);
        args.Handled = true;

        _admin.Add(LogType.Vampire, LogImpact.High, $"Vampire {user} made {target} a thrall via Enthrall action");
    }


    #region Thrall
    /// <summary>
    /// Vampire thralls are unable to be glared at.
    /// </summary>
    private void OnGlare(Entity<VampireThrallComponent> ent, ref GlareAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    /// Vampire thralls have protection against bloodsucking.
    /// </summary>
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
        var user = ent.Owner;
        if (!_thrallsQuery.TryComp(vampire, out var thralls))
            return;

        thralls.Thralls.Remove(user);
        Dirty(vampire, thralls);

        _popup.PopupClient("You are freed from enthrallment!", user, user, PopupType.Large);

        // Notify the vampire that they lost a thrall
        if (_net.IsServer)
            _popup.PopupEntity("You feel like you lost a follower!", vampire, vampire, PopupType.LargeCaution);

        // Remove collective mind channel since they don't need it anymore
        if (!_collectiveMindQuery.TryComp(user, out var collectiveMind))
            return;

        collectiveMind.Channels.Remove(DantalionMind);

        // Remove the antag role
        if (!_mind.TryGetMind(user, out var mindId, out _))
            return;

        _role.MindRemoveRole(mindId, ThrallMindRole);
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
