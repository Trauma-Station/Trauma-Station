// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Items;
using Content.Trauma.Shared.Blink;

namespace Content.Trauma.Client.Blink;

public sealed class BlinkSystem : SharedBlinkSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<BlinkComponent>(ent => new BlinkStatusControl(ent));
    }
}
