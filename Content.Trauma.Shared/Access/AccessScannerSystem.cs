// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Access;

public sealed partial class AccessScannerSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDeviceLinkSystem _device = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private TimeSpan _updateDelay = TimeSpan.FromSeconds(0.2);
    private TimeSpan _nextUpdate;
    private HashSet<Entity<IdCardComponent>> _ids = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessScannerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AccessScannerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AccessScannerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AccessScannerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + _updateDelay;
        var query = EntityQueryEnumerator<AccessScannerComponent, AccessReaderComponent>();
        while (query.MoveNext(out var uid, out var comp, out var reader))
        {
            if (!_power.IsPowered(uid))
                return;

            var range = comp.Settings[comp.Setting].Range;
            var coords = Transform(uid).Coordinates;
            _ids.Clear();
            _lookup.GetEntitiesInRange(coords, range, _ids);
            foreach (var id in _ids)
            {
                // id may have just entered range, check its access now
                if (!_access.IsAllowed(id, uid, reader))
                    continue;

                if (!comp.Scanned.Add(id))
                    continue; // has access but was already in range

                // ping
                UpdateActive((uid, comp));
                if (id.Comp.FullName is { } name)
                    SendString(uid, comp.NamePort, name);
                if (id.Comp.LocalizedJobTitle is { } job)
                    SendString(uid, comp.JobPort, job);
            }

            var removed = comp.Scanned.RemoveWhere(id =>
            {
                if (TerminatingOrDeleted(id))
                    return true; // lol

                if (!_transform.InRange(coords, Transform(id).Coordinates, range))
                    return true; // moved out of range

                return !_access.IsAllowed(id, uid, reader); // access of the scanner or id was changed
            });
            if (removed > 0)
                UpdateActive((uid, comp)); // at least 1 id left range
        }
    }

    private void OnInit(Entity<AccessScannerComponent> ent, ref ComponentInit args)
    {
        _device.EnsureSourcePorts(ent.Owner, ent.Comp.ActivePort, ent.Comp.NamePort, ent.Comp.JobPort);
        var power = ent.Comp.Settings[ent.Comp.Setting].Power;
        _power.SetLoad(ent.Owner, power);
    }

    private void OnExamined(Entity<AccessScannerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var range = ent.Comp.Settings[ent.Comp.Setting].Range;
        args.PushMarkup($"Its range is set to [bold]{range}m[/bold]");
    }

    private void OnInteractUsing(Entity<AccessScannerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.SettingTool))
            return;

        args.Handled = true;
        ent.Comp.Setting++;
        ent.Comp.Setting %= ent.Comp.Settings.Count;
        Dirty(ent);
        var setting = ent.Comp.Settings[ent.Comp.Setting];
        _popup.PopupClient($"You set the scanner to {setting.Range}m range", ent, args.User);
        _audio.PlayPredicted(ent.Comp.CycleSound, ent, args.User);
        _power.SetLoad(ent.Owner, setting.Power);
    }

    private void OnPowerChanged(Entity<AccessScannerComponent> ent, ref PowerChangedEvent args)
    {
        // interrupt active signal while unpowered
        _device.SendSignal(ent.Owner, ent.Comp.ActivePort, ent.Comp.Active && args.Powered);
    }

    private void SendString(EntityUid uid, [ForbidLiteral] string port, string value)
    {
        var data = new NetworkPayload();
        data["logic_string"] = value;
        _device.InvokePort(uid, port, data);
    }

    private void UpdateActive(Entity<AccessScannerComponent> ent)
    {
        var active = ent.Comp.Scanned.Count > 0;
        if (ent.Comp.Active == active)
            return;

        ent.Comp.Active = active;
        _device.SendSignal(ent.Owner, ent.Comp.ActivePort, active);
    }
}
