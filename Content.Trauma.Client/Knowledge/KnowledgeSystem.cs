using System.Linq;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Trauma.Client.Knowledge.Tabs;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Knowledge;

public sealed class KnowledgeSystem : SharedKnowledgeSystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private WeakReference<CharacterWindow>? _activeWindow;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetPerformedAttackTypesEvent>(OnGetAttackTypes);
        SubscribeLocalEvent<KnowledgeHolderComponent, UpdateExperience>(OnUpdateExperience);

        CharacterWindow.OnOpened += OnCharacterWindowOpened;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CharacterWindow.OnOpened -= OnCharacterWindowOpened;
    }

    private void OnGetAttackTypes(Entity<KnowledgeHolderComponent> ent, ref GetPerformedAttackTypesEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<CanPerformComboComponent>(martialArtSkillUid, out var comboComp))
            return;

        args.AttackTypes = comboComp.LastAttacks;
    }

    private void OnCharacterWindowOpened(CharacterWindow window)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        _activeWindow = new WeakReference<CharacterWindow>(window);

        KnowledgeTab? knowledgeTab = null;
        foreach (var child in window.Tabs.Children)
        {
            if (child is KnowledgeTab)
            {
                knowledgeTab = (KnowledgeTab) child;
                break;
            }
        }

        TabContainer.SetTabTitle(window.CharacterTab, Loc.GetString("trauma-character-title"));

        if (knowledgeTab == null)
        {
            knowledgeTab = new KnowledgeTab();
            window.Tabs.AddChild(knowledgeTab);
        }

        knowledgeTab.UpdateKnowledgeTab(player.Value, knowledgeTab);
    }

    /// <summary>
    /// Returns the martial arts that a knowledge entity has, along with some helper data for the client.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public List<(EntityUid, string)> GetMartialArtsForClientDoohickey(EntityUid target)
    {
        var martialArtsList = TryGetKnowledgeWithComp<MartialArtsKnowledgeComponent>(target);

        if (martialArtsList == null)
            return new List<(EntityUid, string)>();

        return martialArtsList
            .Select(martialArt =>
            {
                var protoId = MetaData(martialArt.Owner).EntityPrototype?.ID ?? string.Empty;
                return (Uid: martialArt.Owner, ProtoId: protoId);
            })
            .OrderBy(x => x.ProtoId) // Sort alphabetically by Prototype ID
            .Select(x => (x.Uid, Loc.GetString($"knowledge-{x.ProtoId}")))
            .ToList();
    }

    public List<(string Category, KnowledgeInfo Info)>? GrabAllKnowledge(EntityUid target)
    {
        var knowledgeList = TryGetAllKnowledgeUnits(target);

        if (knowledgeList == null || knowledgeList.Count == 0)
            return null;

        return knowledgeList
        .Select(ent => GetKnowledgeInfo(ent))
        .OrderBy(data => data.Category)
        .ThenBy(data => data.Info.Name)
        .ToList();
    }

    public void OnUpdateExperience(Entity<KnowledgeHolderComponent> ent, ref UpdateExperience args)
    {
        Log.Debug("Received UpdateExperience event, checking if it pertains to the local player.");

        var localPlayer = _playerManager.LocalSession?.AttachedEntity;
        if (localPlayer != ent.Owner)
            return;

        Log.Debug($"Received experience update for {ent.Owner}");

        if (_activeWindow == null || !_activeWindow.TryGetTarget(out var window))
            return;

        Log.Debug("Active character window found, updating experience display.");

        OnCharacterWindowOpened(window);
    }
}
