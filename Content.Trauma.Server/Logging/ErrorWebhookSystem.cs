// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Discord;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Serilog.Events;

namespace Content.Trauma.Server.Logging;

/// <summary>
/// Sends errors to a discord webhook from the server config.
/// </summary>
public sealed class ErrorWebhookSystem : EntitySystem
{
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ErrorWebhookLogHandler _handler = default!;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        _handler = new ErrorWebhookLogHandler(_discord);

        Subs.CVar(_cfg, TraumaCVars.ErrorWebhookUrl, UpdateWebhookUrl, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_enabled)
            _log.RootSawmill.RemoveHandler(_handler);
    }

    public void UpdateWebhookUrl(string url)
    {
        var enabled = !string.IsNullOrEmpty(url);
        if (enabled)
            _discord.GetWebhook(url, data => _handler.Identifier = data.ToIdentifier());
        else
            _handler.Identifier = null;

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
    private readonly DiscordWebhook _discord;
    public WebhookIdentifier? Identifier;

    public ErrorWebhookLogHandler(DiscordWebhook discord)
    {
        _discord = discord;
    }

    void ILogHandler.Log(string sawmillName, LogEvent message)
    {
        if (Identifier is not {} identifier)
            return; // should never happen but whatever

        if (message.Level is not LogEventLevel.Error or LogEventLevel.Fatal)
            return; // only care about errors

        var name = LogMessage.LogLevelToName(message.Level.ToRobust());
        var content = $"[{name}] {sawmillName}: {message.RenderMessage()}";
        if (message.Exception is {} e)
            content += $"\n{e}";

        // trim the end of the stack trace if its too long, usually not important
        if (content.Length > 2000)
            content = content[0..2000];

        var payload = new WebhookPayload()
        {
            Content = content
        };

        try
        {
            _ = _discord.CreateMessage(identifier, payload);
        }
        catch
        {
            // probably shouldnt happen since its not being awaited, but just incase, really don't want a log handler to throw
        }
    }
}
