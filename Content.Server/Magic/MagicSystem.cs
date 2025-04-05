// <Trauma>
using Content.Shared.Chat;
// </Trauma>
using Content.Server.Chat.Systems;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic;

public sealed class MagicSystem : SharedMagicSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    // Trauma - goob removed everything here
}
