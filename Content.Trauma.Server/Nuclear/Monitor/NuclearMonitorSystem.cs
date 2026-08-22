// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Nuclear;
using Content.Trauma.Shared.Nuclear.Monitor;

namespace Content.Trauma.Server.Nuclear.Monitor;

public sealed partial class NuclearMonitorSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _device = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<DeviceLinkSourceComponent> _sourceQuery = default!;
    [Dependency] private EntityQuery<NuclearMonitorComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearMonitorComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<NuclearMonitorComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<NuclearMonitorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnNewLink(Entity<NuclearMonitorComponent> ent, ref NewLinkEvent args)
    {
        if (args.SinkPort != ent.Comp.LinkingPort || _whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Source))
            return;

        ent.Comp.Linked = args.Source;
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<NuclearMonitorComponent> ent, ref PortDisconnectedEvent args)
    {
        if (ent.Comp.Linked == null || args.Port != ent.Comp.LinkingPort)
            return;

        ent.Comp.Linked = null;
        Dirty(ent);
    }

    private void OnAnchorChanged(Entity<NuclearMonitorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            CheckRange(ent.AsNullable());
    }

    /// <summary>
    /// Get the machine linked to a monitor.
    /// </summary>
    public EntityUid? GetLinked(EntityUid monitor)
        => _query.CompOrNull(monitor)?.Linked;

    /// <summary>
    /// Unlink a monitor if it's outside the linked machine's range.
    /// </summary>
    public void CheckRange(Entity<NuclearMonitorComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp) ||
            ent.Comp.Linked is not { } linked ||
            !_sourceQuery.TryComp(linked, out var source) ||
            _transform.InRange(ent.Owner, linked, source.Range))
            return;

        var key = Comp<ActivatableUIComponent>(ent).Key!;
        _ui.CloseUi(ent.Owner, key);
        ent.Comp.Linked = null;
        Dirty(ent);
        _device.RemoveSinkFromSource(linked, ent, source);
    }

    /// <summary>
    /// Relay a BUI message to the linked machine.
    /// </summary>
    public void RelayMessage(EntityUid uid, NuclearMonitorComponent comp, NuclearMachineBUIMessage args)
    {
        if (comp.Linked is { } linked)
        {
            args.Monitor = uid;
            RaiseLocalEvent(linked, args);
        }
    }
}
