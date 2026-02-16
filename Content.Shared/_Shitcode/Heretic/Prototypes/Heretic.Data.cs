// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic.Prototypes;

[DataDefinition]
public sealed partial class EventHereticAscension : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticRerollTargets : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticUpdateTargets : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticResolveStarGazer : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticAddKnowledge : EntityEventArgs
{
    [DataField(required: true)]
    public List<ProtoId<HereticKnowledgePrototype>> Knowledge;
}
