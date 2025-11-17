// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Trigger.Effects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RedialUserOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public string Address = string.Empty;
}
