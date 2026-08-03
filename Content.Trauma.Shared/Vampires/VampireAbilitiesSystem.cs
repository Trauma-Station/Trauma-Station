// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.MobClass;

namespace Content.Trauma.Shared.Vampires;

public sealed partial class VampireAbilitiesSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private VampireSystem _vampire = default!;
    [Dependency] private MobClassSystem _mobClass = default!;

    /// <summary>
    /// List of every vampire ability prototype.
    /// </summary>
    [ViewVariables]
    public List<ProtoId<VampireAbilityPrototype>> AllAbilities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireAbilitiesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VampireAbilitiesComponent, MobClassSelectedEvent>(OnMobClassSelected);

        SubscribeLocalEvent<VampireAbilitiesComponent, VampireTotalBloodChangedEvent>(OnBloodChanged);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    private void OnMobClassSelected(Entity<VampireAbilitiesComponent> ent, ref MobClassSelectedEvent args)
    {
        var totalBlood = _vampire.GetTotalBlood(ent.Owner);
        UnlockAbilities(ent, totalBlood);
    }

    private void OnMapInit(Entity<VampireAbilitiesComponent> ent, ref MapInitEvent args)
    {
        var totalBlood = _vampire.GetTotalBlood(ent.Owner);
        UnlockAbilities(ent, totalBlood);
    }

    private void OnBloodChanged(Entity<VampireAbilitiesComponent> ent, ref VampireTotalBloodChangedEvent args) =>
        UnlockAbilities(ent, args.Blood);

    /// <summary>
    /// Unlocks abilities based on various factors (mob class, total blood, conditions).
    /// </summary>
    private void UnlockAbilities(Entity<VampireAbilitiesComponent> ent, int blood)
    {
        var user = ent.Owner;

        var mobClass = _mobClass.GetClass(user);
        foreach (var ability in AllAbilities)
        {
            if (!ProtoMan.Resolve(ability, out var abilityProto))
                continue;

            // We tried to unlcok an ability, but we don't have enough total blood
            if (abilityProto.Cost > blood)
                continue;

            // We tried to unlock an ability, but we have already unlocked it
            if (ent.Comp.UnlockedAbilities.Contains(ability))
                continue;

            // We tried to unlock an ability that doesn't belong to us, or we're blacklisted from it.
            if (( abilityProto.Class is { } requiredClass && mobClass != requiredClass )
                || abilityProto.BlacklistClass is { } blacklistClass && mobClass == blacklistClass)
                continue;

            // We tried to unlock an ability, but we didn't pass the extra conditions
            if (abilityProto.Conditions is { } conditions && !_conditions.TryConditions(user, conditions, user))
                continue;

            _effects.ApplyEffects(user, abilityProto.OnUnlock);

            ent.Comp.UnlockedAbilities.Add(ability);
            Dirty(ent);

            _admin.Add(LogType.Vampire, LogImpact.Medium, $"User {ent.Owner} has gained the ability {ability.Id}");
        }
    }

    private void LoadPrototypes()
    {
        AllAbilities.Clear();
        foreach (var proto in ProtoMan.EnumeratePrototypes<VampireAbilityPrototype>())
        {
            var id = proto.ID;
            AllAbilities.Add(id);
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<VampireAbilityPrototype>())
            return;

        LoadPrototypes();
    }
}
