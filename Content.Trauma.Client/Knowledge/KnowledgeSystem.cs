// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Trauma.Client.Knowledge.UI;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Knowledge;

public sealed class KnowledgeSystem : SharedKnowledgeSystem
{
    private WeakReference<CharacterWindow>? _activeWindow;
    private bool _showPopups;
    private TimeSpan _nextPopup;
    private TimeSpan _popupCooldown = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetPerformedAttackTypesEvent>(OnGetAttackTypes);
        SubscribeLocalEvent<KnowledgeHolderComponent, UpdateExperienceEvent>(OnUpdateExperienceEvent);
        Subs.CVar(_cfg, TraumaCVars.SkillPopups, x => _showPopups = x, true);
        SubscribeAllEvent<SkillPopupEvent>(OnSkillPopup);

        CharacterWindow.OnOpened += EnsureKnowledgeTab;
        LobbyUIController.OnProfileEditorCreated += AddProfileEditorTab;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CharacterWindow.OnOpened -= EnsureKnowledgeTab;
        LobbyUIController.OnProfileEditorCreated -= AddProfileEditorTab;
    }

    private void OnGetAttackTypes(Entity<KnowledgeHolderComponent> ent, ref GetPerformedAttackTypesEvent args)
    {
        if (GetActiveMartialArt(ent) is not {} skill ||
            !TryComp<CanPerformComboComponent>(skill, out var combo))
            return;

        args.AttackTypes = combo.LastAttacks;
    }

    private void EnsureKnowledgeTab(CharacterWindow window)
    {
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

        if (_player.LocalEntity is {} player)
            knowledgeTab.UpdateKnowledgeTab(player);
    }

    private void AddProfileEditorTab(HumanoidProfileEditor editor)
    {
        // place it before markings tab
        var above = editor.MarkingsTab;
        var index = above.GetPositionInParent();

        var tab = new KnowledgeProfileEditor(_proto, this);
        tab.OnSave += knowledge =>
        {
            editor.Profile = editor.Profile?.WithKnowledge(knowledge);
            editor.IsDirty = true;
        };

        editor.OnSetProfile += profile =>
        {
            if (profile is not null)
                tab.SetProfile(profile.Species, profile.Knowledge);
        };
        editor.TabContainer.AddChild(tab);
        tab.SetPositionInParent(index);
        TabContainer.SetTabTitle(tab, Loc.GetString("knowledge-editor-tab"));
    }

    /// <summary>
    /// Returns the martial arts that a knowledge entity has, along with some helper data for the client.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public List<(EntityUid, EntProtoId, string)> GetMartialArtsForClientDoohickey(EntityUid target)
    {
        if (GetKnowledgeWith<MartialArtsKnowledgeComponent>(target) is not {} arts)
            return [];

        var list = new List<(EntityUid, EntProtoId, string)>();
        foreach (var art in arts)
        {
            list.Add((art, Prototype(art)!.ID, Name(art)));
        }
        list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        return list;
    }

    public List<(ProtoId<KnowledgeCategoryPrototype> Category, KnowledgeInfo Info)>? GrabAllKnowledge(EntityUid target)
    {
        var knowledgeList = TryGetAllKnowledgeUnits(target);

        if (knowledgeList is not { } || knowledgeList.Count == 0)
            return null;

        return knowledgeList
            .Select(ent => GetKnowledgeInfo(ent))
            .OrderBy(data => data.Category)
            .ThenBy(data => data.Info.Name)
            .ToList();
    }

    public void OnUpdateExperienceEvent(Entity<KnowledgeHolderComponent> ent, ref UpdateExperienceEvent args)
    {
        var localPlayer = _player.LocalEntity;
        if (localPlayer != ent.Owner)
            return;

        if (_activeWindow is not { } || !_activeWindow.TryGetTarget(out var window))
            return;

        EnsureKnowledgeTab(window);
    }

    private void OnSkillPopup(SkillPopupEvent args)
    {
        if (!_showPopups)
            return;

        var now = _timing.CurTime;
        if (now < _nextPopup)
            return;

        _nextPopup = now + _popupCooldown;
        _popup.PopupClient(args.Popup, _player.LocalEntity);
    }

    public EntProtoId? GetEntProtoId(Entity<MartialArtsKnowledgeComponent>? martialArt)
    {
        if (martialArt is not { } martialArtTrue)
            return null;

        return Prototype(martialArtTrue.Owner)?.ID;
    }

    /// <summary>
    /// Changes the active martial art of the player.
    /// </summary>
    public void ChangeMartialArt(EntProtoId? id)
    {
        RaisePredictiveEvent(new KnowledgeUpdateMartialArtsEvent(id));
    }
}
