// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Magic;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.Side;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract partial class SharedGhoulSystem : EntitySystem
{
    [Dependency] protected ISharedPlayerManager Player = default!;

    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _dmg = default!;

    [Dependency] private EntityQuery<BrainComponent> _brainQuery = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhoulComponent, BeforeMindSwappedEvent>(OnBeforeMindSwap);

        SubscribeLocalEvent<HereticMinionComponent, AttackAttemptEvent>(OnTryAttack);
    }

    public virtual void UnGhoulifyEntity(Entity<GhoulComponent> ent) { }

    private void OnTryAttack(Entity<HereticMinionComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (target == ent.Comp.BoundHeretic || HasComp<ShadowCloakEntityComponent>(target) &&
            Transform(target).ParentUid == ent.Comp.BoundHeretic)
            args.Cancel();
    }


    private void OnBeforeMindSwap(Entity<GhoulComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;
        args.Message = "ghoul";
    }

    /// <summary>
    /// Required to prevent heretic from farming organs from ghouls
    /// </summary>
    public void MakeOrgansFragile(EntityUid uid)
    {
        foreach (var organ in _body.GetOrgans(uid))
        {
            // Don't curse brain and torso
            if (_brainQuery.HasComp(organ) || _woundableQuery.TryComp(organ, out var woundable) &&
                woundable.RootWoundable == organ.Owner)
                continue;

            EnsureComp<FragileOrganComponent>(organ);
        }
    }

    /// <summary>
    /// If ghoul is not npc controlled and not player controlled (SSD), this method kills and deconverts it
    /// Returns true on success or if entity is not a ghoul
    /// </summary>
    public bool TryKillAndDeconvertInactiveGhoul(Entity<GhoulComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return true;

        // Client check because client can't see other sessions, just assume they have non ssd mind
        if (_net.IsClient)
            return false;

        if (HasComp<ActiveNPCComponent>(ent))
            return false;

        // If body has session attached, don't touch it
        if (Player.TryGetSessionByEntity(ent, out var session) && session.Status == SessionStatus.InGame)
            return false;

        if (!_mobState.IsAlive(ent.Owner))
        {
            UnGhoulifyEntity(ent!);
            return true;
        }

        var dmg = new DamageSpecifier(ProtoMan.Index(Blunt), ent.Comp.TotalHealth * 1.2f);
        _dmg.ChangeDamage(ent.Owner, dmg, targetPart: TargetBodyPart.Vital, ignoreResistances: true, increaseOnly: true);

        // Ghoul component should automatically be removed on death in most cases, or ghoul gets givved
        if (TerminatingOrDeleted(ent) || !Resolve(ent, ref ent.Comp, false))
            return true;

        UnGhoulifyEntity(ent!);
        return true;
    }
}
