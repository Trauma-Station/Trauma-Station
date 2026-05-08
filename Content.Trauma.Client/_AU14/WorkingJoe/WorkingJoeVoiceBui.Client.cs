using Content.Trauma.Client._AU14.WorkingJoe;
using Content.Trauma.Shared._AU14.WorkingJoe;
using Content.Shared.Chat.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client._AU14.WorkingJoe;

public sealed class WorkingJoeVoiceBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private WorkingJoeVoiceWindow? _window;

    public WorkingJoeVoiceBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new WorkingJoeVoiceWindow();
        _window.OnClose += Close;
        _window.OnLineSelected += OnLineSelected;

        // Build list from all emote prototypes tagged for WorkingJoe
        var lines = new List<WorkingJoeVoiceLine>();
        foreach (var emote in _proto.EnumeratePrototypes<EmotePrototype>())
        {
            if (emote.Whitelist?.Tags == null)
                continue;
            if (!emote.Whitelist.Tags.Contains("WorkingJoe"))
                continue;

            lines.Add(new WorkingJoeVoiceLine
            {
                EmoteId = emote.ID,
                DisplayName = _loc.GetString(emote.Name),
                Category = emote.Category.ToString(),
            });
        }

        _window.SetLines(lines);
        _window.OpenCentered();
    }

    private void OnLineSelected(string emoteId)
    {
        SendMessage(new WorkingJoePlayLineMessage(emoteId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
