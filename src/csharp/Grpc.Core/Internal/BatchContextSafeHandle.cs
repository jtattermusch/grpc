#region Copyright notice and license

// Copyright 2015 gRPC authors.
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
using System.Runtime.InteropServices;
using System.Text;
using Grpc.Core;
using Grpc.Core.Logging;
using Grpc.Core.Utils;

namespace Grpc.Core.Internal
{
    internal interface IOpCompletionCallback
    {
        void OnComplete(bool success);
    }

    internal interface IBufferReader
    {
        long? TotalLength { get; }

        bool TryGetNextSlice(out Slice slice);
    }

    /// <summary>
    /// grpcsharp_batch_context
    /// </summary>
    internal class BatchContextSafeHandle : SafeHandleZeroIsInvalid, IOpCompletionCallback, IPooledObject<BatchContextSafeHandle>, IBufferReader
    {
        static readonly NativeMethods Native = NativeMethods.Get();
        static readonly ILogger Logger = GrpcEnvironment.Logger.ForType<BatchContextSafeHandle>();

        Action<BatchContextSafeHandle> returnToPoolAction;
        CompletionCallbackData completionCallbackData;

        private BatchContextSafeHandle()
        {
        }

        public static BatchContextSafeHandle Create()
        {
            var ctx = Native.grpcsharp_batch_context_create();
            return ctx;
        }

        public IntPtr Handle
        {
            get
            {
                return handle;
            }
        }

        public void SetReturnToPoolAction(Action<BatchContextSafeHandle> returnAction)
        {
            GrpcPreconditions.CheckState(returnToPoolAction == null);
            returnToPoolAction = returnAction;
        }

        public void SetCompletionCallback(BatchCompletionDelegate callback, object state)
        {
            GrpcPreconditions.CheckState(completionCallbackData.Callback == null);
            GrpcPreconditions.CheckNotNull(callback, nameof(callback));
            completionCallbackData = new CompletionCallbackData(callback, state);
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
            string details = MarshalUtils.PtrToStringUTF8(detailsPtr, (int)detailsLength.ToUInt32());
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
            byte[] data = new byte[(int)len];
            Native.grpcsharp_batch_context_recv_message_to_buffer(this, data, new UIntPtr((ulong)data.Length));
            return data;
        }

        public bool GetReceivedMessageNextSlice(out Slice slice)
        {
            // TODO: calling this when receivedMessage is null will lead to crash
            UIntPtr sliceLen;
            IntPtr sliceDataPtr;
            Slice.InlineData inlineBuffer;
            if (0 == Native.grpcsharp_batch_context_recv_message_next_slice(this, out sliceLen, out sliceDataPtr, out inlineBuffer, new UIntPtr((uint) Slice.InlineDataMaxLength)))
            {
                slice = default(Slice);
                return false;
            }
            slice = new Slice((long) sliceLen, sliceDataPtr, inlineBuffer);
            return true;
        }

        public ReadOnlySliceBuffer GetReceivedMessageAsSliceBuffer(ReadOnlySliceBuffer sliceBuffer)
        {
            if (Native.grpcsharp_batch_context_recv_message_length(this) == new IntPtr(-1))
            {
                // -1 means that the received message is NULL.
                return null;
            }

            if (sliceBuffer != null)
            {
                sliceBuffer.Reset();  // make sure it's empty
            } else
            {
                sliceBuffer = new ReadOnlySliceBuffer(100);  // use buffer with some default capacity
            }

            while (GetReceivedMessageNextSlice(out Slice slice))
            {
                if (sliceBuffer.SliceCount >= sliceBuffer.Capacity)
                {
                    // if the slices won't fit into the current sliceBuffer, we need to allocate a new one
                    // but this should happen very rarely.
                    var oldSliceBuffer = sliceBuffer;
                    sliceBuffer = sliceBuffer.Copy(sliceBuffer.Capacity * 2);
                    oldSliceBuffer.Reset();
                }
                sliceBuffer.AddSlice(slice);
            }

            return sliceBuffer;
        }

        public IBufferReader GetReceivedMessageReader()
        {
            return this;
        }

        // Gets data of receive_close_on_server completion.
        public bool GetReceivedCloseOnServerCancelled()
        {
            return Native.grpcsharp_batch_context_recv_close_on_server_cancelled(this) != 0;
        }

        public void Recycle()
        {
            if (returnToPoolAction != null)
            {
                Native.grpcsharp_batch_context_reset(this);

                var origReturnAction = returnToPoolAction;
                // Not clearing all the references to the pool could prevent garbage collection of the pool object
                // and thus cause memory leaks.
                returnToPoolAction = null;
                origReturnAction(this);
            }
            else
            {
                Dispose();
            }
        }

        protected override bool ReleaseHandle()
        {
            Native.grpcsharp_batch_context_destroy(handle);
            return true;
        }

        void IOpCompletionCallback.OnComplete(bool success)
        {
            try
            {
                completionCallbackData.Callback(success, this, completionCallbackData.State);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Exception occurred while invoking batch completion delegate.");
            }
            finally
            {
                completionCallbackData = default(CompletionCallbackData);
                Recycle();
            }
        }

        long? IBufferReader.TotalLength
        {
            get
            {
                var len = Native.grpcsharp_batch_context_recv_message_length(this);
                return len != new IntPtr(-1) ? (long?) len : null;
            }
        }

        bool IBufferReader.TryGetNextSlice(out Slice slice)
        {
            return GetReceivedMessageNextSlice(out slice);
        }

        struct CompletionCallbackData
        {
            public CompletionCallbackData(BatchCompletionDelegate callback, object state)
            {
                this.Callback = callback;
                this.State = state;
            }

            public BatchCompletionDelegate Callback { get; }
            public object State { get; }
        }
    }
}
