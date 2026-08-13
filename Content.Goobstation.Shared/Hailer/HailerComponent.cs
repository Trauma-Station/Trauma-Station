// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Hailer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HailerComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionHailer";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField(required: true)]
    public List<HailerMessage> Messages = default!;
}

public sealed partial class HailerActionEvent : InstantActionEvent;

[DataRecord]
public partial record struct HailerMessage(SoundSpecifier Sound, string Message);
