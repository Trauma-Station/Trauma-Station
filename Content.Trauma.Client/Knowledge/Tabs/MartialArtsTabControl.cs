using Content.Client._Shitcode.UserActions.Tabs;
using Content.Goobstation.UIKit.UserActions.Controls;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Knowledge.Tabs;

public sealed partial class MartialArtsTabControl : BaseTabControl
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly CommonKnowledgeSystem _commonKnowledge = default!;

    private GridContainer MartialArtsList => FindControl<GridContainer>("MartialArtsList");
    public MartialArtsTabControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public override bool UpdateState()
    {
        MartialArtsList.RemoveAllChildren();

        var player = _playerManager.LocalEntity;
        if (player is not { Valid: true })
            return false;

        if (_commonKnowledge.TryGetKnowledgeEntity(player.Value) is not { } knowledgeEntity)
            return false;

        var martialArts = _commonKnowledge.TryGetKnowledgeWithComp<MartialArtsKnowledgeComponent>(knowledgeEntity);

        var button = CreateMartialArtsButton(knowledgeEntity, null);
        MartialArtsList.AddChild(button);

        if (martialArts is { })
        {
            foreach (var martialArt in martialArts)
            {
                button = CreateMartialArtsButton(knowledgeEntity, martialArt);
                MartialArtsList.AddChild(button);
            }
        }
        return true;
    }


    private IconButton CreateMartialArtsButton(EntityUid knowledgeEntity, Entity<MartialArtsKnowledgeComponent>? martialArt)
    {
        string locString = "no-martial-art";
        if (martialArt is { } martialArtConfirmed)
            locString = martialArtConfirmed.ToString();

        var button = new IconButton(Loc.GetString(locString));
        //button.Icon.Texture = martialArt.Comp.Icon;
        button.OnPressed += _ => OnChangeMartialArts(knowledgeEntity, martialArt);

        return button;
    }

    private void OnChangeMartialArts(EntityUid knowledgeEntity, Entity<MartialArtsKnowledgeComponent>? martialArt)
    {
        _commonKnowledge.ChangeMartialArts(knowledgeEntity, martialArt);
    }

    protected override void Resized()
    {
    }
}
