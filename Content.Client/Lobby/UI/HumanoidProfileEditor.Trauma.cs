using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Barks;
using Content.Trauma.Common.Knowledge;
using Content.Shared.Preferences;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Trauma - barks specific stuff and slider optimisation
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    [Dependency] private readonly IGameTiming _timing = default!;
    private uint _lastColorUpdate;

    /// <summary>
    /// For other systems to do stuff
    /// </summary>
    public event Action<HumanoidCharacterProfile?>? OnSetProfile;

    private void InitializeTrauma()
    {
        IoCManager.InjectDependencies(this); // did you know IoC exists? now you do

        if (_cfgManager.GetCVar(GoobCVars.BarksEnabled))
        {
            BarksContainer.Visible = true;
            InitializeBarkVoice();
        }

        OnSetProfile += _ => UpdateBarkVoice(); // TODO: move bark shitcode into module
    }

    private void SetBarkVoice(BarkPrototype newVoice)
    {
        Profile = Profile?.WithBarkVoice(newVoice);
        IsDirty = true;
    }

    private void SetKnowledge(KnowledgeProfile profile)
    {
        Profile = Profile?.WithKnowledge(profile);
        IsDirty = true;
    }
}
