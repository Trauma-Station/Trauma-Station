using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;

namespace Content.Shared.Execution;

public sealed partial class SharedExecutionSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private void Decapitation(EntityUid victim)
    {
        if (!TryComp<BodyComponent>(victim, out var body))
            return;

        var ent = (victim, body);
        if (_body.FindPart(ent, BodyPartType.Head) is not {} head || _body.FindPart(ent, BodyPartType.Chest) is not {} chest)
            return;

        _wound.AmputateWoundable(chest, head);
    }
}
