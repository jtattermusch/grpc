#!/bin/bash
# Copyright 2016 gRPC authors.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

set -ex

# change to grpc repo root
cd "$(dirname "$0")/../../.."

cd third_party/protobuf && ./autogen.sh && \
./configure && make -j4 && make install && ldconfig

#RUN apt-get update && apt-get install -y curl unzip automake
#WORKDIR /var/tmp/build-dependencies
#RUN wget -q https://github.com/google/protobuf/archive/v3.5.2.tar.gz
#RUN tar -xf v3.5.2.tar.gz
#WORKDIR /var/tmp/build-dependencies/protobuf-3.5.2
#RUN ./autogen.sh && ./configure && make -j ${NCPU} && make install
#RUN ldconfig

#WORKDIR /var/tmp/build-dependencies
#RUN apt-get remove -y libc-ares-dev libc-ares2
#RUN wget -q https://github.com/c-ares/c-ares/archive/cares-1_14_0.tar.gz
#RUN tar -xf cares-1_14_0.tar.gz
#WORKDIR /var/tmp/build-dependencies/c-ares-cares-1_14_0
#RUN ./buildconf && ./configure && make -j ${NCPU} && make install
#RUN ldconfig

# build gRPC
cd ../.. && make -j4 && make install

cd examples/cpp/helloworld

make
