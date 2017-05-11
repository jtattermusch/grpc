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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Grpc.Core.Logging;
using Grpc.Core.Utils;

namespace Grpc.Core.Internal
{
    internal delegate void OpCompletionDelegate(bool success);

    internal delegate void BatchCompletionDelegate(bool success, BatchContextSafeHandle ctx, object state);

    internal delegate void RequestCallCompletionDelegate(bool success, RequestCallContextSafeHandle ctx);

    internal class CompletionRegistry
    {
        static readonly ILogger Logger = GrpcEnvironment.Logger.ForType<CompletionRegistry>();

        readonly GrpcEnvironment environment;
        readonly ConcurrentDictionary<IntPtr, Entry> dict = new ConcurrentDictionary<IntPtr, Entry>(new IntPtrComparer());
        IntPtr lastRegisteredKey;  // only for testing

        public CompletionRegistry(GrpcEnvironment environment)
        {
            this.environment = environment;
        }

        private void Register(IntPtr key, Entry entry)
        {
            environment.DebugStats.PendingBatchCompletions.Increment();
            GrpcPreconditions.CheckState(dict.TryAdd(key, entry));
            this.lastRegisteredKey = key;
        }

        public void RegisterBatchCompletion(BatchContextSafeHandle ctx, BatchCompletionDelegate callback, object state)
        {
            Register(ctx.Handle, new Entry(ctx, callback, state));
        }

        public void RegisterRequestCallCompletion(RequestCallContextSafeHandle ctx, RequestCallCompletionDelegate callback)
        {
            Register(ctx.Handle, new Entry(ctx, callback));
        }

        public Entry Extract(IntPtr key)
        {
            Entry value;
            GrpcPreconditions.CheckState(dict.TryRemove(key, out value));
            environment.DebugStats.PendingBatchCompletions.Decrement();
            return value;
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        public IntPtr LastRegisteredKey
        {
            get { return this.lastRegisteredKey; }
        }

        private static void HandleBatchCompletion(bool success, BatchContextSafeHandle ctx, BatchCompletionDelegate callback, object state)
        {
            try
            {
                callback(success, ctx, state);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception occured while invoking batch completion delegate.");
            }
            finally
            {
                if (ctx != null)
                {
                    ctx.Dispose();
                }
            }
        }

        private static void HandleRequestCallCompletion(bool success, RequestCallContextSafeHandle ctx, RequestCallCompletionDelegate callback)
        {
            try
            {
                callback(success, ctx);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception occured while invoking request call completion delegate.");
            }
            finally
            {
                if (ctx != null)
                {
                    ctx.Dispose();
                }
            }
        }

        /// <summary>
        /// An entry of completion registry. Allows storing additional state along with a completion callback to prevent the need to allocate object on the heap unnecessarily.
        /// </summary>
        public struct Entry
        {
            static readonly Action<bool, Entry> BatchCompletionHandler = (success, entry) => HandleBatchCompletion(success, (BatchContextSafeHandle) entry.state0, (BatchCompletionDelegate) entry.state1, entry.state2);
            static readonly Action<bool, Entry> RequestCallCompletionHandler = (success, entry) => HandleRequestCallCompletion(success, (RequestCallContextSafeHandle) entry.state0, (RequestCallCompletionDelegate) entry.state1);

            public Entry(BatchContextSafeHandle ctx, BatchCompletionDelegate callback, object state)
            {
                this.handler = BatchCompletionHandler;
                this.state0 = ctx;
                this.state1 = callback;
                this.state2 = state;
            }

            public Entry(RequestCallContextSafeHandle ctx, RequestCallCompletionDelegate callback)
            {
                this.handler = RequestCallCompletionHandler;
                this.state0 = ctx;
                this.state1 = callback;
                this.state2 = null;
            }

            readonly Action<bool, Entry> handler;
            readonly object state0;
            readonly object state1;
            readonly object state2;

            /// <summary>
            /// Invoke the callback associated with this completion registry entry.
            /// </summary>
            public void OnCompleted(bool success)
            {
                handler(success, this);
            }
        }

        /// <summary>
        /// IntPtr doesn't implement <c>IEquatable{IntPtr}</c> so we need to use custom comparer to avoid boxing.
        /// </summary>
        private class IntPtrComparer : IEqualityComparer<IntPtr>
        {
            public bool Equals(IntPtr x, IntPtr y)
            {
                return x == y;
            }

            public int GetHashCode(IntPtr obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
