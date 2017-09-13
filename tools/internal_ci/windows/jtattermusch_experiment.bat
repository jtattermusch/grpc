@rem Copyright 2017 gRPC authors.
@rem
@rem Licensed under the Apache License, Version 2.0 (the "License");
@rem you may not use this file except in compliance with the License.
@rem You may obtain a copy of the License at
@rem
@rem     http://www.apache.org/licenses/LICENSE-2.0
@rem
@rem Unless required by applicable law or agreed to in writing, software
@rem distributed under the License is distributed on an "AS IS" BASIS,
@rem WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
@rem See the License for the specific language governing permissions and
@rem limitations under the License.

@rem Move python installation from _32bit to _32bits where they are expected by python artifact builder
@rem TODO(jtattermusch): get rid of this hack
rename C:\Python27_32bit Python27_32bits
rename C:\Python34_32bit Python34_32bits
rename C:\Python35_32bit Python35_32bits
rename C:\Python36_32bit Python36_32bits

@rem enter repo root
cd /d %~dp0\..\..\..

call tools/internal_ci/helper_scripts/prepare_build_windows.bat

python tools/run_tests/run_tests.py -l c -c dbg --build_only || goto :error

echo "External IP of this machine"
bash -c "curl http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/0/access-configs/0/external-ip -H 'Metadata-Flavor: Google'"
echo

bash -c "echo >>~/.ssh/authorized_keys 'ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDSrt2juWapu+GHlskjeSjIqpR0AjKDBFi/pSk9Q2zq83CBTAFzVQs4/Z/TOXHhmVVDZO9tE3FbtPpDD6L4zvz0dR6UdZ23KV1+uBCCMDmNjC9u06w2LBydRK7tiXgHDzz94e3Z1fqdE3k/XP1YQf/lymynl0CQVSG0bf6hHCvB1Wo5LIkbLtwfgNE0DTaUKHUr8NykVMG9R1MHcDSrY/uWwRV21FBv0fAgEhEVAoP2Q6Eulnlx0gxiIpG1TT/q51fJCZkBOxkyMoecIGedWhgQY3GrM/ywqYQyjYd9V04yIY5swtPUABytcQebgFuOGgWv2qdAapPrOZRWef/Bezqb jtattermusch@jtattermusch0.muc.corp.google.com'"

bash -c "sleep 100000"

goto :EOF

:error
exit /b %errorlevel%
