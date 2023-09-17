using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Program;

static class Program {
    private static List<Server> favorites = new();
    private static List<Server> filteredServers = new();

    static readonly HashSet<string> blacklist = new() { 
        "107.175.134.251:7778", // LS City
        "107.175.134.251:7777"  // LS City
    };

    static async Task Main() {
        Console.Title = "open.mp Server Browser";
        Console.CursorVisible = false;

        Console.Write("Downloading Servers...");
        List<Server> servers = await FetchServerList();

        favorites = GetFavoritesFromJson();

        DateTime lastDownloadTime = DateTime.Now;
        string searchTerm = "";
        int currentIndex = 0;
        int offset = 0;
        int pageSize = Console.WindowHeight - 3;

        while (true) {
            if (servers.Count > 0) {
                UpdateFavoriteServersInfo(servers);

                Console.Clear();

                filteredServers = favorites.Concat(servers)
                .Where(s => !blacklist.Contains(s.ip) &&
                            (s.hn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                             s.gm?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                             s.ip.Contains(searchTerm)))
                .OrderByDescending(s => favorites.Contains(s) ? int.MaxValue : (s.pc ?? 0))
                .ToList();

                // Display selected server
                for (int i = offset; i < Math.Min(offset + pageSize, filteredServers.Count); i++) {
                    if (favorites.Any(f => f.ip == filteredServers[i].ip)) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{(i == currentIndex ? ">" : " ")} {filteredServers[i].ToSimpleString()}");
                    } else {
                        Console.WriteLine($"{(i == currentIndex ? ">" : " ")} {filteredServers[i]}");
                    }
                    Console.ResetColor();
                }


                Console.WriteLine($"\nSearch Term: {searchTerm}");
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.F1) {
                Console.Clear();
                Console.WriteLine("open.mp Server Browser Instructions");
                Console.WriteLine("=====================================");
                Console.WriteLine("UP ARROW: Move selection up");
                Console.WriteLine("DOWN ARROW: Move selection down");
                Console.WriteLine("ENTER: Enter the selected server");
                Console.WriteLine("ESC: Clear search term");
                Console.WriteLine("BACKSPACE: Remove last character from search term");
                Console.WriteLine("Any ASCII Key: Start a search with the key (search name, gamemode and address)");
                Console.WriteLine("F1: Show this page");
                Console.WriteLine("F2: Refresh server list (available every minute)");

                Console.WriteLine("\nMade by VIRUXE (https://github.com/VIRUXE/openmp-server-browser)");

                Console.WriteLine("\nPress any key to return...");

                Console.ReadKey(intercept: true);

                continue;
            } else if (keyInfo.Key == ConsoleKey.F2) {
                servers = await FetchServerList();
                currentIndex = 0;
                offset = 0;
                searchTerm = "";

                continue;
            } else if (keyInfo.Key == ConsoleKey.RightArrow) {
                SaveFavorite(filteredServers[currentIndex]);
                continue;
            } else if (keyInfo.Key == ConsoleKey.LeftArrow) {
                RemoveFavorite(filteredServers[currentIndex]);
                continue;
            } else if (keyInfo.Key == ConsoleKey.UpArrow) {
                currentIndex = (currentIndex > 0) ? currentIndex - 1 : 0;

                if (currentIndex < offset) offset = currentIndex;
            } else if (keyInfo.Key == ConsoleKey.DownArrow) {
                currentIndex = (currentIndex < filteredServers.Count - 1) ? currentIndex + 1 : filteredServers.Count - 1;

                if (currentIndex >= offset + pageSize) offset = currentIndex - pageSize + 1;
            } else if (keyInfo.Key == ConsoleKey.Enter)
                StartServer(filteredServers[currentIndex].ip.Split(':'));
            else if (keyInfo.Key == ConsoleKey.Escape) {
                currentIndex = 0;
                offset = 0;
                searchTerm = "";
            } else if (keyInfo.Key == ConsoleKey.Backspace)
                searchTerm = searchTerm.Length > 0 ? searchTerm[0..^1] : "";
            else {
                if (keyInfo.KeyChar >= ' ' && keyInfo.KeyChar <= '~' && !(keyInfo.KeyChar == ' ' && string.IsNullOrEmpty(searchTerm))) {
                    searchTerm += keyInfo.KeyChar;
                    currentIndex = 0;
                    offset = 0;
                }
            }
        }
    }

    static async Task<List<Server>> FetchServerList() {
        try {
            string response = await new HttpClient().GetStringAsync("https://api.open.mp/servers");
            return JsonSerializer.Deserialize<List<Server>>(response) ?? new List<Server>();
        } catch (HttpRequestException e) {
            Console.WriteLine($"Request error: {e.Message}");
        } catch (JsonException e) {
            Console.WriteLine($"JSON error: {e.Message}");
        }

        return new List<Server>();
    }

    static void UpdateFavoriteServersInfo(List<Server> servers) {
        var serverDictionary = servers.ToDictionary(s => s.ip, s => s);

        foreach (var favorite in favorites) {
            if (serverDictionary.TryGetValue(favorite.ip, out var server)) {
                favorite.hn = server.hn;
                favorite.pc = server.pc;
                favorite.pm = server.pm;
                favorite.gm = server.gm;
                favorite.la = server.la;
                favorite.pa = server.pa;
                favorite.vn = server.vn;
            }
        }
    }

    static void SaveFavorite(Server server) {
        if (!favorites.Contains(server)) {
            favorites.Add(server);
            File.WriteAllText("openmp.json", JsonSerializer.Serialize(new { favorites }));
        }
    }

    static void RemoveFavorite(Server server) {
        if (favorites.Contains(server)) {
            favorites.Remove(server);
            File.WriteAllText("openmp.json", JsonSerializer.Serialize(new { favorites }));
        }
    }

    static List<Server> GetFavoritesFromJson() {
        string path = "openmp.json";

        if (File.Exists(path)) {
            string json = File.ReadAllText(path);

            var data = JsonSerializer.Deserialize<Dictionary<string, List<Server>>>(json);

            return data?["favorites"] ?? new List<Server>();
        }

        return new List<Server>();
    }

    static void StartServer(string[] args) {
        if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", null) is string gamePath) System.Diagnostics.Process.Start($"{System.IO.Path.GetDirectoryName(gamePath)}\\samp.exe", $"{args[0]} {args[1]}");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<We're following the JSON format>")]
    public class Server {
        public required string ip { get; set; }
        public string hn { get; set; }
        public int? pc { get; set; }
        public int? pm { get; set; }
        public string? gm { get; set; }
        public string? la { get; set; }
        public bool? pa { get; set; }
        public string? vn { get; set; }

        public override string ToString() => $"[{pc}/{pm}] {hn} ({gm})";
        public string ToSimpleString() => $"{hn} ({gm ?? "Unknown"})";
    }
}