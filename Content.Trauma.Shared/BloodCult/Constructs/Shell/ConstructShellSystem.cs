// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Trauma.Common.RadialSelector;
using Content.Trauma.Shared.BloodCult.Constructs.SoulShard;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.BloodCult.Constructs.Shell;

public sealed partial class ConstructShellSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<ConstructShellComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var shell = ent.Owner;
        if (_ui.IsUiOpen(shell, RadialSelectorUiKey.Key))
            return;

        var user = args.User;
        var blessed = false;

        // only the shell or a shard inside it can use the verb
        if (user != shell)
        {
            if (_slots.GetItemOrNull(shell, ent.Comp.ShardSlotId) is not { } shard ||
                shard != user ||
                !TryComp<SoulShardComponent>(shard, out var shardComp))
                return;

            blessed = shardComp.IsBlessed;
        }

        var items = blessed ? ent.Comp.PurifiedConstructs : ent.Comp.Constructs;
        args.Verbs.Add(new ExamineVerb
        {
            DoContactInteraction = true,
            Text = "Select form",
            Icon = new SpriteSpecifier.Rsi(
                new("/Textures/_Trauma/BloodCult/Entities/Items/construct_shell.rsi"), "icon"),
            Act = () =>
            {
                _ui.SetUiState(shell, RadialSelectorUiKey.Key, new RadialSelectorState(items));
                _ui.TryToggleUi(shell, RadialSelectorUiKey.Key, user);
            }
        });
    }

    [SubscribeLocalEvent]
    private void OnShellInit(Entity<ConstructShellComponent> shell, ref ComponentInit args)
    {
        _slots.AddItemSlot(shell.Owner, shell.Comp.ShardSlotId, shell.Comp.ShardSlot);
    }

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<ConstructShellComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        var item = args.Item;
        var shell = ent.Owner;
        if (args.Slot.ID != ent.Comp.ShardSlotId ||
            !TryComp(item, out SoulShardComponent? soulShard) ||
            _ui.IsUiOpen(shell, RadialSelectorUiKey.Key))
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp<MindContainerComponent>(item, out var mindContainer) || !mindContainer.HasMind)
        {
            _popup.PopupEntity("The shard has no soul.", ent, args.User);
            args.Cancelled = true;
            return;
        }

        _slots.SetLock(shell, ent.Comp.ShardSlotId, true);
        _ui.SetUiState(shell,
            RadialSelectorUiKey.Key,
            new RadialSelectorState(soulShard.IsBlessed ? ent.Comp.PurifiedConstructs : ent.Comp.Constructs));

        _ui.TryToggleUi(shell, RadialSelectorUiKey.Key, item);
    }

    [SubscribeLocalEvent]
    private void OnConstructSelected(Entity<ConstructShellComponent> shell, ref RadialSelectorSelectedMessage args)
    {
        if (!_mind.TryGetMind(args.Actor, out var mindId, out var mind))
            return;

        _ui.CloseUi(shell.Owner, RadialSelectorUiKey.Key);
        var coords = Transform(shell).Coordinates;
        var construct = PredictedSpawnAtPosition(args.SelectedItem, coords);
        _mind.TransferTo(mindId, construct, mind: mind);
        // TODO: unvisit or something??? set this as original entity??
        PredictedDel(shell.Owner);
    }

    [SubscribeLocalEvent]
    private void OnShellRemove(Entity<ConstructShellComponent> shell, ref ComponentRemove args)
    {
        _slots.RemoveItemSlot(shell.Owner, shell.Comp.ShardSlot);
    }
}
