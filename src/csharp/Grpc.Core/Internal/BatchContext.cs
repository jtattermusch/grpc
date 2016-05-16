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
using Grpc.Core;

namespace Grpc.Core.Internal
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct BatchContext
	{
        static readonly NativeMethods Native = NativeMethods.Get();

        public CompletionQueueEvent cqEvent;
		public MetadataArray recvInitialMetadata;
		public IntPtr recvMessage;
		public UIntPtr recvMessageLength;
		public RecvStatusOnClient recvStatusOnClient;
		public int recvCloseOnServerCancelled;
		public ServerRpcNewNative serverRpcNew;

		MetadataArray sendInitialMetadata;
		IntPtr sendMessage;
	    MetadataArray trailingMetadata;  // send_status_from_server
	    IntPtr statusDetails;  // send_status_from_server

        // Gets data of recv_status_on_client completion.
        public ClientSideStatus GetReceivedStatusOnClient()
        {
            string details = Marshal.PtrToStringAnsi(recvStatusOnClient.statusDetails);
            var status = new Status((StatusCode) recvStatusOnClient.status, details);
            var metadata = MetadataArraySafeHandle.ReadMetadataFromStructUnsafe(recvStatusOnClient.trailingMetadata);

            return new ClientSideStatus(status, metadata);
        }

        // Gets data of recv_initial_metadata completion.
        public Metadata GetReceivedInitialMetadata()
        {
            return MetadataArraySafeHandle.ReadMetadataFromStructUnsafe(recvInitialMetadata);
        }

        // Gets data of recv_message completion.
        public byte[] GetReceivedMessage()
        {
            if (recvMessage == IntPtr.Zero)
            {
                return null;
            }

            ulong len = recvMessageLength.ToUInt64();
            byte[] data = new byte[len];
            if (len == 0)
            {
                return data;
            }
            Native.grpcsharp_byte_buffer_read(recvMessage, data, recvMessageLength);
            return data;
        }

        public bool GetReceivedCloseOnServerCancelled()
        {
            return recvCloseOnServerCancelled != 0;
        }
            
        // Gets data of server_rpc_new completion.
        public ServerRpcNew GetServerRpcNew(Server server)
        {
            var call = new CallSafeHandle(serverRpcNew.call);  // TODO:
            var method = Marshal.PtrToStringAnsi(serverRpcNew.callDetails.method);
            var host = Marshal.PtrToStringAnsi(serverRpcNew.callDetails.host);
            var deadline = serverRpcNew.callDetails.deadline;
            var metadata = MetadataArraySafeHandle.ReadMetadataFromStructUnsafe(serverRpcNew.requestMetadata);
            return new ServerRpcNew(server, call, method, host, deadline, metadata);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RecvStatusOnClient
        {
            public MetadataArray trailingMetadata;
            public int status;
            public IntPtr statusDetails;
            public UIntPtr statusDetailsCapacity;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ServerRpcNewNative
        {
            public IntPtr call;
            public CallDetails callDetails;
            public MetadataArray requestMetadata;
        }

        // corresponds to struct grpc_medatata
        [StructLayout(LayoutKind.Sequential)]
        internal struct MetadataNative
        {
            public IntPtr key;
            public IntPtr value;
            public UIntPtr valueLength;
            public uint flags;

            IntPtr obfuscated0;
            IntPtr obfuscated1;
            IntPtr obfuscated2;
            IntPtr obfuscated3;
        }

        // corresponds to struct grpc_medatata_array
        [StructLayout(LayoutKind.Sequential)]
        internal struct MetadataArray
        {
            public UIntPtr count;
            public UIntPtr capacity;
            public IntPtr metadata;
        }

        // corresponds to grpc_call_details
        [StructLayout(LayoutKind.Sequential)]
        internal struct CallDetails
        {
            public IntPtr method;
            public UIntPtr methodCapacity;
            public IntPtr host;
            public UIntPtr hostCapacity;
            public Timespec deadline;
            public uint flags;
            public IntPtr reserved;
        }
	}
}
