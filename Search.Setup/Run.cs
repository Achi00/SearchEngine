using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Diagnostics;

namespace Search.Setup
{
    public class Run
    {
        public static void RunBenchmark()
        {
            const string modelPath = "D:\\Csharp\\ImageSearch\\Search.Setup\\mobilenetv2-10.onnx";
            const int warmupRuns = 5;
            const int benchmarkRuns = 50;
            const int batchSize = 1;

            Console.WriteLine("=== ONNX Runtime GPU vs CPU Benchmark ===\n");

            // --- CPU ---
            Console.WriteLine("Running CPU benchmark...");
            var cpuTimes = RunBenchmark(CreateCpuSession(modelPath), warmupRuns, benchmarkRuns);
            PrintStats("CPU", cpuTimes);

            // --- GPU (DirectML) ---
            Console.WriteLine("\nRunning GPU benchmark...");
            var gpuTimes = RunBenchmark(CreateGpuSession(modelPath), warmupRuns, benchmarkRuns);
            PrintStats("GPU (DirectML)", gpuTimes);

            // --- Speedup summary ---
            double cpuAvg = cpuTimes.Average();
            double gpuAvg = gpuTimes.Average();
            Console.WriteLine("\n=== Summary ===");
            Console.WriteLine($"CPU avg: {cpuAvg:F2}ms");
            Console.WriteLine($"GPU avg: {gpuAvg:F2}ms");
            Console.WriteLine($"Speedup: {cpuAvg / gpuAvg:F2}x {(gpuAvg < cpuAvg ? "faster on GPU" : "faster on CPU")}");

            // --- Stress test (GPU only, larger batches to spike Task Manager) ---
            Console.WriteLine("\n=== GPU Stress Test (watch Task Manager!) ===");
            Console.WriteLine("Running 200 inferences back to back...");
            var stressTimes = RunBenchmark(CreateGpuSession(modelPath), 0, 200);
            Console.WriteLine($"Stress test done. Avg: {stressTimes.Average():F2}ms | Min: {stressTimes.Min():F2}ms | Max: {stressTimes.Max():F2}ms");

        }
        static InferenceSession CreateCpuSession(string path)
        {
            var opts = new SessionOptions();
            opts.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            return new InferenceSession(path, opts);
        }

        static InferenceSession CreateGpuSession(string path)
        {
            var opts = new SessionOptions();
            opts.AppendExecutionProvider_DML(0);
            opts.EnableMemoryPattern = false;
            opts.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            return new InferenceSession(path, opts);
        }

        static List<NamedOnnxValue> CreateDummyInput(InferenceSession session)
        {
            var inputName = session.InputMetadata.Keys.First();
            var tensor = new DenseTensor<float>(new[] { 32, 3, 224, 224 });
            // Fill with random-ish data so it's not all zeros
            var rng = new Random(42);
            for (int i = 0; i < tensor.Length; i++)
                tensor.SetValue(i, (float)rng.NextDouble());
            return [NamedOnnxValue.CreateFromTensor(inputName, tensor)];
        }

        static List<double> RunBenchmark(InferenceSession session, int warmup, int runs)
        {
            var inputs = CreateDummyInput(session);
            var times = new List<double>();

            // Warmup — first few runs are always slower (JIT, driver init)
            for (int i = 0; i < warmup; i++)
            {
                using var _ = session.Run(inputs);
                Console.Write($"\r  Warmup {i + 1}/{warmup}   ");
            }

            // Actual benchmark
            for (int i = 0; i < runs; i++)
            {
                var sw = Stopwatch.StartNew();
                using var _ = session.Run(inputs);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
                Console.Write($"\r  Run {i + 1}/{runs}   ");
            }

            Console.WriteLine();
            session.Dispose();
            return times;
        }

        static void PrintStats(string label, List<double> times)
        {
            Console.WriteLine($"\n  [{label}]");
            Console.WriteLine($"  Avg:    {times.Average():F2}ms");
            Console.WriteLine($"  Min:    {times.Min():F2}ms");
            Console.WriteLine($"  Max:    {times.Max():F2}ms");
            Console.WriteLine($"  Median: {Median(times):F2}ms");
            Console.WriteLine($"  P95:    {Percentile(times, 95):F2}ms");
        }

        static double Median(List<double> values)
        {
            var sorted = values.OrderBy(x => x).ToList();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
        }

        static double Percentile(List<double> values, int p)
        {
            var sorted = values.OrderBy(x => x).ToList();
            int idx = (int)Math.Ceiling(p / 100.0 * sorted.Count) - 1;
            return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
        }
    }
}
