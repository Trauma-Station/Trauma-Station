// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Hastur.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HasturDevouringComponent : Component;

[NetSerializable, Serializable]
public enum DevourVisuals : byte
{
    Devouring,
}
