// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.EinsteinEngines.Common.Language;
using Robust.Shared.Prototypes;

namespace Content.EinsteinEngines.Server.Language;

[RegisterComponent]
public sealed partial class TowerOfBabelComponent : Component
{
    [DataField]
    public ProtoId<LanguagePrototype> DefaultLanguage = "TauCetiBasic";
}
