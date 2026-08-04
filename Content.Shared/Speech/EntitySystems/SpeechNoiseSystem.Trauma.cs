using Content.Goobstation.Common.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeechSoundSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    private bool _barksEnabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, GoobCVars.BarksEnabled, x => _barksEnabled = x, true);
    }
}
