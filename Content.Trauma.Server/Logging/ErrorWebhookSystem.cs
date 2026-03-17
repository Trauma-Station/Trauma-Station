// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Discord;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.Trauma.Server.Logging;

/// <summary>
/// Sends errors to a discord webhook from the server config.
/// Internally uses a queue to avoid hitting ratelimits as <see cref="DiscordWebhook"/> has no such mechanism.
/// </summary>
public sealed class ErrorWebhookSystem : EntitySystem
{
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ErrorWebhookLogHandler _handler = default!;
    private bool _enabled;
    private WebhookIdentifier? _identifier;
    private TimeSpan _nextSend;
    private TimeSpan _sendDelay;

    public override void Initialize()
    {
        base.Initialize();

        _handler = new ErrorWebhookLogHandler();

        Subs.CVar(_cfg, TraumaCVars.ErrorWebhookUrl, UpdateWebhookUrl, true);
        Subs.CVar(_cfg, TraumaCVars.ErrorWebhookDelay, x => _sendDelay = TimeSpan.FromSeconds(x), true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_enabled)
            _log.RootSawmill.RemoveHandler(_handler);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_identifier is not {} identifier || _handler.Messages.Count == 0)
            return; // not enabled or nothing to send

        var now = _timing.CurTime;
        if (now < _nextSend)
            return; // on cooldown

        // wait before sending the next message to not get ratelimited
        _nextSend = now + _sendDelay;

        var content = _handler.Messages.Dequeue();
        var payload = new WebhookPayload()
        {
            Content = content
        };

        // not awaited so it doesn't affect TPS
        _ = _discord.CreateMessage(identifier, payload);
    }

    public void UpdateWebhookUrl(string url)
    {
        var enabled = !string.IsNullOrEmpty(url);
        if (enabled)
            _discord.GetWebhook(url, data => _identifier = data.ToIdentifier());
        else
            _identifier = null;

        // doing change detection because you dont need to re-add the handler just to change the url
        if (enabled == _enabled)
            return;

        _enabled = enabled;
        var root = _log.RootSawmill;
        if (enabled)
            root.AddHandler(_handler);
        else
            root.RemoveHandler(_handler);
    }
}

public sealed class ErrorWebhookLogHandler : ILogHandler
{
    /// <summary>
    /// Prefix to remove from stack trace paths.
    /// </summary>
    public const string StackTracePrefix = "/home/runner/work/Trauma-Station/";

    public Queue<string> Messages = new();

    void ILogHandler.Log(string sawmillName, LogEvent message)
    {
        if (message.Level < LogEventLevel.Error)
            return; // only care about errors

        var name = LogMessage.LogLevelToName(message.Level.ToRobust());
        var content = $"{DateTime.Now:o} [{name}] {sawmillName}: {message.RenderMessage()}";
        if (message.Exception is {} e)
            content += $"\n```\n{e.ToString().Replace(StackTracePrefix, string.Empty)}\n```";

        // trim the end of the stack trace if its too long, usually not important
        if (content.Length > 2000)
            content = content[0..2000];

        Messages.Enqueue(content);
    }
}
