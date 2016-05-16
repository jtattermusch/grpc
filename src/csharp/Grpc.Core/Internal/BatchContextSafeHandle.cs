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
    /// <summary>
    /// grpcsharp_batch_context
    /// </summary>
    internal class BatchContextSafeHandle : SafeHandleZeroIsInvalid
    {
        static readonly NativeMethods Native = NativeMethods.Get();

        private BatchContextSafeHandle()
        {
        }

        public static BatchContextSafeHandle Create()
        {
            return Native.grpcsharp_batch_context_create();
        }

        public IntPtr Handle
        {
            get
            {
                return handle;
            }
        }
            
        protected override bool ReleaseHandle()
        {
            Native.grpcsharp_batch_context_destroy(handle);
            return true;
        }
    }

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
	}

    /// <summary>
    /// Status + metadata received on client side when call finishes.
    /// (when receive_status_on_client operation finishes).
    /// </summary>
    internal struct ClientSideStatus
    {
        readonly Status status;
        readonly Metadata trailers;

        public ClientSideStatus(Status status, Metadata trailers)
        {
            this.status = status;
            this.trailers = trailers;
        }

        public Status Status
        {
            get
            {
                return this.status;
            }    
        }

        public Metadata Trailers
        {
            get
            {
                return this.trailers;
            }
        }
    }

    /// <summary>
    /// Details of a newly received RPC.
    /// </summary>
    internal struct ServerRpcNew
    {
        readonly Server server;
        readonly CallSafeHandle call;
        readonly string method;
        readonly string host;
        readonly Timespec deadline;
        readonly Metadata requestMetadata;

        public ServerRpcNew(Server server, CallSafeHandle call, string method, string host, Timespec deadline, Metadata requestMetadata)
        {
            this.server = server;
            this.call = call;
            this.method = method;
            this.host = host;
            this.deadline = deadline;
            this.requestMetadata = requestMetadata;
        }

        public Server Server
        {
            get
            {
                return this.server;
            }
        }

        public CallSafeHandle Call
        {
            get
            {
                return this.call;
            }
        }

        public string Method
        {
            get
            {
                return this.method;
            }
        }

        public string Host
        {
            get
            {
                return this.host;
            }
        }

        public Timespec Deadline
        {
            get
            {
                return this.deadline;
            }
        }

        public Metadata RequestMetadata
        {
            get
            {
                return this.requestMetadata;
            }
        }
    }
}
