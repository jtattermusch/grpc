#!/usr/bin/env bash
# Copyright 2021 The gRPC Authors
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

#install ubuntu pre-requisites
sudo apt update
sudo apt install -y build-essential autoconf libtool pkg-config cmake python python-pip clang
sudo pip install six

sudo apt install -y python3.9 python3.9-pip || true
sudo apt install -y python3.8 python3.8-pip || true
sudo apt install -y python3.7 python3.7-pip || true

sudo apt install -y python3 python3-pip

cd grpc

git submodule update --init

python --version || true
python2 --version || true
python3 --version || true

# build and test python
tools/run_tests/run_tests.py -l python --compiler python3.6 -c opt 

# pythonasyncio
