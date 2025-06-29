using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace Qkmaxware.Emulators.Gameboy;

public class GameInfo {
    [JsonPropertyName("cart_titles")]
    public string[]? CartTitles  {get; set;}
    [JsonPropertyName("name")]
    public string? Name {get; set;}
    [JsonPropertyName("boxart")]
    public string? BoxArtUrl {get; set;}
    [JsonPropertyName("description")]
    public string? Description {get; set;}
    [JsonPropertyName("genres")]
    public string[]? Genres  {get; set;}
    [JsonPropertyName("released")]
    public int ReleaseYear {get; set;}
    [JsonPropertyName("developer")]
    public string? DeveloperName {get; set;}
    [JsonPropertyName("publisher")]
    public string? PublisherName {get; set;}

    public bool HasCartTitle(string title) {
        if (this.CartTitles is null)
            return false;
        return this.CartTitles.Contains(title);
    }

}

public class GameDatabase : IEnumerable<GameInfo> {

    private List<GameInfo> all = new List<GameInfo>();

    private GameDatabase() {
        var assembly = typeof(GameDatabase).GetTypeInfo().Assembly;
        foreach (var name in assembly.GetManifestResourceNames()) {
            if (name == ("Gameboy.Database.database.json")) {
                Stream? resource = assembly.GetManifestResourceStream(name);
                if (resource is null)
                    continue;

                var records = System.Text.Json.JsonSerializer.Deserialize<List<GameInfo>>(resource);
                if (records is null)
                    break;
                this.all = records;
            }
        }
    }

    private static GameDatabase? instance;
    public static GameDatabase Instance() {
        if (instance is null)
            instance = new GameDatabase();
        return instance;
    }

    public GameInfo? FindClosest(string userInput) {
        if (string.IsNullOrWhiteSpace(userInput) || all.Count == 0)
            return null;

        var cleanedUserInput = CleanString(userInput);
        var soundex = Soundex(userInput);

        return all
            .OrderBy(game => LevenshteinDistance(CleanString(game.Name), cleanedUserInput))
            .Where(game => userInput == game.Name || game.HasCartTitle(userInput) || Soundex(game.Name) == soundex)
            .FirstOrDefault();
    }

    // Remove accents and normalize string
    private static string CleanString(string? input) {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        return RemoveDiacritics(input)
            .Replace("-", "")
            .Trim()
            .ToUpperInvariant();
    }
    private static string RemoveDiacritics(string text) {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized) {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // Soundex implementation
    public static string Soundex(string? input) {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove accents, hyphens, trim, and convert to uppercase
        string cleaned = CleanString(input);
        if (string.IsNullOrEmpty(cleaned))
            return string.Empty;

        var soundex = new StringBuilder();
        soundex.Append(cleaned[0]);

        string map = "01230120022455012623010202";
        char prevCode = map[cleaned[0] - 'A'];
        int count = 1;

        for (int i = 1; i < cleaned.Length && count < 4; i++) {
            char c = cleaned[i];
            if (c < 'A' || c > 'Z') continue;
            char code = map[c - 'A'];
            if (code != '0' && code != prevCode) {
                soundex.Append(code);
                count++;
            }
            prevCode = code;
        }
        while (soundex.Length < 4)
            soundex.Append('0');
        return soundex.ToString();
    }

    // Levenshtein distance implementation
    private static int LevenshteinDistance(string? a, string? b) {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++) {
            for (int j = 1; j <= b.Length; j++) {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }
        return d[a.Length, b.Length];
    }

    public IEnumerator<GameInfo> GetEnumerator() {
        return all.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return all.GetEnumerator();
    }
}
