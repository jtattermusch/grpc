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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using Grpc.Core;
using Grpc.Core.Utils;
using Grpc.Core.Profiling;

namespace Grpc.Core.Internal
{
    /// <summary>
    /// grpc_call from <c>grpc/grpc.h</c>
    /// </summary>
    internal class CallSafeHandle : SafeHandleZeroIsInvalid, INativeCall
    {
        public static readonly CallSafeHandle NullInstance = new CallSafeHandle();
        static readonly NativeMethods Native = NativeMethods.Get();

        // cache handlers to prevent unnecessary delegate allocations.
        static readonly BatchCompletionDelegate UnaryResponseClientHandler = (success, context, cb, obj) => (cb as UnaryResponseClientHandler)(obj, success, context.GetReceivedStatusOnClient(), context.GetReceivedMessage(), context.GetReceivedInitialMetadata());
        static readonly BatchCompletionDelegate ReceivedStatusOnClientHandler = (success, context, cb, obj) => (cb as ReceivedStatusOnClientHandler)(obj, success, context.GetReceivedStatusOnClient());
        static readonly BatchCompletionDelegate SendCompletionHandler = (success, context, cb, obj) => (cb as SendCompletionHandler)(obj, success);
        static readonly BatchCompletionDelegate ReceivedMessageHandler = (success, context, cb, obj) => (cb as ReceivedMessageHandler)(obj, success, context.GetReceivedMessage());
        static readonly BatchCompletionDelegate ReceivedResponseHeadersHandler = (success, context, cb, obj) => (cb as ReceivedResponseHeadersHandler)(obj, success, context.GetReceivedInitialMetadata());
        static readonly BatchCompletionDelegate ReceivedCloseOnServerHandler = (success, context, cb, obj) => (cb as ReceivedCloseOnServerHandler)(obj, success, context.GetReceivedCloseOnServerCancelled());

        const uint GRPC_WRITE_BUFFER_HINT = 1;
        ThreadLocal<SimpleObjectPool<BatchContextSafeHandle>> batchContextPools;
        CompletionQueueSafeHandle completionQueue;

        private CallSafeHandle()
        {
        }

        public void Initialize(GrpcEnvironment environment, CompletionQueueSafeHandle completionQueue)
        {
            this.batchContextPools = environment.ThreadPool.BatchContextPools;
            this.completionQueue = completionQueue;
        }

        public void SetCredentials(CallCredentialsSafeHandle credentials)
        {
            Native.grpcsharp_call_set_credentials(this, credentials).CheckOk();
        }

