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
using System.Threading;
using Grpc.Core;
using Grpc.Core.Internal;
using System.Collections.Generic;
using System.Diagnostics;

namespace Grpc.Microbenchmarks
{
    public class SendMessageBenchmark
    {
        static readonly NativeMethods Native = NativeMethods.Get();

        GrpcEnvironment environment;

        public void Init()
        {
            var environment = GrpcEnvironment.AddRef();
        }

        public void Cleanup()
        {
            GrpcEnvironment.ReleaseAsync().Wait();
        }

        public void Run(int threadCount, int iterations, int payloadSize)
        {
            Console.WriteLine(string.Format("SendMessageBenchmark: threads={0}, iterations={1}, payloadSize={2}", threadCount, iterations, payloadSize));
            var threads = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(new ThreadStart(() => ThreadBody(iterations, payloadSize)));
                thread.Start();
                threads.Add(thread);
            }

            Console.WriteLine("Running threads.");

            foreach (var thread in threads)
            {
                thread.Join();
            }
            Console.WriteLine("All threads finished.");
        }

        private void ThreadBody(int iterations, int payloadSize)
        {
            var unusedCq = CompletionQueueSafeHandle.CreateSync();
            var call = CallSafeHandle.CreateFake(new IntPtr(0xdead), unusedCq);
            bool unused = false;
            call.DangerousAddRef(ref unused); // avoid calling destroy on a fake pointer

            // TODO: completion registry for CQ.

            var sendCompletionHandler = new SendCompletionHandler((success) => { });
            var payload = new byte[payloadSize];
            var writeFlags = default(WriteFlags);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                call.StartSendMessage(sendCompletionHandler, payload, writeFlags, false);
            }
            stopwatch.Stop();
            Console.WriteLine("Elapsed millis: " + stopwatch.ElapsedMilliseconds);

            unusedCq.Dispose();
        }
    }
}
