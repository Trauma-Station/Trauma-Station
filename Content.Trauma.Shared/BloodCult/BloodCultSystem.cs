// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Spells;
using Content.Trauma.Shared.Roles;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.BloodCult;

public abstract partial class BloodCultSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;
    [Dependency] private EntityQuery<BloodCultMemberComponent> _query = default!;
    [Dependency] private EntityQuery<BloodCultRuleComponent> _ruleQuery = default!;
    [Dependency] private EntityQuery<MindContainerComponent> _mcQuery = default!;

    private HashSet<Entity<BloodCultMemberComponent>> _cultists = new();
    private HashSet<Entity<HumanoidProfileComponent>> _targets = new();

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<BloodCultMemberComponent> ent, ref PlayerAttachedEvent args)
    {
        _pvsOverride.AddSessionOverride(ent.Comp.Rule, args.Player);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<BloodCultMemberComponent> ent, ref PlayerDetachedEvent args)
    {
        _pvsOverride.RemoveSessionOverride(ent.Comp.Rule, args.Player);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BloodCultMemberComponent> ent, ref ComponentShutdown args)
    {
        if (_actorQuery.TryComp(ent, out var actor))
            _pvsOverride.RemoveSessionOverride(ent.Comp.Rule, actor.PlayerSession);
    }

    /// <summary>
    /// Get the gamerule associated with an entity belonging to the cult.
    /// </summary>
    public Entity<BloodCultRuleComponent>? GetRule(EntityUid mob)
    {
        if (_mcQuery.CompOrNull(mob)?.Mind is { } mind)
            return MindGetRule(mind);

        return _query.CompOrNull(mob)?.Rule is { } rule && _ruleQuery.TryComp(rule, out var ruleComp)
            ? (rule, ruleComp)
            : null;
    }

    /// <summary>
    /// Returns true if a player is a blood cultist, leader or construct.
    /// </summary>
    public bool IsCultist(EntityUid uid)
        => GetRole(uid) != null;

    /// <summary>
    /// Returns true if a mind is a blood cultist, leader or construct.
    /// </summary>
    public bool IsMindCultist(EntityUid mind)
        => MindGetRole(mind) != null;

    public Entity<BloodCultistRoleComponent>? GetRole(EntityUid uid)
        => _mcQuery.CompOrNull(uid)?.Mind is {} mind ? MindGetRole(mind) : null;

    public Entity<BloodCultistRoleComponent>? MindGetRole(EntityUid mind)
        => _role.MindHasRole<BloodCultistRoleComponent>(mind, out var role)
            ? (role.Value.Owner, role.Value.Comp2)
            : null;

    public Entity<BloodCultRuleComponent>? MindGetRule(EntityUid mind)
        => MindGetRole(mind)?.Comp.Rule is { } rule && _ruleQuery.TryComp(rule, out var ruleComp)
            ? (rule, ruleComp)
            : null;

    public Entity<BloodCultSpellsComponent>? GetSpells(EntityUid uid)
        => _mcQuery.CompOrNull(uid)?.Mind is {} mind && TryComp<BloodCultSpellsComponent>(mind, out var comp)
            ? (mind, comp)
            : null;

    public EntityUid? GetTarget(EntityUid member)
        => GetRule(member)?.Comp.OfferingTarget;

    public bool IsTarget(EntityUid member, EntityUid target)
        => GetTarget(member) == target;

    /// <summary>
    /// Returns true if a cult's target was sacraficed.
    /// </summary>
    public bool TargetKilled(EntityUid member)
        => GetRule(member)?.Comp.TargetSacrificed ?? false;

    public virtual void Convert(EntityUid member, EntityUid target)
    {
    }

    /// <summary>
    /// Gets all cultists/constructs near a rune.
    /// The hashset returned is reused between calls, do not store it.
    /// </summary>
    public HashSet<Entity<BloodCultMemberComponent>> GatherCultists(EntityUid rune, float range)
    {
        var pos = Transform(rune).Coordinates;
        _cultists.Clear();
        _lookup.GetEntitiesInRange(pos, range, _cultists);
        return _cultists;
    }

    /// <summary>
    /// Gets all the humanoids (and monkeys/scurrets) near a rune.
    /// This will include cultists.
    /// The hashset returned is reused between calls, do not store it.
    /// </summary>
    public HashSet<Entity<HumanoidProfileComponent>> GetTargetsNearRune(EntityUid rune, float range)
    {
        var pos = Transform(rune).Coordinates;
        _targets.Clear();
        _lookup.GetEntitiesInRange(pos, range, _targets);
        return _targets;
    }

    /// <summary>
    /// Set a cultist's gamerule and network it to them.
    /// </summary>
    public void SetCultRule(EntityUid mob, EntityUid rule)
    {
        var comp = EnsureComp<BloodCultMemberComponent>(mob);
        comp.Rule = rule;
        Dirty(mob, comp);

        if (_actorQuery.TryComp(mob, out var actor))
            _pvsOverride.AddSessionOverride(rule, actor.PlayerSession);
        if (GetRole(mob) is { } role)
        {
            role.Comp.Rule = rule;
            Dirty(role);
        }
    }

    /// <summary>
    /// Copy a cult member's cult rule to another entity.
    /// </summary>
    public void CopyMember(Entity<BloodCultMemberComponent?> src, Entity<BloodCultMemberComponent?> dest)
    {
        if (!_query.Resolve(src, ref src.Comp))
            return;

        dest.Comp ??= EnsureComp<BloodCultMemberComponent>(dest);
        dest.Comp.Rule = src.Comp.Rule;
        Dirty(dest, dest.Comp);
    }
}
