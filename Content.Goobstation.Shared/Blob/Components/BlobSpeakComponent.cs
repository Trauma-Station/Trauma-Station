// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlobSpeakComponent : Component
{
    [DataField]
    public ProtoId<LanguagePrototype> Language = "Blob";
}
