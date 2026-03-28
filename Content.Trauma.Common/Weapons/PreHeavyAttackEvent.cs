// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Weapons;

[Serializable, NetSerializable]
public record struct PreHeavyAttackEvent(Vector2 Direction, bool Handled = false);
