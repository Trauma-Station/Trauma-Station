// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Discord;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Content.Trauma.Server.Logging;

/// <summary>
/// Sends errors to a discord webhook from the server config.
/// Internally uses a <see cref="TimedRingBuffer"/> to queue messages and avoid hitting ratelimits as <see cref="DiscordWebhook"/> has no such mechanisms.
/// </summary>
public sealed class ErrorWebhookSystem : EntitySystem
{
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ErrorWebhookLogHandler _handler = new();
    private bool _enabled;
    private WebhookIdentifier? _identifier;
    private TimeSpan _nextSend;
    private TimeSpan _sendDelay;
    private int _limit;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.ErrorWebhookUrl, UpdateWebhookUrl, true);
        Subs.CVar(_cfg, TraumaCVars.ErrorWebhookDelay, x => _sendDelay = TimeSpan.FromSeconds(x), true);
        Subs.CVar(_cfg, TraumaCVars.ErrorWebhookLimit, UpdateBufferLimit, true);

        _handler.Buffer = new(_limit);
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

        if (_identifier is not {} identifier || _handler.Buffer.IsEmpty)
            return; // not enabled or nothing to send

        var now = _timing.CurTime;
        if (now < _nextSend)
            return; // on cooldown

        _nextSend = now + _sendDelay;

        _handler.Buffer.Pop(out var content);
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

    private void UpdateBufferLimit(int limit)
    {
        _limit = limit;
        // any queued messages will get dropped if it's changed midgame
        if (_handler.Buffer != default)
            _handler.Buffer.Reset(limit);
    }
}

public sealed class ErrorWebhookLogHandler : ILogHandler
{
    /// <summary>
    /// Prefix to remove from stack trace paths.
    /// </summary>
    public const string StackTracePrefix = "/home/runner/work/Trauma-Station/Trauma-Station/";

    /// <summary>
    /// Ignore errors that contain this string.
    /// </summary>
    public const string NetEntitySlop = "Can't resolve \"Robust.Shared.GameObjects.MetaDataComponent\" on entity";

    public RingBuffer<string> Buffer = default!; // set in Initialize

    void ILogHandler.Log(string sawmillName, LogEvent message)
    {
        if (message.Level < LogEventLevel.Error)
            return; // only care about errors

        var text = message.RenderMessage()
            .Replace(StackTracePrefix, string.Empty);
        if (text.Contains(NetEntitySlop))
            return; // ignore state error spam for deleted entities referenced in a component, engine "maintainer" is a chud and won't do anything about it

        var name = LogMessage.LogLevelToName(message.Level.ToRobust());
        var content = $"{DateTime.Now:o} [{name}] {sawmillName}: {text}";
        if (message.Exception is {} e)
            content += $"\n{e.ToString().Replace(StackTracePrefix, string.Empty)}\n";

        // trim the end of the stack trace if its too long, usually not important
        var limit = 2000 - 8;
        if (content.Length > limit)
            content = content[0..limit];

        content = $"```\n{content}\n```";

        // if logs are being spammed too fast, oldest messages just get dropped
        Buffer.Push(content, out _);
    }
}
