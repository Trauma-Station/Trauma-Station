// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Trauma.Shared.VentCrawling.Components;

/// <summary>
/// A component representing a vent that you can crawl through
/// </summary>
[RegisterComponent]
public sealed partial class VentCrawlerTubeComponent : Component
{
    [DataField]
    public string ContainerId = "VentCrawlerTube";

    [DataField]
    public bool Connected = true;

    [ViewVariables]
    public Container Contents = default!;
}

[ByRefEvent]
public record struct GetVentCrawlingsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
