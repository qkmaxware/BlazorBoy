using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Players;

/// <summary>
/// Game Boy player that runs in the command line
/// </summary>
public class CliPlayer {

    private bool CpuTrace;
    private bool Benchmark;

    private Gameboy gb;

    /// <summary>
    /// Create a new CliPlayer
    /// </summary>
    public CliPlayer() {
        this.gb = new Gameboy();
    }
    

    /// <summary>
    /// Main entrypoint for the program
    /// </summary>
    /// <param name="cpuTrace">Print out a log of executed CPU instructions</param>
    /// <param name="benchmark">Record timing metrics for instructions and hardware components</param>
    static void Main(bool cpuTrace = false, bool benchmark = false) {
        CliPlayer player = new CliPlayer() {
            CpuTrace = cpuTrace,
            Benchmark = benchmark
        };
        player.Start();
    }

    /// <summary>
    /// Start the emulator interface
    /// </summary>
    public void Start() {
        // ---------------------------------------------------------------------------------------
        // Browse for ROM
        // ---------------------------------------------------------------------------------------
        FileBrowser browser = new FileBrowser();
        FileInfo? gameFile = null;
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
        // ---------------------------------------------------------------------------------------

        CliRenderer renderer = new CliRenderer(CliRendererCharacterSet.Ascii);
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
        var position = Console.GetCursorPosition();
        bool running = true;
        Console.CancelKeyPress += new ConsoleCancelEventHandler((object? sender, ConsoleCancelEventArgs e) => {
            e.Cancel = true;
            Console.WriteLine();
            Console.WriteLine("SIGINT Recieved, shutting down emulator");
            running = false;
        });

        while (running) {
            // TODO somehow handle user input without cancelling or pausing?
            gb.DispatchUntilBufferFlush();
            var metric = gb.PerformanceAnalyzer?.BeginMeasure(renderer);
            Console.SetCursorPosition(position.Left, position.Top);
            renderer.ToConsole(gb.GPU.Canvas);
            metric?.Record();
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