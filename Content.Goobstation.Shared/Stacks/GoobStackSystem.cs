// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Stack;
using Content.Shared.Stacks;

namespace Content.Goobstation.Shared.Stacks;

/// <summary>
/// Gives every <see cref="StackComponent"/> a split dialog UI.
/// </summary>
public sealed class GoobStackSystem : EntitySystem
{
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StackComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StackComponent, StackCustomSplitAmountMessage>(OnCustomSplitMessage);
    }

    private void OnMapInit(Entity<StackComponent> ent, ref MapInitEvent args)
    {
        var data = new InterfaceData("StackCustomSplitBoundUserInterface");
        _ui.SetUi(ent.Owner, StackCustomSplitUiKey.Key, data);
    }

    // Custom stack splitting dialog
    private void OnCustomSplitMessage(Entity<StackComponent> ent, ref StackCustomSplitAmountMessage message)
    {
        var user = message.Actor;
        var amount = message.Amount;
        _stack.UserSplit(ent, user, amount);
    }
}
