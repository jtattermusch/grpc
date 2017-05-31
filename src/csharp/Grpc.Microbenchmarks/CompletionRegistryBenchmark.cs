#region Copyright notice and license

// Copyright 2015, Google Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using Grpc.Core;
using Grpc.Core.Internal;
using System.Diagnostics;
using System.Threading;

namespace Grpc.Microbenchmarks
{
    public class CompletionRegistryBenchmark
    {
        GrpcEnvironment environment;

        public void Init()
        {
            environment = GrpcEnvironment.AddRef();
        }

        public void Cleanup()
        {
            GrpcEnvironment.ReleaseAsync().Wait();
            // TODO(jtattermusch): track GC stats
        }

        public void Run(int threadCount, int iterations, int pendingCompletions)
        {
            Console.WriteLine(string.Format("CompletionRegistryBenchmark: threads={0}, iterations={1}, pendingCompletions={2}", threadCount, iterations, pendingCompletions));
            var threadedBenchmark = new ThreadedBenchmark(threadCount, () => ThreadBody(iterations, pendingCompletions));
            threadedBenchmark.Run();
        }

        private void ThreadBody(int iterations, int pendingCompletions)
        {
            // TODO: support pending completions...

            var completionRegistry = new CompletionRegistry(environment);

            var pool = new SimpleObjectPool<BatchContextSafeHandle>(Thread.CurrentThread, () => BatchContextSafeHandle.Create(), 1000);
            environment.ThreadPool.BatchContextPools.Value = pool;

            
            var completionHandler = new BatchCompletionDelegate((success, context, state0, state1) => { });
            
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var ctx = pool.Lease();
                ctx.SetPool(pool);
                completionRegistry.RegisterBatchCompletion(ctx, completionHandler, null, null);
                var key = completionRegistry.LastRegisteredKey;
                var entry = completionRegistry.Extract(completionRegistry.LastRegisteredKey);
                entry.OnCompleted(true);
            }
            stopwatch.Stop();
            
            Console.WriteLine("Elapsed millis: " + stopwatch.ElapsedMilliseconds);
            pool.Dispose();
        }
    }
}
