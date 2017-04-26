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
using System.Runtime.InteropServices;
using System.Text;
using Grpc.Core;

namespace Grpc.Core.Internal
{
    /// <summary>
    /// grpcsharp_batch_context
    /// </summary>
    internal class BatchContextSafeHandle : SafeHandleZeroIsInvalid
    {
        static readonly byte[] SideBuffer = new byte[1024*1024];
        static readonly NativeMethods Native = NativeMethods.Get();

        static readonly byte[] EmptyByteArray = new byte[0];

        public GCHandle SendMessageGCHandle;
        private bool resetOnly;

        private BatchContextSafeHandle()
        {
        }

        public static BatchContextSafeHandle Create(bool resetOnly = false)
        {
            var ctx = Native.grpcsharp_batch_context_create();
            ctx.resetOnly = resetOnly;
            return ctx;
        }

        public IntPtr Handle
        {
            get
            {
                return handle;
            }
        }

        public bool IsResetOnly
        {
            get { return resetOnly; }
        }

        // Gets data of recv_initial_metadata completion.
        public Metadata GetReceivedInitialMetadata()
        {
            IntPtr metadataArrayPtr = Native.grpcsharp_batch_context_recv_initial_metadata(this);
            return MetadataArraySafeHandle.ReadMetadataFromPtrUnsafe(metadataArrayPtr);
        }
            
        // Gets data of recv_status_on_client completion.
        public ClientSideStatus GetReceivedStatusOnClient()
        {
            UIntPtr detailsLength;
            IntPtr detailsPtr = Native.grpcsharp_batch_context_recv_status_on_client_details(this, out detailsLength);
            string details = MarshalUtils.PtrToStringUTF8(detailsPtr, (int) detailsLength.ToUInt32());
            var status = new Status(Native.grpcsharp_batch_context_recv_status_on_client_status(this), details);

            IntPtr metadataArrayPtr = Native.grpcsharp_batch_context_recv_status_on_client_trailing_metadata(this);
            var metadata = MetadataArraySafeHandle.ReadMetadataFromPtrUnsafe(metadataArrayPtr);

            return new ClientSideStatus(status, metadata);
        }

        // Gets data of recv_message completion.
        public byte[] GetReceivedMessage()
        {
            IntPtr len = Native.grpcsharp_batch_context_recv_message_length(this);
            if (len == new IntPtr(-1))
            {
                return null;
            }

            if (len == IntPtr.Zero) {
                return EmptyByteArray;
            }

            byte[] data;

            if ((int)len > 1000) {
              // don't allocate anything for benchmark messages
              data = SideBuffer;
            } else {
              data = new byte[(int)len];
            }
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Native.grpcsharp_batch_context_recv_message_to_buffer(this, handle.AddrOfPinnedObject(), new UIntPtr((ulong)len));
            handle.Free();
            return data;
        }

        // Gets data of receive_close_on_server completion.
        public bool GetReceivedCloseOnServerCancelled()
        {
            return Native.grpcsharp_batch_context_recv_close_on_server_cancelled(this) != 0;
        }

        public void Reset()
        {
            Native.grpcsharp_batch_context_reset(this);
        }
            
        protected override bool ReleaseHandle()
        {
            Native.grpcsharp_batch_context_destroy(handle);

            if (SendMessageGCHandle.IsAllocated) {
                SendMessageGCHandle.Free();
            }

            return true;
        }
    }
}
