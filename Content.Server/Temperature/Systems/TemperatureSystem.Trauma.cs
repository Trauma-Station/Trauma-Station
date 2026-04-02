// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Temperature;
using Content.Goobstation.Common.Temperature.Components;
using Content.Shared.Atmos;
using Content.Trauma.Common.Wizard;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Trauma - shitcode collection
/// </summary>
public sealed partial class TemperatureSystem
{
    [Dependency] private readonly CommonSpellbladeSystem _spellblade = default!;

    // it was already hardcoded so idc
    private const float IdealTemperature = Atmospherics.T37C;

    private void InitializeTrauma()
    {
        SubscribeLocalEvent<SpecialLowTempImmunityComponent, TemperatureImmunityEvent>(OnCheckLowTemperatureImmunity);
        SubscribeLocalEvent<SpecialHighTempImmunityComponent, TemperatureImmunityEvent>(OnCheckHighTemperatureImmunity);
    }

    private bool CanTakeHeatDamage(EntityUid uid)
        => !_spellblade.IsHoldingItemWithFireSpellbladeEnchantmentComponent(uid);

    private void OnCheckLowTemperatureImmunity(Entity<SpecialLowTempImmunityComponent> ent, ref TemperatureImmunityEvent args)
    {
        if (args.CurrentTemperature < IdealTemperature)
            args.CurrentTemperature = IdealTemperature;
    }

    private void OnCheckHighTemperatureImmunity(Entity<SpecialHighTempImmunityComponent> ent, ref TemperatureImmunityEvent args)
    {
        if (args.CurrentTemperature > IdealTemperature)
            args.CurrentTemperature = IdealTemperature;
    }
}
