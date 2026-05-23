// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;

namespace Content.Medical.Shared.Restrict;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RestrictInteractionByUserTagComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Contains = [];

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> DoesntContain = [];

    [DataField, AutoNetworkedField]
    public List<string> Messages = [];
}
