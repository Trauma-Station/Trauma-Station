// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Shared.Popups;
using Content.Trauma.Client.Knowledge.UI;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Client.UserInterface.Controls;

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
        if (GetActiveMartialArt(ent) is not { } skill ||
            !TryComp<CanPerformComboComponent>(skill, out var combo))
            return;

        args.AttackTypes = combo.LastAttacks;
    }

    private void EnsureKnowledgeTab(CharacterWindow window)
    {
        _activeWindow = new WeakReference<CharacterWindow>(window);

        SkillTab? skillTab = null;
        foreach (var child in window.Tabs.Children)
        {
            if (child is SkillTab)
            {
                skillTab = (SkillTab) child;
                break;
            }
        }

        AttributeTab? attributeTab = null;
        foreach (var child in window.Tabs.Children)
        {
            if (child is AttributeTab)
            {
                attributeTab = (AttributeTab) child;
                break;
            }
        }

        TabContainer.SetTabTitle(window.CharacterTab, Loc.GetString("trauma-character-title"));

        if (skillTab == null)
        {
            skillTab = new SkillTab();
            window.Tabs.AddChild(skillTab);
        }

        if (attributeTab == null)
        {
            attributeTab = new AttributeTab();
            window.Tabs.AddChild(attributeTab);
        }

        if (_player.LocalEntity is { } player)
        {
            skillTab.UpdateSkillTab(player);
            attributeTab.UpdateAttributeTab(player);
        }
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
    public List<(EntityUid, EntProtoId, string)> GetMartialArtsForClientDoohickey(EntityUid target)
    {
        if (GetSkillWith<MartialArtsSkillComponent>(target) is not { } arts)
            return [];

        var list = new List<(EntityUid, EntProtoId, string)>();
        foreach (var art in arts)
        {
            list.Add((art, Prototype(art)!.ID, Name(art)));
        }
        list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        return list;
    }

    public List<(ProtoId<SkillCategoryPrototype> Category, SkillInfo Info)>? GrabAllSkills(EntityUid target)
    {
        var knowledgeList = TryGetAllSkillUnits(target);

        if (knowledgeList is not { } || knowledgeList.Count == 0)
            return null;

        return knowledgeList
            .Select(ent => GetSkillInfo(ent))
            .OrderBy(data => data.Category)
            .ThenBy(data => data.Info.Name)
            .ToList();
    }

    public List<(int Order, AttributeInfo Info)>? GrabAllAttributes(EntityUid target)
    {
        var knowledgeList = TryGetAllAttributeUnits(target);

        if (knowledgeList is not { } || knowledgeList.Count == 0)
            return null;

        return knowledgeList
            .Select(ent => GetAttributeInfo(ent))
            .OrderBy(data => data.Order)
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
        if (_player.LocalEntity is { } player)
            _popup.PopupEntity(args.Popup, player, player, PopupType.Small);
    }

    public EntProtoId? GetEntProtoId(Entity<MartialArtsSkillComponent>? martialArt)
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
