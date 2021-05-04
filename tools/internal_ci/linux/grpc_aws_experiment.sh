#!/bin/bash -e

# This script is responsible for remotely running tests on an ARM instance.
# It should return a status code useful to the kokoro infrastructure.
# It currently assumes an instance will be selected by the time this script begins running.

if [ -z "$KOKORO_KEYSTORE_DIR" ]; then
    echo "KOKORO_KEYSTORE_DIR is unset. This must be run from kokoro"
    exit 1
fi

IDENTITY=${KOKORO_KEYSTORE_DIR}/73836_grpc_arm_instance_ssh_private_test_key1
AWS_CREDENTIALS=${KOKORO_KEYSTORE_DIR}/73836_grpc_aws_ec2_credentials

if [ -z "$INSTANCE" ]; then
    echo "no INSTANCE set, aborting"
    exit 1
fi

# Spawn an instance for running the workflow
## Setup aws cli
# debug linker
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install 
aws --version

# authenticate with aws cli
mkdir ~/.aws/
echo "[default]" >> ~/.aws/config
ln -s $AWS_CREDENTIALS ~/.aws/credentials
echo "debug cred"
head -c 11 ~/.aws/credentials
echo "debug config"
head -c 11 ~/.aws/config

# setup instance 
sudo apt update && sudo apt install -y jq
# debug date skew
date
chronyd -q
date
# ubuntu 20.04 lts(arm64), micro (TODO)
# aws ec2 run-instances --image-id ami-064446ad1d755489e --region us-east-2
aws ec2 describe-regions --region us-east-2 --debug
# TODO: auto shutdown
exit



FILE=grpc_aws_experiment_remote.sh
chmod 700 $IDENTITY
REMOTE_SCRIPT_FAILURE=0
ssh -i $IDENTITY -o StrictHostKeyChecking=no ubuntu@$INSTANCE "rm -rf grpc"
scp -i $IDENTITY -o StrictHostKeyChecking=no -r github/grpc ubuntu@$INSTANCE:
ssh -i $IDENTITY -o StrictHostKeyChecking=no ubuntu@$INSTANCE "uname -a; ls -l; bash grpc/tools/internal_ci/linux/$FILE" || REMOTE_SCRIPT_FAILURE=$?

# Sync back sponge_log artifacts (wip)
# echo "looking for sponge logs..."
# find . | grep sponge_log


# Match return value
echo "returning $REMOTE_SCRIPT_FAILURE based on script output"
exit $REMOTE_SCRIPT_FAILURE
