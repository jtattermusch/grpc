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

using Grpc.Core.Utils;

namespace Grpc.Core
{
    /// <summary>
    /// Provides storage for payload when serializing a message.
    /// </summary>
    public abstract class SerializationContext
    {
        // TODO: make serialization context sth like buffer writer?? request memory, then write into it...
        //  add methods like IBufferWriter...
        //    void Advance(int count);
        //     Memory<T> GetMemory(int sizeHint = 0);
        //     Span<T> GetSpan(int sizeHint = 0);

        // signal that serialization has been done
        //internal abstract void Complete();

        // byte array ownership goes to grpc

        /// <summary>
        /// Use the byte array as serialized form of current message and mark serialization process as complete.
        /// Complete() can only be called once per message.
        /// </summary>
        /// <param name="array">the serialized for of current message</param>
        public abstract void Complete(byte[] array);
    }

    internal class EmulatedSerializationContext : SerializationContext
    {
        bool isComplete;
        byte[] result;

        public override void Complete(byte[] array)
        {
            GrpcPreconditions.CheckState(!isComplete);
            this.isComplete = true;
            this.result = array;
        }

        internal byte[] GetResult()
        {
            // TODO(jtattermusch): check if serialization has completed?
            return this.result;
        }
    }
}
