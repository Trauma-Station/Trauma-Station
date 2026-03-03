using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Barks;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Trauma - barks specific stuff and slider optimisation
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    [Dependency] private readonly IGameTiming _timing = default!;
    private uint _lastColorUpdate;

    private void InitializeTrauma()
    {
        IoCManager.InjectDependencies(this); // did you know IoC exists? now you do

        if (_cfgManager.GetCVar(GoobCVars.BarksEnabled))
        {
            BarksContainer.Visible = true;
            InitializeBarkVoice();
        }
    }

    private void SetBarkVoice(BarkPrototype newVoice)
    {
        Profile = Profile?.WithBarkVoice(newVoice);
        IsDirty = true;
    }
}
