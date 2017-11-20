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
using System.Collections.Generic;
using Grpc.Core.Utils;

namespace Grpc.Core.Internal
{
    /// <summary>
    /// Simple queue-based pool of objects.
    /// </summary>
    internal class SimpleObjectPool<T> where T : IDisposable
    {
        readonly object myLock = new object();
        readonly Func<T> itemFactory;
        readonly Queue<T> queue;
        readonly ThreadLocal<Queue<T>> threadLocalQueue;
        readonly int capacity;
        readonly int threadLocalCapacity;

        bool disposed;

        public SimpleObjectPool(Func<T> itemFactory, int capacity, int threadLocalCapacity)
        {
            this.itemFactory = itemFactory;
            this.queue = new Queue<T>(capacity);
            this.capacity = capacity;
            this.threadLocalQueue = new ThreadLocal<Queue<T>>(() => new Queue<T>(threadLocalCapacity), false);
            this.threadLocalCapacity = threadLocalCapacity;
        }

        public T Lease()
        {
            var localQueue = threadLocalQueue.Value;
            if (localQueue.Count > 0)
            {
                return localQueue.Dequeue();
            }

            lock(myLock)
            {
                GrpcPreconditions.CheckState(!disposed);
                if (queue.Count > 0)
                {
                    return queue.Dequeue();
                }
            }
            return itemFactory();
        }

        public void Return(T item)
        {
            var localQueue = threadLocalQueue.Value;
            if (localQueue.Count < threadLocalCapacity)
            {
                localQueue.Enqueue(item);
                return;
            }

            lock(myLock)
            {
                GrpcPreconditions.CheckState(!disposed);
                if (queue.Count < capacity)
                {
                    queue.Enqueue(item);
                    return;
                }
            }
            item.Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                while (queue.Count > 0)
                {
                    queue.Dequeue().Dispose();
                }
            }
        }
    }
}
