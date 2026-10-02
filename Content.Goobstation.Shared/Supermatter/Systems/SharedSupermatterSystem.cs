// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Content.Goobstation.Shared.Supermatter.Components;
using Robust.Shared.GameObjects;

namespace Content.Goobstation.Shared.Supermatter.Systems;

public abstract partial class SharedSupermatterSystem : EntitySystem
{
    public enum SuperMatterSound : sbyte
    {
        Aggressive = 0,
        Delam = 1
    }

    public enum DelamType : sbyte
    {
        Explosion = 0,
        Singulo = 1,
        Tesla = 2,
        Cascade = 3 // save for later
    }
}
