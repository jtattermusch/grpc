#region Copyright notice and license

// Copyright 2016, Google Inc.
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
using System.Threading.Tasks;
using Grpc.Core.Logging;

namespace Grpc.Core.Internal
{
    /// <summary>
    /// Event processor that polls on completion queue using parallel tasks on the build-in .NET thread pool
    /// (instead of utilizing a dedicated thread pool).
    /// </summary>
    internal class TaskEventProcessor : IEventProcessor
    {
        static readonly ILogger Logger = GrpcEnvironment.Logger.ForType<TaskEventProcessor>();

        readonly object myLock = new object();
        readonly TaskCompletionSource<object> shutdownTcs = new TaskCompletionSource<object>();
        readonly GrpcEnvironment environment;
        readonly int desiredPollerCount;
        readonly CompletionQueueSafeHandle cq;

        int activePollerCount;  // number of active pollers
        int shutdownPollerCount;  // number of pollers that have finished with shutdown event.

        private TaskEventProcessor(GrpcEnvironment environment, int desiredPollerCount)
        {
            this.environment = environment;
            this.desiredPollerCount = desiredPollerCount;
            this.cq = CompletionQueueSafeHandle.Create();

            MaintainPollers(0);
        }

        public void Stop()
        {
            lock (myLock)
            {
                cq.Shutdown();
                shutdownTcs.Task.Wait();

                cq.Dispose();
            }
        }

        public CompletionQueueSafeHandle CompletionQueue
        {
            get { return cq; }
        }

        /// <summary>
        /// Checks if any poller tasks need to be added and starts them if so.
        /// </summary>
        private void MaintainPollers(int extraPollers)
        {
            int origPollerCount = Interlocked.Exchange(ref activePollerCount, desiredPollerCount);
            int pollersToStart = Math.Min(0, desiredPollerCount - origPollerCount) + extraPollers;

            for (int i = 0; i < pollersToStart; i++)
            {
                Task.Run(PollOnceBody);
            }
        }

        /// <summary>
        /// Body of the polling task.
        /// </summary>
        private void PollOnceBody()
        {
            var ev = cq.Next();

            if (ev.type == GRPCCompletionType.Shutdown)
            {
                int count = Interlocked.Increment(ref shutdownPollerCount);
                if (desiredPollerCount == count)
                {
                    Task.Run(() => shutdownTcs.SetResult(null));
                }
                return;
            }

            MaintainPollers(extraPollers: 1);  //  1 extra poller to replace the one that has just finished.

            if (ev.type == GRPCCompletionType.OpComplete)
            {
                bool success = (ev.success != 0);
                IntPtr tag = ev.tag;
                try
                {
                    var callback = environment.CompletionRegistry.Extract(tag);
                    callback(success);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Exception occured while invoking completion delegate");
                }
            }
        }

        public static TaskEventProcessor CreateStarted(GrpcEnvironment environment, int poolSize)
        {
            return new TaskEventProcessor(environment, poolSize);
        }
    }
}
