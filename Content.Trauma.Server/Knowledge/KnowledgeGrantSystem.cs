using Content.Shared.Interaction.Events;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Server.Knowledge;

public sealed partial class KnowledgeGrantSystem : SharedKnowledgeGrantSystem
{
    protected override void OnActivate(Entity<KnowledgeGrantOnUseComponent> ent, EntityUid user, BoundUserInterface window)
    {
        // Do nothing, client will handle the UI and send server a message when the user performs a rep.
    }
}
