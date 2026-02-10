// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Administration.Commands;

[AdminCommand(AdminFlags.Spawn)]
public sealed class EquipTo : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public const string CommandName = "equipto";
    public override string Command => CommandName;

    public static readonly ProtoId<InventoryTemplatePrototype> HumanTemplate = "human";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var invSystem = _entMan.System<InventorySystem>();

        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("cmd-equipto-args-error"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetNet)
            || !_entMan.TryGetEntity(targetNet, out var targetEntity))
        {
            shell.WriteLine(Loc.GetString("cmd-equipto-bad-target", ("target", args[0])));
            return;
        }
        var target = targetEntity.Value;

        EntityUid item;
        if (NetEntity.TryParse(args[1], out var itemNet) &&
            _entMan.TryGetEntity(itemNet, out var itemEntity))
        {
            item = itemEntity.Value;
        }
        else if (_proto.TryIndex(args[1], out var prototype))
        {
            item = _entMan.SpawnEntity(prototype.ID, _entMan.GetComponent<TransformComponent>(target).Coordinates);
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-equipto-bad-proto", ("proto", args[1])));
            return;
        }

        if (!bool.TryParse(args[2], out var deletePrevious))
            return;

        if (args.Length >= 4)
        {
            var targetSlot = args[3];

            invSystem.TryGetSlotEntity(target, targetSlot, out var existing);
            if (invSystem.TryEquip(target, item, targetSlot, force: true, silent: true))
            {
                if (deletePrevious
                    && existing != null)
                    _entMan.DeleteEntity(existing.Value);

                shell.WriteLine(Loc.GetString("cmd-equipto-success",
                    ("item", _entMan.ToPrettyString(item)),
                    ("target", _entMan.ToPrettyString(target)),
                    ("targetSlot", targetSlot)));
            }
            else
            {
                shell.WriteLine(Loc.GetString("cmd-equipto-failure",
                    ("item", _entMan.ToPrettyString(item)),
                    ("target", _entMan.ToPrettyString(target)),
                    ("targetSlot", targetSlot)));
            }
            return;
        }

        var equipped = false;
        if (invSystem.TryGetSlots(target, out var slots)
            && _entMan.TryGetComponent<ClothingComponent>(item, out var clothingComponent))
        {
            foreach (var slot in slots)
            {
                if (!clothingComponent.Slots.HasFlag(slot.SlotFlags))
                    continue;

                if (deletePrevious
                    && invSystem.TryGetSlotEntity(target, slot.Name, out var existing))
                    _entMan.DeleteEntity(existing.Value);
                else
                    invSystem.TryUnequip(target, slot.Name, true, true);

                invSystem.TryEquip(target, item, slot.Name, force: true, silent: true);

                if (slot.Name == "id" &&
                    _entMan.TryGetComponent(item, out PdaComponent? pdaComponent) &&
                    _entMan.TryGetComponent<IdCardComponent>(pdaComponent.ContainedId, out var id))
                {
                    id.FullName = _entMan.GetComponent<MetaDataComponent>(target).EntityName;
                }

                shell.WriteLine(Loc.GetString("cmd-equipto-success",
                    ("item", _entMan.ToPrettyString(item)),
                    ("target", _entMan.ToPrettyString(target)),
                    ("targetSlot", slot.Name)));

                equipped = true;
                break;
            }
        }

        if (equipped)
            return;

        shell.WriteLine(Loc.GetString("cmd-equipto-total-failure",
            ("item", _entMan.ToPrettyString(item)),
            ("target", _entMan.ToPrettyString(target))));

        _entMan.DeleteEntity(item);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 4
            || !_proto.Resolve(HumanTemplate, out var inventoryTemplate))
            return CompletionResult.Empty;

        var options = inventoryTemplate.Slots.Select(c => c.Name).OrderBy(c => c).ToArray();
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-equipto-hint"));
    }
}
