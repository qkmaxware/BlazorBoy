using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Qkmaxware.Emulators.Gameboy;

public class GameInfo {
    [JsonPropertyName("title")]
    public string? CartTitle {get; set;}
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

    public IEnumerator<GameInfo> GetEnumerator() {
        return all.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return all.GetEnumerator();
    }
}
