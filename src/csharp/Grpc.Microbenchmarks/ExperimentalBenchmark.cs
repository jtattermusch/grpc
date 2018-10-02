using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Grpc.Microbenchmarks
{
    [CoreJob]
    [MemoryDiagnoser]
    [RankColumn]
    public class Md5VsSha256
    {
        private SHA256 sha256 = SHA256.Create();
        private MD5 md5 = MD5.Create();
        private byte[] data;

        [Params(10)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        //[Benchmark]
        //public byte[] Md5() => md5.ComputeHash(data);

        // TODO:
        // microbenchmark: reading messages, deserializing messages, parse metadata from native (

        // TODO:
        //microbenchmark: metadataarraysafehandle.create(), fill existing  metadataarraysafehandle

        // TODO:
        //microbenchmark: read metadata from ptr unsafe

        // TODO:
        // microbenchmark of batch context safe handle: get received status on client, get received message, get received initial metadata

       // TODO:
       // microbenchmark of request call context safe handle: getserverrpcnew

       // optimization idea: intern well known metadata keys and values: e.g.   user-agent: 10 bytes key

       // optimization idea: channelsafehandle: create call copies method and host
    }
}