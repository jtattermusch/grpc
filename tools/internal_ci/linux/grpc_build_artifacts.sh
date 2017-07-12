#!/bin/bash
# Copyright 2017 gRPC authors.
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

source tools/internal_ci/helper_scripts/prepare_build_linux_rc

# TODO(jtattermusch): install ruby on the internal_ci worker
gpg --keyserver hkp://keys.gnupg.net --recv-keys 409B6B1796C275462A1703113804BB82D39DC0E3
# TODO(jtattermusch): grep works around https://github.com/rvm/rvm/issues/4068
curl -sSL https://get.rvm.io | grep -v __rvm_print_headline | bash -s stable --ruby
#curl -sSL https://get.rvm.io | bash -s stable
#set +ex  # rvm script is very verbose and exits with errorcode
#source $HOME/.rvm/scripts/rvm
#set -e  # rvm commands are very verbose
#rvm install ruby-2.4
#set -ex

tools/run_tests/task_runner.py -f artifact linux
