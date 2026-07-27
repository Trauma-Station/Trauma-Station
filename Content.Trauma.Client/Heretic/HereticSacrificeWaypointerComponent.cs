// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Heretic;

[RegisterComponent]
public sealed partial class HereticSacrificeTargetComponent : Component
{
    public TimeSpan RemovalTimer;

    public TimeSpan RemovalTime = TimeSpan.FromSeconds(2);
}
