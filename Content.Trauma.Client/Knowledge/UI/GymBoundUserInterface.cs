// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using JetBrains.Annotations;

namespace Content.Trauma.Client.Knowledge.UI;

[UsedImplicitly]
public sealed class GymBoundUserInterface : BoundUserInterface
{
    private GymWindow? _window;
    public float Time;

    public GymBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new GymWindow();
        _window.OnClose += Close;

        _window.OnRepPressed += (staminaDamage, timingAccuracy) =>
        {
            SendMessage(new GymRepTryMessage(staminaDamage, timingAccuracy));
        };

        _window.OnRepSend += accuracy =>
        {
            SendMessage(new GymRepPerformedMessage(accuracy));
        };

        if (EntMan.TryGetComponent<KnowledgeGrantOnUseComponent>(Owner, out var comp))
        {
            _window.SetupRhythm(comp.IdealRhythmInterval);
        }

        _window.OpenCentered();
    }

    public (int, float) StaminaDamageInput()
    {
        return _window?.StaminaDamageInput() ?? (1, 0f);
    }

    public bool ShouldSendMessage()
    {
        return _window?.GetCurrentAccuracy() > 1f;
    }

    public float HandleRepInput()
    {
        return _window?.HandleRepInput() ?? 0.0f;
    }

    public void UpdateTime(float time)
    {
        Time = time;
        _window?.SetupRhythm(time);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Close();
    }
}
