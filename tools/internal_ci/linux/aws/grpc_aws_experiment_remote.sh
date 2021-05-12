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

cd grpc

# without port server running, many tests will fail
# TODO(jtattermusch): redirecting the output and executing under nohup currently
# seems neccessary to avoid the SSH session from kokoro worker from hanging after
# this script finishes. With the current way of starting the port server things
# work, but it's something that needs further investigation.
nohup python tools/run_tests/start_port_server.py >/dev/null 2>&1

# test gRPC C/C++ with bazel
tools/bazel test --config=opt --test_output=errors --test_tag_filters=-no_linux --build_tag_filters=-no_linux --flaky_test_attempts=1 --runs_per_test=1 //test/...
