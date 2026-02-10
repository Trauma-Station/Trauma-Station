using Content.Server.Polymorph.Systems;
using Content.Shared.Chat;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Content.Trauma.Shared.MartialArts.Events;

namespace Content.Trauma.Server.Knowledge;
public sealed class KnowledgeSystem : SharedKnowledgeSystem
{
    [Dependency] private readonly SharedChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CanPerformComboComponent, MartialArtsSaying>(OnMartialArtsSaying);
    }

    private void OnMartialArtsSaying(Entity<CanPerformComboComponent> ent, ref MartialArtsSaying args)
    {
        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Saying), InGameICChatType.Speak, false);
    }
}
