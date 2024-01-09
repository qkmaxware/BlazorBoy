namespace Qkmaxware.Emulators.Gameboy;

public class PerformanceMetric {
    public TimeSpan Min;
    public TimeSpan Max;
    public TimeSpan Sum;
    public TimeSpan Average => Sum / Count;
    public long Count;
}

public static class PerformanceAnalyzer {
    private static Dictionary<object, PerformanceMetric> records = new Dictionary<object, PerformanceMetric>();

    public static IEnumerable<KeyValuePair<object, PerformanceMetric>> AllRecords => records;

    public static void Record(object key, TimeSpan value) {
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

    public static PerformanceMetric GetPerformance (object key) {
        PerformanceMetric? metric;
        if (!records.TryGetValue(key, out metric)) {
            return new PerformanceMetric();
        } else {
            return metric;
        }
    }
}