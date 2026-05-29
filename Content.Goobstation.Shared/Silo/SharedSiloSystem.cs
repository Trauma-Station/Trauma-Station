// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Silo;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Materials;
using Robust.Shared.Configuration;

namespace Content.Goobstation.Shared.Silo;

public abstract partial class SharedSiloSystem : CommonSiloSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] protected SharedDeviceLinkSystem DeviceLink = default!;
    [Dependency] protected SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private EntityQuery<MaterialStorageComponent> _matsQuery = default!;
    [Dependency] private EntityQuery<SiloUtilizerComponent> _utilizerQuery = default!;

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

        if (!_utilizerQuery.TryComp(args.Sink, out var utilizer))
            return;

        if (utilizer.Silo != null)
            DeviceLink.RemoveSinkFromSource(utilizer.Silo.Value, args.Sink);

        utilizer.Silo = null;

        if (_matsQuery.TryComp(args.Sink, out var utilizerStorage) &&
            utilizerStorage.Storage.Count != 0 &&
            _matsQuery.TryComp(ent, out var siloStorage))
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
        if (GetSilo(machine) is not { } silo || !_matsQuery.TryComp(silo, out var siloComp))
            return false;

        amount = siloComp.Storage.GetValueOrDefault(material, 0);
        return true;
    }

    public override bool TryGetTotalMaterialAmount(EntityUid machine, out int amount)
    {
        amount = 0;
        if (GetSilo(machine) is not { } silo || !_matsQuery.TryComp(silo, out var siloComp))
            return false;

        amount = siloComp.Storage.Values.Sum();
        return true;
    }

    public override void DirtySilo(EntityUid machine)
    {
        if (GetSilo(machine) is not { } silo || !_matsQuery.TryComp(silo, out var siloComp))
            return;
        Dirty(silo, siloComp);
    }

    public override EntityUid? GetSilo(EntityUid machine)
    {
        if (_siloEnabled &&
            _utilizerQuery.TryComp(machine, out var utilizer) &&
            utilizer.Silo is { } silo &&
            _matsQuery.HasComp(silo))
            return silo;

        return null;
    }
}
