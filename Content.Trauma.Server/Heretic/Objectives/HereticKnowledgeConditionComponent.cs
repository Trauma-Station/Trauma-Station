// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Heretic.Objectives;

[RegisterComponent]
public sealed partial class HereticKnowledgeConditionComponent : Component
{
    [DataField]
    public float Researched;
}
