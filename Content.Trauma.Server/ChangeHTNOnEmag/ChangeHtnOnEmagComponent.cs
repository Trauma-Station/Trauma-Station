// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC.HTN;

namespace Content.Trauma.Server.ChangeHTNOnEmag;

[RegisterComponent]
public sealed partial class ChangeHtnOnEmagComponent : Component
{
    [DataField]
    public HTNCompoundTask Task;
}
