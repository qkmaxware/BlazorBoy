using System.Diagnostics;

namespace Qkmaxware.Emulators.Gameboy;

public class PerformanceMetric {
    public TimeSpan Min;
    public TimeSpan Max;
    public TimeSpan Sum;
    public TimeSpan Average => Sum / Count;
    public long Count;
}

public class PerformanceAnalyzer {
    private Dictionary<object, PerformanceMetric> records = new Dictionary<object, PerformanceMetric>();

    public IEnumerable<KeyValuePair<object, PerformanceMetric>> AllMetrics => records;

    public void Record(object key, TimeSpan value) {
        PerformanceMetric? metric;
        if (!records.TryGetValue(key, out metric)) {
            metric = new PerformanceMetric();
            records.Add(key, metric);
        }

        metric.Max = value > metric.Max ? value : metric.Max;
        metric.Min = value < metric.Min ? value : metric.Min;
        metric.Sum += value;
        metric.Count += 1;
    }

    public PerformanceMetric GetPerformance (object key) {
        PerformanceMetric? metric;
        if (!records.TryGetValue(key, out metric)) {
            return new PerformanceMetric();
        } else {
            return metric;
        }
    }

    public class ActivePerformanceMeasure {
        private PerformanceAnalyzer analyzer;
        private object? key;
        private Stopwatch stopwatch;
        public ActivePerformanceMeasure(PerformanceAnalyzer analyzer, object? key) {
            this.analyzer = analyzer;
            this.key = key;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void Record() {
            this.stopwatch.Stop();
            if (key is not null)
                analyzer.Record(this.key, this.stopwatch.Elapsed);
        }

        public ActivePerformanceMeasure ChangeKey(object? key) {
            this.key = key;
            return this;
        }
    }
    public ActivePerformanceMeasure? BeginMeasure(object? key) {
        return new ActivePerformanceMeasure(this, key);
    }
}

