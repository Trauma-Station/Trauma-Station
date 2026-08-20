// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;

namespace Content.Trauma.Shared.Spy.Ui;

[Serializable, NetSerializable]
public sealed class SpyRewardSelectedMessage(string id, ProtoId<ListingPrototype> listing) : BoundUserInterfaceMessage
{
    public string Id = id;

    public ProtoId<ListingPrototype> Listing = listing;
}

[Serializable, NetSerializable]
public enum SpyUplinkUiKey : byte
{
    Key,
}
