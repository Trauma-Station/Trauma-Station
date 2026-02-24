// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Targeting;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Targeting;

/// <summary>
/// Message sent by the client to change its mob's targeted part.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangeTargetMessage(TargetBodyPart part): EntityEventArgs
{
    public readonly TargetBodyPart BodyPart = part;
}
