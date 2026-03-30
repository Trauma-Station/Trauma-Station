// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Silo;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Materials;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Silo;

public abstract class SharedSiloSystem : CommonSiloSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] protected readonly SharedDeviceLinkSystem DeviceLink = default!;
    [Dependency] protected readonly SharedMaterialStorageSystem _materialStorage = default!;

    private bool _siloEnabled;

    protected ProtoId<SourcePortPrototype> SourcePort = "MaterialSilo";
    protected ProtoId<SinkPortPrototype> SinkPort = "MaterialSiloUtilizer";

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(GoobCVars.SiloEnabled, enabled => _siloEnabled = enabled, true);

        SubscribeLocalEvent<SiloComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<SiloUtilizerComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    private void OnPortDisconnected(Entity<SiloUtilizerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != SinkPort)
            return;

        ent.Comp.Silo = null;
        Dirty(ent);
    }

    private void OnNewLink(Entity<SiloComponent> ent, ref NewLinkEvent args)
    {
        if (args.SinkPort != SinkPort || args.SourcePort != SourcePort)
            return;

        if (!TryComp(args.Sink, out SiloUtilizerComponent? utilizer))
            return;

        if (utilizer.Silo != null)
            DeviceLink.RemoveSinkFromSource(utilizer.Silo.Value, args.Sink);

        utilizer.Silo = null;

        if (TryComp(args.Sink, out MaterialStorageComponent? utilizerStorage) &&
            utilizerStorage.Storage.Count != 0 &&
            TryComp(ent, out MaterialStorageComponent? siloStorage))
        {
            foreach (var material in utilizerStorage.Storage.Keys.ToArray())
            {
                var materialAmount = utilizerStorage.Storage.GetValueOrDefault(material, 0);
                if (_materialStorage.TryChangeMaterialAmount(ent, material, materialAmount, siloStorage))
                    _materialStorage.TryChangeMaterialAmount(args.Sink, material, -materialAmount, utilizerStorage);
            }
        }

        utilizer.Silo = ent;
        Dirty(args.Sink, utilizer);
    }

    public override bool TryGetMaterialAmount(EntityUid machine, string material, out int amount)
    {
        amount = 0;
        var silo = GetSilo(machine);
        if (silo is not { } || !TryComp<MaterialStorageComponent>(silo, out var siloComp))
            return false;

        amount = siloComp.Storage.GetValueOrDefault(material, 0);
        return true;
    }

    public override bool TryGetTotalMaterialAmount(EntityUid machine, out int amount)
    {
        amount = 0;
        var silo = GetSilo(machine);
        if (silo is not { } || !TryComp<MaterialStorageComponent>(silo, out var siloComp))
            return false;

        amount = siloComp.Storage.Values.Sum();
        return true;
    }

    public override void DirtySilo(EntityUid machine)
    {
        var silo = GetSilo(machine);
        if (silo is not { } || !TryComp<MaterialStorageComponent>(silo, out var siloComp))
            return;
        Dirty(silo.Value, siloComp);
    }

    public override EntityUid? GetSilo(EntityUid machine)
    {
        if (!_siloEnabled)
            return null;

        if (!TryComp(machine, out SiloUtilizerComponent? utilizer))
            return null;

        if (!TryComp(utilizer.Silo, out MaterialStorageComponent? storage))
            return null;

        return utilizer.Silo.Value;
    }
}
