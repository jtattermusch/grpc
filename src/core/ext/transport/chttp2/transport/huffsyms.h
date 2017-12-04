/*
 *
 * Copyright 2015 gRPC authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

#ifndef GRPC_CORE_EXT_TRANSPORT_CHTTP2_TRANSPORT_HUFFSYMS_H
#define GRPC_CORE_EXT_TRANSPORT_CHTTP2_TRANSPORT_HUFFSYMS_H

/* HPACK static huffman table */


 #ifdef __cplusplus
 extern "C" {
 #endif

#define GRPC_CHTTP2_NUM_HUFFSYMS 257

typedef struct {
  unsigned bits;
  unsigned length;
} grpc_chttp2_huffsym;

extern const grpc_chttp2_huffsym grpc_chttp2_huffsyms[GRPC_CHTTP2_NUM_HUFFSYMS];

#ifdef __cplusplus
}
#endif

#endif /* GRPC_CORE_EXT_TRANSPORT_CHTTP2_TRANSPORT_HUFFSYMS_H */
