using Content.Client.Clothing.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Tag;
//using Content.Shared.Prototypes; // Trauma - die
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.UI;

[UsedImplicitly]
public sealed partial class ChameleonBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;
    private readonly ChameleonClothingSystem _chameleon;
    private readonly TagSystem _tag;

    [ViewVariables]
    private ChameleonMenu? _menu;
    private CompName _tagName; // Trauma

    public ChameleonBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _chameleon = EntMan.System<ChameleonClothingSystem>();
        _tag = EntMan.System<TagSystem>();
        _tagName = EntMan.ComponentFactory.CompName<TagComponent>(); // Trauma
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChameleonMenu>();
        _menu.OnIdSelected += OnIdSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ChameleonBoundUserInterfaceState st)
            return;

        var targets = _chameleon.GetValidTargets(st.Slot);
        if (st.RequiredTag != null)
        {
            var newTargets = new List<EntProtoId>();
            foreach (var target in targets)
            {
                if (string.IsNullOrEmpty(target) || !_proto.Resolve(target, out EntityPrototype? proto))
                    continue;

                if (!proto.TryComp<TagComponent>(_tagName, out var tag) || !_tag.HasTag(tag, st.RequiredTag)) // Trauma - use _tagName
                    continue;

                newTargets.Add(target);
            }
            _menu?.UpdateState(newTargets, st.SelectedId);
        } else
        {
            _menu?.UpdateState(targets, st.SelectedId);
        }
    }

    private void OnIdSelected(string selectedId)
    {
        SendMessage(new ChameleonPrototypeSelectedMessage(selectedId));
    }
}
