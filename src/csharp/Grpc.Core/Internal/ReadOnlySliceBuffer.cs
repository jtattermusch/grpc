#region Copyright notice and license

// Copyright 2018 The gRPC Authors
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
using Grpc.Core.Utils;

namespace Grpc.Core.Internal
{
    /// <summary>
    /// A reuseable buffer of continguous memory areas (slices).
    /// </summary>
    internal class ReadOnlySliceBuffer
    {
        // how to estimate:
        // reads usually operate with 8KB slices -> for 8MB message need 1024 slots
        // memory consumption: 40 bytes per slice

        const int DefaultThreadLocalCapacity = 100;  // capacity of default thread local instances
        static readonly ThreadLocal<ReadOnlySliceBuffer> defaultThreadLocalBuffers = new ThreadLocal<ReadOnlySliceBuffer>(() => new ReadOnlySliceBuffer(DefaultThreadLocalCapacity));

        int capacity;
        int sliceCount;
        Slice[] slices;

        public ReadOnlySliceBuffer(int capacity)
        {
            GrpcPreconditions.CheckArgument(capacity > 0);
            this.capacity = capacity;
            this.sliceCount = 0;
            this.slices = new Slice[this.capacity];
        }

        public void AddSlice(Slice slice)
        {
            GrpcPreconditions.CheckState(sliceCount < capacity);
            slices[sliceCount] = slice;
            sliceCount ++;
        }

        public void Reset() {
            for (int i = 0; i < sliceCount; i++) {
                slices[i] = default(Slice);
            }
            sliceCount = 0;
        }

        public Slice GetSlice(int index)
        {
            GrpcPreconditions.CheckState(index < sliceCount);
            return slices[index];
        }

        public ReadOnlySliceBuffer Copy(int newCapacity)
        {
            var newBuffer = new ReadOnlySliceBuffer(newCapacity);
            for (int i = 0; i < sliceCount; i++)
            {
                newBuffer.AddSlice(slices[i]);
            }
            return newBuffer;
        }

        public int SliceCount => sliceCount;

        public int Capacity => capacity;

        public static ReadOnlySliceBuffer GetDefaultThreadLocal()
        {
            return defaultThreadLocalBuffers.Value;
        }
    }
}
