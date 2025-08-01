using System.Diagnostics.CodeAnalysis;
using Content.Client.Lobby;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.DeadSpace.Interfaces.Client;

namespace Content.Client.Players.PlayTimeTracking;

public sealed class JobRequirementsManager : ISharedPlaytimeManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    private IClientSponsorsManager? _sponsorsManager; // DS14-sponsors

    private readonly Dictionary<string, TimeSpan> _roles = new();
    private readonly List<string> _roleBans = new();
    private readonly List<string> _jobWhitelists = new();

    private ISawmill _sawmill = default!;

    public event Action? Updated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("job_requirements");

        // Yeah the client manager handles role bans and playtime but the server ones are separate DEAL.
        _net.RegisterNetMessage<MsgRoleBans>(RxRoleBans);
        _net.RegisterNetMessage<MsgPlayTime>(RxPlayTime);
        _net.RegisterNetMessage<MsgJobWhitelist>(RxJobWhitelist);

        _client.RunLevelChanged += ClientOnRunLevelChanged;

        IoCManager.Instance!.TryResolveType(out _sponsorsManager); // DS14-sponsors
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect, just in case.
            _roles.Clear();
            _jobWhitelists.Clear();
            _roleBans.Clear();
        }
    }

    private void RxRoleBans(MsgRoleBans message)
    {
        _sawmill.Debug($"Received roleban info containing {message.Bans.Count} entries.");

        _roleBans.Clear();
        _roleBans.AddRange(message.Bans);
        Updated?.Invoke();
    }

    private void RxPlayTime(MsgPlayTime message)
    {
        _roles.Clear();

        // NOTE: do not assign _roles = message.Trackers due to implicit data sharing in integration tests.
        foreach (var (tracker, time) in message.Trackers)
        {
            _roles[tracker] = time;
        }

        /*var sawmill = Logger.GetSawmill("play_time");
        foreach (var (tracker, time) in _roles)
        {
            sawmill.Info($"{tracker}: {time}");
        }*/
        Updated?.Invoke();
    }

    private void RxJobWhitelist(MsgJobWhitelist message)
    {
        _jobWhitelists.Clear();
        _jobWhitelists.AddRange(message.Whitelist);
        Updated?.Invoke();
    }

    public bool IsAllowed(JobPrototype job, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (_roleBans.Contains($"Job:{job.ID}"))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-ban"));
            return false;
        }

        if (!CheckWhitelist(job, out reason))
            return false;

        var player = _playerManager.LocalSession;
        if (player == null)
            return true;

        // DS14-blueshield-disabilities-disallow-start
        if (profile != null && !CheckTraitBlacklist(profile, job, out reason))
            return false;
        // DS14-blueshield-disabilities-disallow-start

        // DS14-sponsors-start
        if (_sponsorsManager?.TryGetInfo(out var sponsorInfo) == true && (sponsorInfo.AllowJob || sponsorInfo.AllowedMarkings.Contains(job.ID)))
            return true;

        if (_sponsorsManager != null && job.SponsorOnly)
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-sponsor-only"));
            return false;
        }
        // DS14-sponsors-end

        return CheckRoleRequirements(job, profile, out reason);
    }

    public bool CheckRoleRequirements(JobPrototype job, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var reqs = _entManager.System<SharedRoleSystem>().GetJobRequirement(job);
        return CheckRoleRequirements(reqs, profile, out reason);
    }

    public bool CheckRoleRequirements(HashSet<JobRequirement>? requirements, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (requirements == null || !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        // DS14-meteor-sponsor-start
        if (_sponsorsManager?.TryGetInfo(out var sponsorInfo) == true && sponsorInfo.AllowJob == true)
            return true;
        // DS14-meteor-sponsor-end

        var reasons = new List<string>();
        foreach (var requirement in requirements)
        {
            if (requirement.Check(_entManager, _prototypes, profile, _roles, out var jobReason))
                continue;

            reasons.Add(jobReason.ToMarkup());
        }

        reason = reasons.Count == 0 ? null : FormattedMessage.FromMarkupOrThrow(string.Join('\n', reasons));
        return reason == null;
    }

    // DS14-blueshield-disabilities-disallow-start
    public bool CheckTraitBlacklist(HumanoidCharacterProfile profile, JobPrototype job, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;

        if (profile.TraitPreferences.Intersect(job.TraitsBlacklist).Any())
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-trait-blacklist"));
            return false;
        }

        return true;
    }
    // DS14-blueshield-disabilities-disallow-end

    public bool CheckWhitelist(JobPrototype job, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (job.Whitelisted && !_jobWhitelists.Contains(job.ID))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));
            return false;
        }

        return true;
    }

    public TimeSpan FetchOverallPlaytime()
    {
        return _roles.TryGetValue("Overall", out var overallPlaytime) ? overallPlaytime : TimeSpan.Zero;
    }

    public IEnumerable<KeyValuePair<string, TimeSpan>> FetchPlaytimeByRoles()
    {
        var jobsToMap = _prototypes.EnumeratePrototypes<JobPrototype>();

        foreach (var job in jobsToMap)
        {
            if (_roles.TryGetValue(job.PlayTimeTracker, out var locJobName))
            {
                yield return new KeyValuePair<string, TimeSpan>(job.Name, locJobName);
            }
        }
    }

    public IReadOnlyDictionary<string, TimeSpan> GetPlayTimes(ICommonSession session)
    {
        if (session != _playerManager.LocalSession)
        {
            return new Dictionary<string, TimeSpan>();
        }

        return _roles;
    }
}
