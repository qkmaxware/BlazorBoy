using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Players;

/// <summary>
/// Game Boy player that runs in the command line
/// </summary>
public class CliPlayer {

    private bool CpuTrace;
    private bool Benchmark;
    private float ScaleX = 1;
    private float ScaleY = 1;

    private Gameboy gb;

    /// <summary>
    /// Create a new CliPlayer
    /// </summary>
    public CliPlayer() {
        this.gb = new Gameboy();
    }
    

    /// <summary>
    /// Run a GB file in your terminal.
    /// </summary>
    /// <param name="rom">Path to a rom file</param>
    /// <param name="width">Width of the screen on the terminal</param>
    /// <param name="height">Height of the screen on the terminal (default is usually too large for default font)</param>
    /// <param name="cpuTrace">Print out a log of executed CPU instructions</param>
    /// <param name="benchmark">Record timing metrics for instructions and hardware components</param>
    static void Main(string? rom = null, int width = 160, int height = 144, bool cpuTrace = false, bool benchmark = false) {
        CliPlayer player = new CliPlayer() {
            CpuTrace = cpuTrace,
            Benchmark = benchmark,
            ScaleX = (float)width / (float)Gpu.LCD_WIDTH,
            ScaleY = (float)height / (float)Gpu.LCD_HEIGHT
        };
        player.Start(rom);
    }

    /// <summary>
    /// Start the emulator interface
    /// </summary>
    public void Start(string? rom) {
        // ---------------------------------------------------------------------------------------
        // Browse for ROM
        // ---------------------------------------------------------------------------------------
        FileInfo? gameFile = null;
        if (string.IsNullOrEmpty(rom)) {
            FileBrowser browser = new FileBrowser();
            while (gameFile is null) {
                Console.Clear();
                browser.ToConsole();

                var key = Console.ReadKey();
                switch (key.Key) {
                    case ConsoleKey.Enter:
                        gameFile = browser.Accept();
                        break;
                    case ConsoleKey.UpArrow:
                        browser.PrevEntry();
                        break;
                    case ConsoleKey.DownArrow:
                        browser.NextEntry();
                        break;
                }
            }
            Console.Clear();
        } else {
            gameFile = new FileInfo(rom);
        }
        // ---------------------------------------------------------------------------------------
        Gameboy gb = new Gameboy();

        // ---------------------------------------------------------------------------------------
        // Configure debug options
        // ---------------------------------------------------------------------------------------
        PerformanceAnalyzer? analyzer = null;
        ITrace? trace = null;
        if (CpuTrace) {
            trace = new FileTrace(gameFile.FullName + ".cpu_trace.log", gb.CPU.Registry);
            gb.AttachCpuTrace(trace);
        }
        if (Benchmark) {
            analyzer = new PerformanceAnalyzer();
            gb.AttachPerformanceAnalyzer(analyzer);
        }
        // ---------------------------------------------------------------------------------------

        // ---------------------------------------------------------------------------------------
        // Play
        // ---------------------------------------------------------------------------------------
        Cartridge cart = new Cartridge(File.ReadAllBytes(gameFile.FullName));
        gb.LoadCartridge(cart);
        bool running = true;
        Console.CancelKeyPress += new ConsoleCancelEventHandler((object? sender, ConsoleCancelEventArgs e) => {
            e.Cancel = true;
            Console.WriteLine();
            running = false;
        });

        Console.Title = cart.Info.title;
        var renderer = new CliRenderer(CliRendererCharacterSet.Ascii, ScaleX, ScaleY);
        while (running) {
            // Run CPU until flush
            gb.DispatchUntilBufferFlush();
            // Draw screen
            var metric = gb.PerformanceAnalyzer?.BeginMeasure(renderer);
            renderer.ToConsole(gb.GPU.Canvas);
            metric?.Record();
            // Handle user input without cancelling or pausing?
            gb.Input.ClearKeys();
            if (Console.KeyAvailable) {
                var key = Console.ReadKey(true); // Read key without printing to console
                switch (key.Key) {
                    //case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        gb.Input.SetKeyState(KeyCodes.Up, true);
                        break;
                    //case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        gb.Input.SetKeyState(KeyCodes.Down, true);
                        break;
                    //case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        gb.Input.SetKeyState(KeyCodes.Left, true);
                        break;
                    //case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        gb.Input.SetKeyState(KeyCodes.Right, true);
                        break;

                    case ConsoleKey.Enter:
                        gb.Input.SetKeyState(KeyCodes.Start, true);
                        break;
                    case ConsoleKey.Tab:
                        gb.Input.SetKeyState(KeyCodes.Select, true);
                        break;

                    case ConsoleKey.Spacebar:
                        gb.Input.SetKeyState(KeyCodes.A, true);
                        break;
                    case ConsoleKey.Escape:
                        gb.Input.SetKeyState(KeyCodes.B, true);
                        break;
                }
            }
        }
        // ---------------------------------------------------------------------------------------
        
        // ---------------------------------------------------------------------------------------
        // Cleanup
        // ---------------------------------------------------------------------------------------
        cleanup(trace, analyzer, gameFile);
        // ---------------------------------------------------------------------------------------
    }

    private void cleanup(ITrace? trace, PerformanceAnalyzer? analyzer, FileInfo cart) {
        Console.WriteLine("Performing final cleanup");
        if (trace is IDisposable dtrace) {
            Console.Write("Flushing cpu-trace...");
            dtrace.Dispose();
            Console.WriteLine("done");
        }
        if (analyzer is not null) {
            Console.Write("Saving performance metrics...");
            using (var writer = new StreamWriter(cart.FullName + ".benchmark.csv")) {
                writer.Write('"');writer.Write("Object");writer.Write('"'); writer.Write(',');
                writer.Write("Event Count"); writer.Write(',');
                writer.Write("Total Time"); writer.Write(',');
                writer.Write("Average Time"); writer.Write(',');
                writer.Write("Max Time"); writer.Write(',');
                writer.Write("Min Time");
                writer.WriteLine();
                foreach (var metric in analyzer.AllMetrics.OrderByDescending((kv) => kv.Value.Sum)) {
                    writer.Write('"');writer.Write(metric.Key);writer.Write('"'); writer.Write(',');
                    writer.Write(metric.Value.Count); writer.Write(',');
                    writer.Write(metric.Value.Sum); writer.Write(',');
                    writer.Write(metric.Value.Average); writer.Write(',');
                    writer.Write(metric.Value.Max); writer.Write(',');
                    writer.Write(metric.Value.Min);
                    writer.WriteLine();
                }
            }
            Console.WriteLine("done");
        }
    }
}