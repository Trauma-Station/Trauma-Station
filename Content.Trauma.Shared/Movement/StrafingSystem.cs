// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CombatMode;
using Content.Trauma.Common.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Movement;

/// <summary>
/// Handles adding MouseRotator while you hold the strafe key (shift)
/// </summary>
public sealed partial class StrafingSystem : EntitySystem
{
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private EntityQuery<CombatModeComponent> _combatQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(TraumaKeyFunctions.Strafe,
                InputCmdHandler.FromDelegate(args => ToggleRotator(args, true), args => ToggleRotator(args, false), false, false))
            .Register<StrafingSystem>();
    }

    private void ToggleRotator(ICommonSession? session, bool value)
    {
        if (session?.AttachedEntity is not { } ent)
            return;

        // Don't try and override combat mode doing the same thing
        if (_combatQuery.CompOrNull(ent) is { ToggleMouseRotator: true, IsInCombatMode: true })
            return;

        _combat.SetMouseRotatorComponents(ent, value);
    }
}
