﻿#region Copyright notice and license

// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            Native.grpcsharp_test_override_method("grpcsharp_call_start_batch", "nop");
            environment = GrpcEnvironment.AddRef();
        }

        public void Cleanup()
        {
            GrpcEnvironment.ReleaseAsync().Wait();
        }

        public void Run(int threadCount, int iterations, int payloadSize, int maxPendingCompletions)
        {
            Console.WriteLine(string.Format("SendMessageBenchmark: threads={0}, iterations={1}, payloadSize={2}, maxPendingCompletions={3}", threadCount, iterations, payloadSize, maxPendingCompletions));
            var threadedBenchmark = new ThreadedBenchmark(threadCount, () => ThreadBody(iterations, payloadSize, maxPendingCompletions));
            threadedBenchmark.Run();
        }

        private void ThreadBody(int iterations, int payloadSize, int maxPendingCompletions)
        {
            var completionRegistry = new CompletionRegistry(environment);
            var cq = CompletionQueueSafeHandle.CreateAsync(completionRegistry);
            var call = CreateFakeCall(cq);

            var sendCompletionCallback = new NopSendCompletionCallback();
            var payload = new byte[payloadSize];
            var writeFlags = default(WriteFlags);

            var callbackPointers = new IntPtr[maxPendingCompletions];

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations / maxPendingCompletions; i++)
            {
                for (int j = 0; j < maxPendingCompletions; j++) {
                    call.StartSendMessage(sendCompletionCallback, payload, writeFlags, false);
                    callbackPointers[j] = completionRegistry.LastRegisteredKey;
                }
                for (int j = 0; j < maxPendingCompletions; j++) {
                    var callback = completionRegistry.Extract(callbackPointers[j]);
                    callback.OnComplete(true);
                }
            }
            stopwatch.Stop();
            Console.WriteLine("Elapsed millis: " + stopwatch.ElapsedMilliseconds);

            cq.Dispose();
        }

        private static CallSafeHandle CreateFakeCall(CompletionQueueSafeHandle cq)
        {
            var call = CallSafeHandle.CreateFake(new IntPtr(0xdead), cq);
            bool success = false;
            while (!success)
            {
                // avoid calling destroy on a nonexistent grpc_call pointer
                call.DangerousAddRef(ref success);
            }
            return call;
        }

        private class NopSendCompletionCallback : ISendCompletionCallback
        {
            public void OnSendCompletion(bool success)
            {
                // NOP
            }
        }
    }
}
