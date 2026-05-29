// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Robust.Shared.GameObjects;

namespace Content.Goobstation.Common.Actions;

[Serializable, NetSerializable]
public sealed class LoadActionsEvent(NetEntity entity) : EntityEventArgs
{
    public NetEntity Entity = entity;
}
