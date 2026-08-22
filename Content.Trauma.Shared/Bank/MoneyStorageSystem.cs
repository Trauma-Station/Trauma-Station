// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Trauma.Common.Bank;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Bank;

public sealed partial class MoneyStorageSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBankSystem _bank = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _component = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoneyStorageComponent, InteractUsingEvent>(OnInsertMoney);
        SubscribeLocalEvent<MoneyStorageComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<MoneyStorageComponent, VendingMachineVendAttemptEvent>(OnVend);
    }

    private void OnInsertMoney(Entity<MoneyStorageComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency) || !TryComp<StackComponent>(args.Used, out var stack))
        {
            _popup.PopupPredicted($"{Name(args.Used)} is not a valid currency.", args.User, args.User, PopupType.Medium);
            return;
        }

        args.Handled = true;
        _bank.InsertMoney(ent, (args.Used, currency, stack));
        _audio.PlayPvs(ent.Comp.SoundOnInsertMoney, ent);
    }

    private void OnDestroyed(Entity<MoneyStorageComponent> ent, ref DestructionEventArgs args)
    {
        _bank.PrintMoney(ent, ent.Comp.StoredMoney + ent.Comp.MoneyBuffer, _proto.Index(ent.Comp.Currency));
    }

    private void OnVend(Entity<MoneyStorageComponent> ent, ref VendingMachineVendAttemptEvent args)
    {
        if (!_proto.TryIndex<EntityPrototype>(args.ItemId, out var proto) || !proto.TryGetComponent<SellPriceComponent>(out var sellPriceComp, _component) || sellPriceComp.Price >= ent.Comp.MoneyBuffer)
        {
            args.Cancelled = true;
            args.Reason = "vending-machine-component-try-eject-no-money";
            return;
        }

        ent.Comp.StoredMoney += sellPriceComp.Price;
        ent.Comp.MoneyBuffer -= sellPriceComp.Price;
        Dirty(ent);
    }
}
