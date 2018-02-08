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

if("${gRPC_GMOCK_PROVIDER}" STREQUAL "module")
  if(NOT GMOCK_ROOT_DIR)
    set(GMOCK_ROOT_DIR ${CMAKE_CURRENT_SOURCE_DIR}/third_party/googletest/googlemock)
  endif()
  if(EXISTS "${GMOCK_ROOT_DIR}/CMakeLists.txt")
    add_subdirectory(${GMOCK_ROOT_DIR} third_party/googletest/googletest/googlemock)
    # no need for gtest target as gmock already includes gtest
    if(TARGET gmock)
      set(_gRPC_GMOCK_LIBRARIES gmock)
      set(_gRPC_GMOCK_INCLUDE_DIR "${CMAKE_CURRENT_BINARY_DIR}/third_party/googletest/googlemock/include" "${CMAKE_CURRENT_BINARY_DIR}/third_party/googletest/googletest/include")
    endif()
  else()
    message(WARNING "gRPC_GMOCK_PROVIDER is \"module\" but GMOCK_ROOT_DIR is wrong")
  endif()
elseif("${gRPC_GMOCK_PROVIDER}" STREQUAL "package")
  # Use "CONFIG" as there is no built-in cmake module for gmock.
  find_package(gmock REQUIRED CONFIG)
  if(TARGET gmock::gmock)
    set(_gRPC_GMOCK_LIBRARIES gmock::gmock)
    set(_gRPC_GMOCK_INCLUDE_DIR ${GMOCK_INCLUDE_DIR})
  endif()
  # TODO: not necessary!!!
  set(_gRPC_FIND_GMOCK "if(NOT gmock_FOUND)\n  find_package(gmock CONFIG)\nendif()")
endif()
