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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core.Internal;
using Grpc.Core.Logging;
using Grpc.Core.Profiling;
using Grpc.Core.Utils;

namespace Grpc.Core.Internal
{
    // key for a dict that's either a managed byte array or a pointer to native memory.
    internal struct ManagedOrNativeBytes : IEquatable<ManagedOrNativeBytes>
    {
        byte[] managedValue;
        IntPtr nativeValue;
        int nativeLength;

        public ManagedOrNativeBytes(byte[] managedValue)
        {
            // TODO: add defensive copy...
            this.managedValue = GrpcPreconditions.CheckNotNull(managedValue);
            this.nativeValue = IntPtr.Zero;
            this.nativeLength = 0;
        }

        public ManagedOrNativeBytes(IntPtr nativeValue, int nativeLength)
        {
            GrpcPreconditions.CheckArgument(nativeValue != IntPtr.Zero);
            GrpcPreconditions.CheckArgument(nativeLength >= 0);
            this.managedValue = null;
            this.nativeValue = nativeValue;
            this.nativeLength = nativeLength;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ManagedOrNativeBytes))
            {
                return false;
            }
            return Equals((ManagedOrNativeBytes)obj);
        }

        public bool Equals(ManagedOrNativeBytes other)
        {
            unsafe
            {
                if (this.managedValue != null)
                {
                    if (other.managedValue != null)
                    {
                        fixed (byte *buffer1 = this.managedValue)
                        fixed (byte *buffer2 = other.managedValue)
                        {
                            return EqualsUnsafe(buffer1, this.managedValue.Length, buffer2, other.managedValue.Length);
                        }
                    }
                    else
                    {
                        fixed (byte *buffer1 = this.managedValue)
                        {
                            return EqualsUnsafe(buffer1, this.managedValue.Length, (byte*) other.nativeValue, other.nativeLength);
                        }
                    }
                } 
                else
                {
                    if (other.managedValue != null)
                    {
                        fixed (byte *buffer2 = other.managedValue)
                        {
                            return EqualsUnsafe((byte*) this.nativeLength, this.nativeLength, buffer2, other.managedValue.Length);
                        }
                    }
                    else
                    {
                        return EqualsUnsafe((byte*) this.nativeLength, this.nativeLength, (byte*) other.nativeValue, other.nativeLength);
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            unsafe
            {
              if (managedValue != null)
              {
                  fixed (byte *buffer = managedValue)
                  {
                      return GetHashCodeUnsafe(buffer, managedValue.Length);
                  }
              }
              return GetHashCodeUnsafe((byte *)nativeValue, nativeLength);
            }
        }

        private static unsafe int GetHashCodeUnsafe(byte *buffer, int len)
        {
            int hash = 31;
            for (int i = 0; i < len; i++)
            {
                hash = (hash << 5) ^ buffer[i];
            }
            return hash;
        }

        private static unsafe bool EqualsUnsafe(byte *buffer1, int len1, byte *buffer2, int len2)
        {
            if (len1 != len2)
            {
                return false;
            }
            for (int i = 0; i < len1; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
