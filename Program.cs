using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

static class Program {
    static async Task Main() {
        Console.Title = "open.mp Server Browser";
        Console.CursorVisible = false;

        List<Server> servers = await LoadServers();

        DateTime lastDownloadTime = DateTime.Now;
        string searchTerm = "";
        int currentIndex = 0;
        int offset = 0;
        int pageSize = Console.WindowHeight - 3;

        while (true) {
            Console.Clear();
            List<Server> filteredServers = servers
                .Where(s =>
                    string.IsNullOrEmpty(searchTerm) ||
                    s.hn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.gm.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.ip.Contains(searchTerm))
                .OrderByDescending(s => s.pc)
                .ToList();

            // Display selected server
            for (int i = offset; i < Math.Min(offset + pageSize, filteredServers.Count); i++) Console.WriteLine($"{(i == currentIndex ? ">" : " ")} {filteredServers[i]}");

            Console.WriteLine($"\nSearch Term: {searchTerm}");

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
                Console.WriteLine("Any ASCII Key: Start a search with the key");
                Console.WriteLine("F1: Show this page");
                Console.WriteLine("F2: Refresh server list (available every minute)");

                Console.WriteLine("\nMade by VIRUXE (https://github.com/VIRUXE/openmp-server-browser)");

                Console.WriteLine("\nPress any key to return...");

                Console.ReadKey(intercept: true);

                continue;
            } else if (keyInfo.Key == ConsoleKey.F2) {
                servers = await LoadServers();
                currentIndex = 0;
                offset = 0;
                searchTerm = "";

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

    static async Task<List<Server>> LoadServers() {
        string response = await new System.Net.Http.HttpClient().GetStringAsync("https://api.open.mp/servers");
        return System.Text.Json.JsonSerializer.Deserialize<List<Server>>(response) ?? new List<Server>();
    }

    static void StartServer(string[] args) {
        if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", null) is string gamePath) System.Diagnostics.Process.Start($"{System.IO.Path.GetDirectoryName(gamePath)}\\samp.exe", $"{args[0]} {args[1]}");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<We're following the JSON format>")]
    public class Server {
        public required string ip { get; set; }
        public required string hn { get; set; }
        public required int pc { get; set; }
        public required int pm { get; set; }
        public required string gm { get; set; }
        public required string la { get; set; }
        public required bool pa { get; set; }
        public required string vn { get; set; }

        public override string ToString() => $"[{pc}/{pm}] {hn} ({gm})";
    }
}