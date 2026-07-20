using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Localizations;
using Content.Shared.Roles;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Random;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    [Dependency] private IRobustRandom _random = default!;

    private readonly HttpClient _webhookHttp = new();
    private string _serverName = string.Empty;
    private string _webhookUrl = string.Empty;
    private string _webhookName = string.Empty;
    private string _webhookAvatarUrl = string.Empty;

    private static readonly List<string> PermaBanNames = new()
    {
        "**Permanent**",
        "**Forever**",
        "**For All Eternity**",
        "**Until appeal?**",
        "**Until the end of time**",
        "**10000 years**"
    };

    private void InitializeTrauma()
    {
        _cfg.OnValueChanged(CCVars.GameHostName, x => _serverName = x, true);
        _cfg.OnValueChanged(TraumaCVars.DiscordBanAvatar, x => _webhookAvatarUrl = x, true);
        _cfg.OnValueChanged(TraumaCVars.DiscordBanName, x => _webhookName = x, true);
        _cfg.OnValueChanged(TraumaCVars.DiscordBanWebhook, x => _webhookUrl = x, true);
    }

    private async void SendBanWebhook(BanDef def)
    {
        if (def.UserIds.Length == 0)
            return; // cant understand mystery userless ban

        try
        {
            var payload = await GetBanPayload(def);
            SendWebhook(payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Caught exception while trying to send ban webhook for ban #{def.Id}: {e}");
        }
    }

    private async void SendWebhook(WebhookPayload payload)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
            return;

        var response = await _webhookHttp.PostAsync(_webhookUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode)
            return; // it worked

        var content = response.Content.ReadAsStringAsync();
        _sawmill.Error($"Got bad status code {response.StatusCode} when sending ban webhook\nResponse: {content}");
    }

    private async Task<WebhookPayload> GetBanPayload(BanDef ban)
    {
        var adminName = "Unknown Admin";
        if (ban.BanningAdmin is not { } admin)
            adminName = Loc.GetString("system-user");
        else if (await _db.GetPlayerRecordByUserId(admin) is { } adminRecord)
            adminName = adminRecord.LastSeenUserName;

        var targetName = "Unknown Player"; // ground battles
        // this assumes only 1 player per ban, cry if you do multiple
        if (await _db.GetPlayerRecordByUserId(ban.UserIds[0]) is { } targetRecord)
            targetName = targetRecord.LastSeenUserName;

        // who cares if its some RICO case across multiple rounds its just a webhook. one round is fine
        var round = ban.RoundIds.Length > 0
            ? ban.RoundIds[0].ToString()
            : "?";

        var desc = new StringBuilder();
        desc.Append($"{targetName} has been banned ");
        if (ban.Type == BanType.Server)
        {
            desc.Append("from the server.");
        }
        else // wake me up when they add a third ban type
        {
            desc.Append("from playing ");
            desc.Append(GetRoleNames(ban));
            desc.Append(" roles.");
        }
        desc.Append("\n> **Banning admin**: ");
        desc.Append(adminName);
        desc.Append("\n> **Duration**: ");
        if (ban.ExpirationTime is { } expires)
        {
            var duration = expires - ban.BanTime;
            desc.Append(LazyTimeName(duration));
        }
        else
        {
            desc.Append(_random.Pick(PermaBanNames));
        }
        desc.Append("\n\n> Reason: *");
        desc.Append(ban.Reason);
        desc.Append("*\n");

        var payload = new WebhookPayload
        {
            Username = _webhookName,
            AvatarUrl = _webhookAvatarUrl,
            Embeds = new()
            {
                new()
                {
                    Color = 0xffb840,
                    Description = desc.ToString(),
                    Footer = new()
                    {
                        Text = $"{_serverName} | Round #{round}"
                    }
                }
            }
        };
        return payload;
    }

    private string GetRoleNames(BanDef ban)
    {
        var names = new List<string>();
        var jobs = new HashSet<string>();
        foreach (var role in ban.Roles!)
        {
            if (role.RoleType == DbTypeAntag)
                names.Add(_prototypeManager.TryIndex<AntagPrototype>(role.RoleId, out var antag)
                    ? Loc.GetString(antag.Name)
                    : role.RoleId);
            else if (role.RoleType == DbTypeJob && _prototypeManager.HasIndex<JobPrototype>(role.RoleId))
                jobs.Add(role.RoleId);
            else
                names.Add(role.RoleId); // who knows what it is
        }

        // coalesce jobs that make up a whole department to make it less spammy
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!department.Roles.All(id => jobs.Contains(id)))
                continue;

            foreach (var id in department.Roles)
            {
                jobs.Remove(id);
            }

            var name = Loc.GetString(department.Name);
            names.Add($"all {name}");
        }

        // just add back any loose jobs
        foreach (var job in jobs)
        {
            names.Add(Loc.GetString(_prototypeManager.Index<JobPrototype>(job).Name));
        }

        return ContentLocalizationManager.FormatList(names);
    }

    private string LazyTimeName(TimeSpan time)
    {
        string Format(string name, double n)
        {
            var i = (int) Math.Floor(n);
            var suffix = i != 1 ? "s" : "";
            return $"{i} {name}{suffix}";
        }

        if (time.TotalDays >= 1)
            return Format("day", time.TotalDays);
        if (time.TotalHours >= 1)
            return Format("hour", time.TotalHours);
        if (time.TotalMinutes >= 1)
            return Format("minute", time.TotalMinutes);
        if (time.TotalSeconds >= 1)
            return Format("second", time.TotalSeconds);

        // troll admin
        return $"{time.TotalMilliseconds} ms";
    }
}
