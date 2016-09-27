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

namespace Grpc.Core.Internal
{
    public class HardwiredUnaryCallAsync
    {
        private object myLock = new object();
        private CallSafeHandle nativeCall;
        private Action<byte[]> okCallback;
        private Action<RpcException> errorCallback;

        public HardwiredUnaryCallAsync(Channel channel, Action<byte[]> okCallback, Action<RpcException> errorCallback)
        {
            this.okCallback = okCallback;
            this.errorCallback = errorCallback;
            this.nativeCall = channel.Handle.CreateCall(CallSafeHandle.NullInstance, ContextPropagationToken.DefaultMask,
                channel.CompletionQueue, "/grpc.testing.BenchmarkService/UnaryCall", null, Timespec.InfFuture, null);
            using (var metadataArray = MetadataArraySafeHandle.Create(null))
            {
                nativeCall.StartUnary(HandleUnaryResponse, new byte[0], metadataArray, default(WriteFlags));
            }
        }

        /// <summary>
        /// Handler for unary response completion.
        /// </summary>
        private void HandleUnaryResponse(bool success, ClientSideStatus receivedStatus, byte[] receivedMessage, Metadata responseHeaders)
        {
            // NOTE: because this event is a result of batch containing GRPC_OP_RECV_STATUS_ON_CLIENT,
            // success will be always set to true.

            //TResponse msg = default(TResponse);
            //var deserializeException = TryDeserialize(receivedMessage, out msg);

            lock (myLock)
            {
                nativeCall.Dispose();
            }

            //responseHeadersTcs.SetResult(responseHeaders);

            var status = receivedStatus.Status;
            if (status.StatusCode != StatusCode.OK)
            {
                errorCallback(new RpcException(status));
                return;
            }

            okCallback(receivedMessage);
        }
    }
}