        public void StartUnary(UnaryResponseClientHandler callback, object userState, byte[] payload, WriteFlags writeFlags, MetadataArraySafeHandle metadataArray, CallFlags callFlags)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, UnaryResponseClientHandler, callback, userState);
                Native.grpcsharp_call_start_unary(this, ctx, payload, new UIntPtr((ulong)payload.Length), writeFlags, metadataArray, callFlags)
                    .CheckOk();
            }
        }

        public void StartUnary(BatchContextSafeHandle ctx, byte[] payload, WriteFlags writeFlags, MetadataArraySafeHandle metadataArray, CallFlags callFlags)
        {
            Native.grpcsharp_call_start_unary(this, ctx, payload, new UIntPtr((ulong)payload.Length), writeFlags, metadataArray, callFlags)
                .CheckOk();
        }

        public void StartClientStreaming(UnaryResponseClientHandler callback, object userState, MetadataArraySafeHandle metadataArray, CallFlags callFlags)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, UnaryResponseClientHandler, callback, userState);
                Native.grpcsharp_call_start_client_streaming(this, ctx, metadataArray, callFlags).CheckOk();
            }
        }

        public void StartServerStreaming(ReceivedStatusOnClientHandler callback, object userState, byte[] payload, WriteFlags writeFlags, MetadataArraySafeHandle metadataArray, CallFlags callFlags)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, ReceivedStatusOnClientHandler, callback, userState);
                Native.grpcsharp_call_start_server_streaming(this, ctx, payload, new UIntPtr((ulong)payload.Length), writeFlags, metadataArray, callFlags).CheckOk();
            }
        }

        public void StartDuplexStreaming(ReceivedStatusOnClientHandler callback, object userState, MetadataArraySafeHandle metadataArray, CallFlags callFlags)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, ReceivedStatusOnClientHandler, callback, userState);
                Native.grpcsharp_call_start_duplex_streaming(this, ctx, metadataArray, callFlags).CheckOk();
            }
        }

        public void StartSendMessage(SendCompletionHandler callback, object userState, byte[] payload, WriteFlags writeFlags, bool sendEmptyInitialMetadata)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, SendCompletionHandler, callback, userState);
                Native.grpcsharp_call_send_message(this, ctx, payload, new UIntPtr((ulong)payload.Length), writeFlags, sendEmptyInitialMetadata ? 1 : 0).CheckOk();
            }
        }

        public void StartSendCloseFromClient(SendCompletionHandler callback, object userState)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, SendCompletionHandler, callback, userState);
                Native.grpcsharp_call_send_close_from_client(this, ctx).CheckOk();
            }
        }

        public void StartSendStatusFromServer(SendCompletionHandler callback, object userState, Status status, MetadataArraySafeHandle metadataArray, bool sendEmptyInitialMetadata,
            byte[] optionalPayload, WriteFlags writeFlags)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                var optionalPayloadLength = optionalPayload != null ? new UIntPtr((ulong)optionalPayload.Length) : UIntPtr.Zero;
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, SendCompletionHandler, callback, userState);
                var statusDetailBytes = MarshalUtils.GetBytesUTF8(status.Detail);
                Native.grpcsharp_call_send_status_from_server(this, ctx, status.StatusCode, statusDetailBytes, new UIntPtr((ulong)statusDetailBytes.Length), metadataArray, sendEmptyInitialMetadata ? 1 : 0,
                    optionalPayload, optionalPayloadLength, writeFlags).CheckOk();
            }
        }

        public void StartReceiveMessage(ReceivedMessageHandler callback, object userState)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, ReceivedMessageHandler, callback, userState);
                Native.grpcsharp_call_recv_message(this, ctx).CheckOk();
            }
        }

        public void StartReceiveInitialMetadata(ReceivedResponseHeadersHandler callback, object userState)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, ReceivedResponseHeadersHandler, callback, userState);
                Native.grpcsharp_call_recv_initial_metadata(this, ctx).CheckOk();
            }
        }

        public void StartServerSide(ReceivedCloseOnServerHandler callback, object userState)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, ReceivedCloseOnServerHandler, callback, userState);
                Native.grpcsharp_call_start_serverside(this, ctx).CheckOk();
            }
        }

        public void StartSendInitialMetadata(SendCompletionHandler callback, object userState, MetadataArraySafeHandle metadataArray)
        {
            using (completionQueue.NewScope())
            {
                var ctx = CreateBatchContext();
                completionQueue.CompletionRegistry.RegisterBatchCompletion(ctx, SendCompletionHandler, callback, userState);
                Native.grpcsharp_call_send_initial_metadata(this, ctx, metadataArray).CheckOk();
            }
        }

        public void Cancel()
        {
            Native.grpcsharp_call_cancel(this).CheckOk();
        }

        public void CancelWithStatus(Status status)
        {
            Native.grpcsharp_call_cancel_with_status(this, status.StatusCode, status.Detail).CheckOk();
        }

        public string GetPeer()
        {
            using (var cstring = Native.grpcsharp_call_get_peer(this))
            {
                return cstring.GetValue();
            }
        }

        public AuthContextSafeHandle GetAuthContext()
        {
            return Native.grpcsharp_call_auth_context(this);
        }

        protected override bool ReleaseHandle()
        {
            Native.grpcsharp_call_destroy(handle);
            return true;
        }

        private static uint GetFlags(bool buffered)
        {
            return buffered ? 0 : GRPC_WRITE_BUFFER_HINT;
        }

        /// <summary>
        /// Only for testing.
        /// </summary>
        public static CallSafeHandle CreateFake(IntPtr ptr, GrpcEnvironment environment, CompletionQueueSafeHandle cq)
        {
            var call = new CallSafeHandle();
            call.SetHandle(ptr);
            call.Initialize(environment, cq);
            return call;
        }
        private BatchContextSafeHandle CreateBatchContext()
        {
            if (!batchContextPools.IsValueCreated)
            {
                return BatchContextSafeHandle.Create();
            }
            var pool = batchContextPools.Value;
            var ctx = pool.Lease();
            ctx.SetPool(pool);
            return ctx;
        }
    }
}
