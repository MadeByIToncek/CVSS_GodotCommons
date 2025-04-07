using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Godot;
using HttpClient = System.Net.Http.HttpClient;
// ReSharper disable InconsistentNaming

namespace CVSS_GodotCommons;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")] // no, it can't, this is made to be public across the app!
public partial class ApiHandler : Control {
    private readonly Config _cfg;
    private readonly HttpClient _scl;

    public ApiHandler() {
        if (!File.Exists("api.config")) {
            GenerateConfig("api.config");
        }
        Config? cfg = ReadConfig("api.config");

        _cfg = cfg ?? throw new Exception("Config couldn't be read!");
        _scl = new HttpClient
        {
            BaseAddress = new Uri(_cfg.BaseUrl),
        };
    }

    private static Config? ReadConfig(string f) { 
        using FileStream stream = File.OpenRead(f);
        return JsonSerializer.Deserialize<Config>(stream);
    }

    private static void GenerateConfig(string cfg) {
        using FileStream stream = File.Create(cfg);
        JsonSerializer.Serialize(stream, new Config {
            BaseUrl = "http://localhost:4444"
        });
    }

    public override void _Ready() {
        GD.Print(GetServerVersion());
    }

    public override void _EnterTree() {
        
    }

    public void Remove() {
        _scl.Dispose();
    }
    
    // ------------------------------- HTTP ACCESS METHODS ---------------------------------------

    public string GetServerVersion() {
        return _scl.GetStringAsync("/").Result;
    }
    
    public string GetOverlayStreamAddress() {
        return "ws://" + new Uri(_cfg.BaseUrl).GetComponents(UriComponents.HostAndPort,UriFormat.UriEscaped) + "/overlay/stream";
    }
    
    public string GetTimeStreamAddress() {
        return "ws://" + new Uri(_cfg.BaseUrl).GetComponents(UriComponents.HostAndPort,UriFormat.UriEscaped) + "/stream/time";
    }
    public async Task<CurrentMatch> GetCurrentMatch() {
        string left = await _scl.GetStringAsync("/match/leftTeamId");
        string right = await _scl.GetStringAsync("/match/rightTeamId");

        Team leftTeam = await GetTeam(Convert.ToInt32(left));
        Team rightTeam = await GetTeam(Convert.ToInt32(right));
        return new CurrentMatch(leftTeam, rightTeam);
    }

    private async Task<Team> GetTeam(int id) {
        HttpResponseMessage data = await _scl.PutAsJsonAsync("/teams/team", new Requests.TeamRequest(id));
        return (await data.Content.ReadFromJsonAsync<Responses.TeamResponse>()).ToTeam();
    }

    public bool ShouldSwitchTVs() {
        return bool.Parse(_scl.GetStringAsync("/overlay/switchTVs").Result);
    }
    public int GetMatchDurationSync() {
        return int.Parse(_scl.GetStringAsync("/defaultMatchLength").Result);
    }

    public async Task<int> GetMatchDuration() {
        return int.Parse(await _scl.GetStringAsync("/defaultMatchLength"));
    }

    public async Task<(int leftScore, int rightScore)> GetCurrentMatchScore() {
        Responses.MatchScoreResponse r = await JsonSerializer.DeserializeAsync<Responses.MatchScoreResponse>(await _scl.GetStreamAsync("/score/matchScore"));

        return (r.left, r.right);
    }

    // ------------------------------- /HTTP ACCESS METHODS --------------------------------------
    
    public class Config
    {
        public required string BaseUrl { get; init; }
    }

}

public readonly struct CurrentMatch(Team left, Team right) {
    public Team LeftTeam { get; } = left;
    public Team RightTeam { get; } = right;
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public readonly struct Match(int id, Team left, Team right, Match.MatchStateEnum state, Match.ResultEnum result) {
    public int Id { get; } = id;
    public Team LeftTeam { get; } = left;
    public Team RightTeam { get; } = right;
    public MatchStateEnum MatchState { get; } = state;
    public ResultEnum Result { get; } = result;

    public enum MatchStateEnum {
        UPCOMING,
        PLAYING,
        ENDED
    }

    public enum ResultEnum {
        LEFT_WON,
        RIGHT_WON,
        NOT_FINISHED
    }
}

public readonly struct Team(int id, string name, Color colorBright, Color colorDark, string[] members) {
    public int Id { get; } = id;
    public string Name { get; } = name;
    public Color ColorBright { get; } = colorBright;
    public Color ColorDark { get; } = colorDark;
    public string[] Members { get; } = members;
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Requests {
    public readonly struct TeamRequest(int id) {
        public int id { get; } = id;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Responses {
    public readonly struct TeamResponse {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public int id { get; init; }
        public string name { get;  init; }
        public string colorBright { get;  init; }
        public string colorDark { get;  init; }
        public string[] members { get;  init; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global

        public Team ToTeam() => new(id, name, ParseColor(colorBright),ParseColor(colorDark),members);

        private static Color ParseColor(string hexString) {
            return new Color(
                int.Parse(hexString[..2], NumberStyles.HexNumber)/255f,
                int.Parse(hexString.Substring(2,2), NumberStyles.HexNumber)/255f,
                int.Parse(hexString.Substring(4,2), NumberStyles.HexNumber)/255f);
        }
    }
    
    public readonly struct MatchScoreResponse {
        public int left { get; init; }
        public int right { get; init; }
    }
}