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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Internal;
using Grpc.Core.Logging;
using Grpc.Core.Utils;
using NUnit.Framework;

namespace Grpc.Core.Tests
{
    public class ChannelThroughputTest
    {
        static readonly ILogger Logger = GrpcEnvironment.Logger.ForType<ChannelThroughputTest>();

        MockServiceHelper helper;
        Server server;
        Channel channel;

        [SetUp]
        public void Init()
        {
            helper = new MockServiceHelper();

            server = helper.GetServer();
            server.Start();
            channel = helper.GetChannel();
        }

        [TearDown]
        public void Cleanup()
        {
            channel.ShutdownAsync().Wait();
            server.ShutdownAsync().Wait();
        }

        [Test]
        public async Task ChannelThroughput_ServerStreaming()
        {
            int counter = 0;

            var task = Task.Run(() =>
            {
                while(true)
                {
                    Logger.Info("TotalMemory (managed) "  + (GC.GetTotalMemory(false) / 1048576) + "MB");

                    Logger.Info("TotalMemory (private memory) " + (Process.GetCurrentProcess().PrivateMemorySize64 / 1048576) + "MB");
                    
                    Logger.Info("Counter " + counter);
                    Thread.Sleep(1000);


                }
            });

            helper.ServerStreamingHandler = new ServerStreamingServerMethod<string, string>(async (request, responseStream, context) =>
            {
                var response = "SDFASFASDFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFSADFSAFSADSADFSADF SAFASDFASDFASDFSADFASDFASDFASDFASDFASDFSADFSADFASDFSADFASDFSADFSADFAFFSADFASDFA";
                var cancellationToken = context.CancellationToken;
                while (!cancellationToken.IsCancellationRequested)
                {
                    await responseStream.WriteAsync(response);
                }
            });

            var call = Calls.AsyncServerStreamingCall(helper.CreateServerStreamingCall(), "ABC");

            while(await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                counter++;
            }
        }
    }
}
