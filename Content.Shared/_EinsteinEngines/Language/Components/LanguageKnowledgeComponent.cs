// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._EinsteinEngines.Language.Components;

/// <summary>
///     Stores data about entities' intrinsic language knowledge.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedLanguageSystem))]
[AutoGenerateComponentState]
public sealed partial class LanguageKnowledgeComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    ///     List of languages this entity can speak without any external tools.
    /// </summary>
    [DataField("speaks", required: true)]
    [AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand without any external tools.
    /// </summary>
    [DataField("understands", required: true)]
    [AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();
}
