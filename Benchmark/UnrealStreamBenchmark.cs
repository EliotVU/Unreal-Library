﻿using System.IO;
using UELib;
using BenchmarkDotNet.Attributes;
using UELib.Core;

namespace Eliot.UELib.Benchmark
{
    public class UnrealStreamBenchmark
    {
        private IUnrealStream _Stream;

        public UnrealStreamBenchmark()
        {
            // B, G, R, A;
            var structBuffer = new byte[] { 255, 128, 64, 80 };
            var baseStream = new MemoryStream(structBuffer);
            _Stream = new UnrealTestStream(null, baseStream);
        }

        [Benchmark]
        public void ReadStruct()
        {
            var stream = _Stream;
            stream.Seek(0, SeekOrigin.Begin);
            stream.ReadStruct(out UColor color);
        }
        /// <summary>
        /// Verify that ReadAtomicStruct is indeed performing its purpose :)
        /// </summary>
        [Benchmark]
        public void ReadAtomicStruct()
        {
            var stream = _Stream;
            stream.Seek(0, SeekOrigin.Begin);
            stream.ReadAtomicStruct(out UColor color);
        }
    }
}