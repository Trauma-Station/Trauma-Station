using Content.Client._Shitcode.UserActions.Tabs;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Common.Knowledge;
using Pidgin;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Knowledge.Tabs;

public sealed partial class KnowledgeTab : Control
{
    [Dependency] private readonly IEntitySystemManager _system = default!;
    private readonly KnowledgeSystem _knowledge;
    private readonly SpriteSystem _sprite;

    private BoxContainer KnowledgeBox => FindControl<BoxContainer>("KnowledgeBox");
    private Placeholder KnowledgePlaceholder => FindControl<Placeholder>("KnowledgePlaceholder");

    public KnowledgeTab()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _knowledge = _system.GetEntitySystem<KnowledgeSystem>();
        _sprite = _system.GetEntitySystem<SpriteSystem>();
    }

    /// <summary>
    /// Updates the specificied knowledge tab with the player's current martial arts knowledge.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="knowledgeTab"></param>
    public void UpdateKnowledgeTab(EntityUid player, KnowledgeTab knowledgeTab)
    {
        TabContainer.SetTabTitle(knowledgeTab, Loc.GetString("trauma-knowledge-title"));

        knowledgeTab.KnowledgeBox.RemoveAllChildren();
        knowledgeTab.KnowledgePlaceholder.Visible = true;

        var doohickeys = _knowledge.GrabAllKnowledge(player);
        if (doohickeys == null)
            return;

        knowledgeTab.KnowledgePlaceholder.Visible = false;
        foreach (var (groupId, conditions) in doohickeys)
        {
            var boxContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
            };

            var textRect = new TextureRect
            {
                Margin = new Thickness(0, 8, 0, 0),
            };
            if (conditions.Sprite != null)
                textRect.Texture = _sprite.Frame0(conditions.Sprite);

            var box = new BoxContainer
            {
                MinSize = new System.Numerics.Vector2(10, 0),
                Orientation = BoxContainer.LayoutOrientation.Vertical,
            };

            var objectiveText = new RichTextLabel
            {
                Text = conditions.Name,
                Modulate = conditions.Color,
                SetWidth = 325,
                HorizontalAlignment = HAlignment.Left,
            };

            var objectiveDescription = new RichTextLabel
            {
                Text = conditions.Description,
                Modulate = conditions.Color,
                SetWidth = 325,
                HorizontalAlignment = HAlignment.Left,
                StyleClasses = { "LabelSubText" }
            };

            box.AddChild(objectiveText);
            box.AddChild(objectiveDescription);
            boxContainer.AddChild(textRect);
            boxContainer.AddChild(box);
            knowledgeTab.KnowledgeBox.AddChild(boxContainer);
        }
    }
}
