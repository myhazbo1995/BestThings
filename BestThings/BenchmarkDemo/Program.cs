using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<Demo>();
        }
    }

    //[SimpleJob(RuntimeMoniker.Net471, baseline:true)]
    //[SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class Demo
    {
        [Benchmark(Baseline = true)]
        public string GetFullStringNormally()
        {
            string output = "";
            for (int i = 0; i < 100; i++)
            {
                output += i;
            }

            return output;
        }

        [Benchmark]
        public string GetFullStringStringBuilder()
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                output.Append(i);
            }

            return output.ToString();
        }
    }
}