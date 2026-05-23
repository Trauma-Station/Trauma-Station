// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Traumas;

public sealed partial class TraumaSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AlertsSystem _alert = default!;

    private static readonly ProtoId<AlertPrototype> _brokenBonesAlertId = "BrokenBones";

    public override void Initialize()
    {
        base.Initialize();
        InitProcess();
        InitBones();
        InitOrgans();
    }
}
