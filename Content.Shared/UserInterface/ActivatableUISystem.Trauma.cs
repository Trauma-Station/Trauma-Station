using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    private void InitializeTrauma()
    {
        SubscribeLocalEvent<ActivatableUIComponent, GetVerbsEvent<AlternativeVerb>>(GetAltVerb);
    }

    private void GetAltVerb(EntityUid uid, ActivatableUIComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.AltVerb || !ShouldAddVerb(uid, component, args))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => InteractUI(args.User, uid, component),
            Text = Loc.GetString(component.VerbText),
            // TODO VERB ICON find a better icon
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }
}
