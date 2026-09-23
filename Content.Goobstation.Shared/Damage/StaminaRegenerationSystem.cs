// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Goobstation.Shared.Damage;

// TODO: make this a status effect
public sealed partial class StaminaRegenerationSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    [SubscribeLocalEvent]
    private void OnStaminaRegenerationStartup(Entity<StaminaRegenerationComponent> ent, ref ComponentStartup args)
    {
        _stamina.ToggleStaminaDrain(ent, ent.Comp.RegenerationRate, true, false, ent.Comp.RegenerationKey, ent);
    }

    [SubscribeLocalEvent]
    private void OnStaminaRegenerationShutdown(Entity<StaminaRegenerationComponent> ent, ref ComponentShutdown args)
    {
        _stamina.ToggleStaminaDrain(ent, ent.Comp.RegenerationRate, false, false, ent.Comp.RegenerationKey, ent);
    }
}
