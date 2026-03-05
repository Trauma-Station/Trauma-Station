// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Contains no extra data and signifies to knowledge that whatever item its attached to is "easy to use".
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EasyToUseComponent : Component;
