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
cd $(dirname $0)/../../..

mkdir -p cmake/build
cd cmake/build

cmake ../..

ls -lR third_party/zlib
exit 1

# Install zlib using cmake
#ZLIB_VERSION=$(git submodule status third_party/zlib | awk '{print $1}')
ZLIB_VERSION=v1.2.11
git clone -b ${ZLIB_VERSION} https://github.com/madler/zlib.git ~/zlib
(cd ~/zlib; mkdir build; cd build; cmake ..; make install)



make -j4 install

exit 1

