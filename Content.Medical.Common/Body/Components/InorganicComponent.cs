// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Common.Body;

/// <summary>
/// An inorganic organ/bodypart.
/// Used for some damage splitting types e.g. in reagents.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InorganicComponent : Component;
