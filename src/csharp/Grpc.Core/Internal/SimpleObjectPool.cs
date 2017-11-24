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
    internal class SimpleObjectPool<T> : IObjectPool<T>
        where T : class, IDisposable
    {
        readonly object myLock = new object();
        readonly Func<T> itemFactory;

        // Queue shared between threads, access needs to be synchronized.
        readonly Queue<T> sharedQueue;
        readonly int sharedCapacity;

        readonly ThreadLocal<ThreadLocalData> threadLocalData;
        readonly int threadLocalCapacity;
        readonly int rentLimit;

        bool disposed;

        public SimpleObjectPool(Func<T> itemFactory, int sharedCapacity, int threadLocalCapacity)
        {
            GrpcPreconditions.CheckArgument(sharedCapacity >= 0);
            GrpcPreconditions.CheckArgument(threadLocalCapacity >= 0);
            this.itemFactory = GrpcPreconditions.CheckNotNull(itemFactory, nameof(itemFactory));
            this.sharedQueue = new Queue<T>(sharedCapacity);
            this.sharedCapacity = sharedCapacity;
            this.threadLocalData = new ThreadLocal<ThreadLocalData>(() => new ThreadLocalData(threadLocalCapacity), false);
            this.threadLocalCapacity = threadLocalCapacity;
            this.rentLimit = threadLocalCapacity / 2;
        }

        public T Lease()
        {
            GrpcPreconditions.CheckState(!disposed);

            var localData = threadLocalData.Value;
            if (localData.Queue.Count > 0)
            {
                return localData.Queue.Dequeue();
            }
            if (localData.CreateBudget > 0)
            {
                localData.CreateBudget --;
                return itemFactory();
            }

            int itemsMoved = 0;
            T leasedItem = null;
            lock(myLock)
            {
                if (sharedQueue.Count > 0)
                {
                    leasedItem = sharedQueue.Dequeue();
                }
                while (sharedQueue.Count > 0 && itemsMoved < rentLimit)
                {
                    localData.Queue.Enqueue(sharedQueue.Dequeue());
                    itemsMoved ++;
                }
            }

            // If the shared pool didn't contain all rentLimit items,
            // next time we try to lease we will just create those
            // instead of trying to grab them from the shared queue.
            // This is to guarantee we won't be accessing the shared queue too often.
            localData.CreateBudget += rentLimit - itemsMoved;

            return leasedItem ?? itemFactory();
        }

        public void Return(T item)
        {
            GrpcPreconditions.CheckState(!disposed);

            var localData = threadLocalData.Value;
            if (localData.Queue.Count < threadLocalCapacity)
            {
                localData.Queue.Enqueue(item);
                return;
            }
            if (localData.DisposeBudget > 0)
            {
                localData.DisposeBudget --;
                item.Dispose();
                return;
            }

            int itemsReturned = 0;
            int returnLimit = rentLimit + 1;
            lock(myLock)
            {
                if (sharedQueue.Count < sharedCapacity)
                {
                    sharedQueue.Enqueue(item);
                    itemsReturned ++;
                }
                while (sharedQueue.Count < sharedCapacity && itemsReturned < returnLimit)
                {
                    sharedQueue.Enqueue(localData.Queue.Dequeue());
                    itemsReturned ++;
                }
            }

            // If the shared pool could not accomodate all returnLimit items,
            // next time we try to return we will just dispose the item
            // instead of trying to return them to the shared queue.
            // This is to guarantee we won't be accessing the shared queue too often.
            localData.DisposeBudget += returnLimit - itemsReturned;

            if (itemsReturned == 0)
            {
                localData.DisposeBudget --;
                item.Dispose();
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                while (sharedQueue.Count > 0)
                {
                    sharedQueue.Dequeue().Dispose();
                }
            }
        }

        class ThreadLocalData
        {
            public ThreadLocalData(int capacity)
            {
                this.Queue = new Queue<T>(capacity);
            }

            public Queue<T> Queue { get; }
            public int CreateBudget { get; set; }
            public int DisposeBudget { get; set; }
        }
    }
}
