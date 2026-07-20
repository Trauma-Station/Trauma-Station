// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Blob;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.GameTicking;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Player;

namespace Content.Goobstation.Client.Blob;

public sealed partial class BlobObserverSystem : SharedBlobObserverSystem
{
    [Dependency] private ILightManager _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobObserverComponent, GetStatusIconsEvent>(OnShowBlobIcon);
        SubscribeLocalEvent<ZombieBlobComponent, GetStatusIconsEvent>(OnShowBlobIcon);
        SubscribeLocalEvent<BlobCarrierComponent, GetStatusIconsEvent>(OnShowBlobIcon);
        SubscribeLocalEvent<BlobbernautComponent, GetStatusIconsEvent>(OnShowBlobIcon);
    }

    private static readonly ProtoId<FactionIconPrototype> BlobFaction = "BlobFaction";

    private void OnShowBlobIcon<T>(Entity<T> ent, ref GetStatusIconsEvent args) where T : Component
    {
        args.StatusIcons.Add(ProtoMan.Index<FactionIconPrototype>(BlobFaction));
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<BlobObserverComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _light.DrawLighting = false;
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<BlobObserverComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _light.DrawLighting = true;
    }

    [SubscribeNetworkEvent]
    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _light.DrawLighting = true;
    }
}
