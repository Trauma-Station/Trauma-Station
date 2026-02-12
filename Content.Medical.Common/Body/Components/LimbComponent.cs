// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Common.Body;

/// <summary>
/// A limb that can easily be severed.
/// Arms/legs/tail
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LimbComponent : Component;
