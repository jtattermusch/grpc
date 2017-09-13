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

source tools/internal_ci/helper_scripts/prepare_build_macos_rc

echo >>~/.ssh/authorized_keys 'ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDSrt2juWapu+GHlskjeSjIqpR0AjKDBFi/pSk9Q2zq83CBTAFzVQs4/Z/TOXHhmVVDZO9tE3FbtPpDD6L4zvz0dR6UdZ23KV1+uBCCMDmNjC9u06w2LBydRK7tiXgHDzz94e3Z1fqdE3k/XP1YQf/lymynl0CQVSG0bf6hHCvB1Wo5LIkbLtwfgNE0DTaUKHUr8NykVMG9R1MHcDSrY/uWwRV21FBv0fAgEhEVAoP2Q6Eulnlx0gxiIpG1TT/q51fJCZkBOxkyMoecIGedWhgQY3GrM/ywqYQyjYd9V04yIY5swtPUABytcQebgFuOGgWv2qdAapPrOZRWef/Bezqb jtattermusch@jtattermusch0.muc.corp.google.com'
echo 'ssh kbuilder@THIS_MACHINE_IP'

sleep 100000
