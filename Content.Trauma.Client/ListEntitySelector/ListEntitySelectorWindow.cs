// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.IdentityManagement;

namespace Content.Trauma.Client.ListEntitySelector;

[GenerateTypedNameReferences]
public sealed partial class ListEntitySelectorWindow : FancyWindow
{
    [Dependency] private IEntityManager _entMan = default!;

    public Action<EntityUid>? OnPressEntity;

    public ListEntitySelectorWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void PopulateWindow(HashSet<NetEntity> entities, string windowTitle)
    {
        Title = windowTitle;
        EntityContainer.DisposeAllChildren();

        foreach (var ent in entities)
        {
            var entity = _entMan.GetEntity(ent);
            var button = new Button()
            {
                Text = Identity.Name(entity, _entMan),
            };

            EntityContainer.AddChild(button);
            button.OnPressed += _ => OnPressEntity?.Invoke(entity);
        }
    }
}
