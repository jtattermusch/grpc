#!/bin/bash

# This script is responsible for remotely running tests on an ARM instance.
# It should return a status code useful to the kokoro infrastructure.
# It currently assumes an instance will be selected by the time this script begins running.

if [ -z "$KOKORO_KEYSTORE_DIR" ]; then
    echo "KOKORO_KEYSTORE_DIR is unset. This must be run from kokoro"
    exit 1
fi

IDENTITY=${KOKORO_KEYSTORE_DIR}/73836_grpc_arm_instance_private_key

if [ -z "$INSTANCE" ]; then
    echo "no INSTANCE set, aborting"
    exit 1
fi

FILE=grpc_aws_experiment_remote.sh
scp -i $IDENTITY tools/internal_cli/linux/$FILE $INSTANCE:
ssh -i $IDENTITY $INSTANCE "uname -a; ls -l; bash ./$FILE"
