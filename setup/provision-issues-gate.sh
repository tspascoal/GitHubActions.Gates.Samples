#!/bin/bash

### variables to call external dependencies stored in the same folder
script_path=$(dirname "$0")
###

export COMMAND_NAME
COMMAND_NAME=$(basename "$0")
export SP_NAME="issues-gate-sp"
export APP_SHORT_NAME="issues"
export SERVICE_BUS_QUEUE_NAME="issuesProcessing"
export VARPREFIX="ISSUES_GATE"

"${script_path}/_setup-deploy.sh" "$@"