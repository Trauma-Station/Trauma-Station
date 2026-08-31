// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Server.Lathe;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Trauma.Common.DeviceLinking;

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
            TryPrintLast(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalStateReceived(Entity<LatheAutomationComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (args.Data.State == SignalState.Low || args.Port == ent.Comp.PrintPort)
            TryPrintLast(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalIntReceived(Entity<LatheAutomationComponent> ent, ref SignalReceivedEvent<LogicIntPayload> args)
    {
        if (args.Port != ent.Comp.QuantityPort || args.Data.Value < 1)
            return;

        ent.Comp.Quantity = args.Data.Value;
    }

    [SubscribeLocalEvent]
    private void OnSignalStringReceived(Entity<LatheAutomationComponent> ent, ref SignalReceivedEvent<LogicStringPayload> args)
    {
        if (args.Port != ent.Comp.SetRecipePort)
            return;

        // invalid ids will reset it to null
        // lathe system checks if the recipe is allowed on this lathe in CanProduce, don't need to check it here
        ProtoMan.TryIndex<LatheRecipePrototype>(args.Data.Value, out var recipe);
        SetRecipe(ent, recipe);
    }

    private void SetRecipe(Entity<LatheAutomationComponent> ent, LatheRecipePrototype? recipe)
    {
        if (ent.Comp.LastRecipe == recipe)
            return;

        ent.Comp.LastRecipe = recipe;
        var payload = new LogicStringPayload(recipe?.ID ?? string.Empty);
        _device.InvokePort(ent.Owner, ent.Comp.CurrentRecipePort, ref payload);
    }

    private void TryPrintLast(Entity<LatheAutomationComponent> ent)
    {
        if (ent.Comp.LastRecipe is not {} recipe)
            return;

        _lathe.TryAddToQueue(ent.Owner, recipe, quantity: ent.Comp.Quantity);
        _lathe.TryStartProducing(ent.Owner); // Won't do anything otherwise
    }
}
