// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Goobstation.Wizard.SpellCards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SpellCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public bool Targeted;

    [DataField, AutoNetworkedField]
    public bool Flipped;

    [DataField]
    public float TargetedSpeed = 20f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan FlipAt = TimeSpan.Zero;

    [DataField]
    public TimeSpan TimeToFlip = TimeSpan.FromMilliseconds(400);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromMilliseconds(100);

    [DataField]
    public float Tolerance = 0.1f;

    [DataField]
    public Color FlippedTrailColor = Color.White;
}

[Serializable, NetSerializable]
public enum SpellCardVisuals : byte
{
    State // 0 - not flipped, 1 - flipping, 2 - flipped
}
