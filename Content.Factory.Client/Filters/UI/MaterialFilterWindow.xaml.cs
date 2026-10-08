// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Factory.Shared.Filters;
using Content.Shared.Materials;
using Robust.Shared.Timing;

namespace Content.Factory.Client.Filters.UI;

[GenerateTypedNameReferences]
public sealed partial class MaterialFilterWindow : FancyWindow
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private SpriteSystem _sprite = default!;

    public event Action? OnInvert;
    public event Action<ProtoId<MaterialPrototype>>? OnToggle;

    private MaterialFilterComponent _comp = default!;
    private List<(ProtoId<MaterialPrototype>, string, ContainerButton)> _materials = new();

    public MaterialFilterWindow()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _sprite = _ent.System<SpriteSystem>();

        InvertButton.OnPressed += _ => OnInvert?.Invoke();
        Search.OnTextChanged += _ => UpdateSearch(Search.Text);

        foreach (var proto in _proto.EnumeratePrototypes<MaterialPrototype>())
        {
            var id = proto.ID;
            var name = Loc.GetString(proto.Name);

            var icon = new TextureRect()
            {
                Texture = _sprite.Frame0(proto.Icon),
                TextureScale = new(2, 2),
                Margin = new(6)
            };
            var label = new Label()
            {
                Text = name
            };
            var button = new ContainerButton()
            {
                HorizontalExpand = true,
                ToggleMode = true
            };
            button.OnPressed += _ => OnToggle?.Invoke(id);
            button.AddChild(icon);
            button.AddChild(label);

            _materials.Add((id, name.ToLowerInvariant(), button));
            Materials.AddChild(button);
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Update();
    }

    public void SetComp(MaterialFilterComponent comp)
    {
        _comp = comp;
        Update();
    }

    private void Update()
    {
        InvertButton.Text = _comp.Inverted ? "Denying" : "Allowing";
        var whitelist = _comp.Whitelist;
        foreach (var (id, _, button) in _materials)
        {
            button.Pressed = whitelist.Contains(id);
        }
    }

    private void UpdateSearch(string query)
    {
        query = query.Trim().ToLowerInvariant();
        var empty = string.IsNullOrEmpty(query);
        foreach (var (_, name, button) in _materials)
        {
            button.Visible = empty || name.Contains(query);
        }
    }
}
