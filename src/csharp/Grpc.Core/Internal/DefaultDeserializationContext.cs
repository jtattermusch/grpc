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
using System;
using System.Buffers;

namespace Grpc.Core.Internal
{
    internal class DefaultDeserializationContext : DeserializationContext
    {
        readonly IBufferReader bufferReader;

        public DefaultDeserializationContext(IBufferReader bufferReader)
        {
            // TODO: fetch the total length immediately...
            this.bufferReader = bufferReader;
        }

        // TODO: revisit narrowing conversion
        public override int PayloadLength => bufferReader.TotalLength.Value;

        public override byte[] PayloadAsNewBuffer()
        {
            if (!bufferReader.TotalLength.HasValue)
            {
                return null;
            }
            int len = (int)bufferReader.TotalLength.Value;
            var buffer = new byte[len];
            FillContinguousBuffer(bufferReader, new Span<byte>(buffer));
            return buffer;
        }

        internal override IMemoryOwner<byte> PayloadAsRentedBuffer()
        {
            if (!bufferReader.TotalLength.HasValue)
            {
                return null;
            }
            int len = (int)bufferReader.TotalLength.Value;
            // TODO(jtattermusch): what is the default MaxBufferSize?
            var rentedBuffer = MemoryPool<byte>.Shared.Rent(len);
            FillContinguousBuffer(bufferReader, rentedBuffer.Memory.Span);
            return rentedBuffer;
        }

        internal override bool TryGetNextBufferSegment(out ReadOnlySpan<byte> bufferSegment)
        {
            // TODO:....
            throw new NotImplementedException();
        }

        private static void FillContinguousBuffer(IBufferReader reader, Span<byte> destination)
        {
            int offset = 0;
            while (reader.TryGetNextSlice(out Slice slice))
            {
                slice.CopyTo(destination.Slice(offset, (int)slice.length));
                offset += (int)slice.length;
            }
            // check that we fill the entire destination
            GrpcPreconditions.CheckState(offset == reader.TotalLength.Value);
        }
    }
}
