// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Server.Lathe;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;

namespace Content.Goobstation.Server.Lathe;

public sealed partial class LatheAutomationSystem : EntitySystem
{
    [Dependency] private LatheSystem _lathe = default!;
    [Dependency] private DeviceLinkSystem _device = default!;

    [SubscribeLocalEvent]
    private void OnStartPrinting(Entity<LatheAutomationComponent> ent, ref LatheStartPrintingEvent args)
    {
        SetRecipe(ent, args.Recipe);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<LatheAutomationComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.PrintPort)
        {
            if (ent.Comp.LastRecipe is not {} recipe)
                return;

            // ignore low signals
            var state = SignalState.Momentary;
            args.Data?.TryGetValue("logic_state", out state);
            if (state == SignalState.Low)
                return;

            _lathe.TryAddToQueue(ent.Owner, recipe, quantity: ent.Comp.Quantity);
            _lathe.TryStartProducing(ent.Owner); // Won't do anything otherwise
        }
        else if (args.Port == ent.Comp.SetRecipePort)
        {
            if (args.Data is not { } data ||
                !data.TryGetValue<string>("logic_string", out var id))
                return;

            // invalid ids will reset it to null
            // lathe system checks if the recipe is allowed on this lathe in CanProduce, don't need to check it here
            ProtoMan.TryIndex<LatheRecipePrototype>(id, out var recipe);
            SetRecipe(ent, recipe);
        }
        else if (args.Port == ent.Comp.QuantityPort)
        {
            if (args.Data is not { } data ||
                !data.TryGetValue<int>("logic_int", out var quantity) ||
                quantity < 1)
                return;

            ent.Comp.Quantity = quantity;
        }
    }

    private void SetRecipe(Entity<LatheAutomationComponent> ent, LatheRecipePrototype? recipe)
    {
        if (ent.Comp.LastRecipe == recipe)
            return;

        ent.Comp.LastRecipe = recipe;
        var payload = new NetworkPayload()
        {
            ["logic_string"] = recipe?.ID ?? string.Empty
        };
        _device.InvokePort(ent.Owner, ent.Comp.CurrentRecipePort, payload);
    }
}
