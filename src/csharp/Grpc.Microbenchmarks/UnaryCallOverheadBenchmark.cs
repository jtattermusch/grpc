#region Copyright notice and license

// Copyright 2019 The gRPC Authors
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

using System.Threading.Tasks;
using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Grpc.Core;
using Grpc.Core.Internal;
using System;
using System.Threading;

namespace Grpc.Microbenchmarks
{
    // this test measures the overhead of C# wrapping layer when invoking calls;
    // the marshallers **DO NOT ALLOCATE**, so any allocations
    // are from the framework, not the messages themselves

    [ClrJob, CoreJob] // test .NET Core and .NET Framework
    [MemoryDiagnoser] // allocations
    //[GcMode(Server=true)]
    public class UnaryCallOverheadBenchmark
    {
        private static readonly Task<string> CompletedString = Task.FromResult("");
        private static readonly Marshaller<byte[]> IdentityMarshaller = new Marshaller<byte[]>(msg => msg, payload => payload);
        private static readonly Method<byte[], byte[]> PingMethod = new Method<byte[], byte[]>(MethodType.Unary, nameof(PingBenchmark), "Ping", IdentityMarshaller, IdentityMarshaller);

        private int payloadSize;
        private byte[] payload;

        private readonly ConcurrentQueue<EchoParams> workQueue = new ConcurrentQueue<EchoParams>();
        private readonly ConcurrentQueue<byte[]> resultQueue = new ConcurrentQueue<byte[]>();
        private Thread workerThread;

        NativeMethods native = NativeMethods.Get();

        // size of payload that is sent as request and received as response.
        [Params(0)]
        public int PayloadSize
        {
            get { return payloadSize; }
            set
            {
                payloadSize = value;
                payload = new byte[value];
            }
        }

        [Params(true, false)]
        public bool DispatchToOtherThread
        { get; set; }

        [Benchmark]
        public byte[] SyncUnaryCallOverhead()
        {
            return client.Ping(payload, new CallOptions());
        }

        //[Benchmark]
        //public byte[] SyncUnaryCallOverheadDispatch()
        //{
        //    return PingFromDifferentThread();
        //}

        //[Benchmark]
        public void Sleepnano()
        {
            native.grpcsharp_sleep_micros(100);

        }

        private void StartUnaryEchoDispatch(EchoParams args)
        {
            workQueue.Enqueue(args);
            byte[] result;
            while (!resultQueue.TryDequeue(out result))
            {
            }
            //return result;
        }

        Channel channel;
        PingClient client;

        private void WorkerBody()
        {
            Console.WriteLine("started thread");
            while(true)
            {
                EchoParams arg;
                while (!workQueue.TryDequeue(out arg))
                {
                }
                if (arg.call == null)
                {
                    // exit the worker
                    return;
                }
                //Console.WriteLine("processing");
                native.grpcsharp_test_call_start_unary_echo(arg.call, arg.ctx, arg.sendBuffer, arg.sendBufferLen, arg.writeFlags, arg.metadataArray, arg.metadataFlags);
                
                resultQueue.Enqueue(null);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            // create client, the channel will actually never connect because call logic will be short-circuited
            channel = new Channel("localhost", 10042, ChannelCredentials.Insecure);
            client = new PingClient(new DefaultCallInvoker(channel));

            var native = NativeMethods.Get();

            // replace the implementation of a native method with a fake
            NativeMethods.Delegates.grpcsharp_call_start_unary_delegate fakeCallStartUnary = (CallSafeHandle call, BatchContextSafeHandle ctx, byte[] sendBuffer, UIntPtr sendBufferLen, WriteFlags writeFlags, MetadataArraySafeHandle metadataArray, CallFlags metadataFlags) => {
                if (DispatchToOtherThread)
                {
                    var args = new EchoParams
                    {
                        call = call,
                        ctx = ctx,
                        sendBuffer = sendBuffer,
                        sendBufferLen = sendBufferLen,
                        writeFlags = writeFlags,
                        metadataArray = metadataArray,
                        metadataFlags = metadataFlags

                    };
                    StartUnaryEchoDispatch(args);
                    return CallError.OK;
                }
                else 
                {
                    return native.grpcsharp_test_call_start_unary_echo(call, ctx, sendBuffer, sendBufferLen, writeFlags, metadataArray, metadataFlags);
                }
                
            };
            native.GetType().GetField(nameof(native.grpcsharp_call_start_unary)).SetValue(native, fakeCallStartUnary);

            NativeMethods.Delegates.grpcsharp_completion_queue_pluck_delegate fakeCqPluck = (CompletionQueueSafeHandle cq, IntPtr tag) => {
                return new CompletionQueueEvent {
                    type = CompletionQueueEvent.CompletionType.OpComplete,
                    success = 1,
                    tag = tag
                };
            };
            native.GetType().GetField(nameof(native.grpcsharp_completion_queue_pluck)).SetValue(native, fakeCqPluck);

            workerThread = new Thread(new ThreadStart(WorkerBody));
            workerThread.Start();
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            workQueue.Enqueue(default(EchoParams));
            workerThread.Join();
            await channel.ShutdownAsync();
        }

        class PingClient : LiteClientBase
        {
            public PingClient(CallInvoker callInvoker) : base(callInvoker) { }

            public byte[] Ping(byte[] request, CallOptions options)
            {
                return CallInvoker.BlockingUnaryCall(PingMethod, null, options, request);
            }
        }

         struct EchoParams
        {
            public CallSafeHandle call;
            public BatchContextSafeHandle ctx;
            public byte[] sendBuffer;
            public UIntPtr sendBufferLen;
            public WriteFlags writeFlags;
            public MetadataArraySafeHandle metadataArray;
            public CallFlags metadataFlags;
        }
    }
}
