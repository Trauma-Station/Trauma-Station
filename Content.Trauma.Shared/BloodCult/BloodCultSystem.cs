// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Trauma.Shared.BloodCult.Spells;
using Content.Trauma.Shared.Roles;

namespace Content.Trauma.Shared.BloodCult;

public abstract partial class BloodCultSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private EntityQuery<MindContainerComponent> _mcQuery = default!;

    private HashSet<Entity<BloodCultMemberComponent>> _cultists = new();
    private HashSet<Entity<HumanoidProfileComponent>> _targets = new();

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

    public EntityUid? GetRole(EntityUid uid)
        => _mcQuery.CompOrNull(uid)?.Mind is {} mind ? MindGetRole(mind) : null;

    public EntityUid? MindGetRole(EntityUid mind)
        => _role.MindHasRole<BloodCultistRoleComponent>(mind, out var role)
            ? role
            : null;

    public Entity<BloodCultSpellsComponent>? GetSpells(EntityUid uid)
        => _mcQuery.CompOrNull(uid)?.Mind is {} mind && TryComp<BloodCultSpellsComponent>(mind, out var comp)
            ? (mind, comp)
            : null;

    public virtual EntityUid? GetTarget(EntityUid member)
        => null;

    public virtual bool IsTarget(EntityUid member, EntityUid target)
        => false;

    /// <summary>
    /// Returns true if a cult's target was sacraficed.
    /// </summary>
    public virtual bool TargetKilled(EntityUid member)
        => false;

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
}
