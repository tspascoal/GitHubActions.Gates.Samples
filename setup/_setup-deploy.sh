#!/bin/bash

## Provisions a gate and configures it.
## Not meant to be called directly (but you can if you want to).

set -e

### CONSTANTS

info_color="\033[0;36m"
green_color="\033[0;32m"
error_color="\033[0;31m"
warn_color="\033[0;33m"
yellow_color="\033[1;33m"

export TZ=UTC

### variables to call external dependencies stored in the same folder
script_path=$(dirname "$0")
###

### Variables

PrintUsage()
{
  COMMAND=${COMMAND_NAME:-$(basename "$0")}

  cat <<EOM
Usage: ${COMMAND} [options]

Options:
    -h, --help                    : Show script help

    -a, --app-id                  : GitHub application ID

    --function-name               : Function name. This name needs to be globally unique on Azure

    --function-short-name         : Function short name, this will be used for azure resources prefix to make it easier to
                                    identify them by name. Must be 6 characters or less.

                                    Default value: [$APP_SHORT_NAME]
 
    -k, --key-file                : Path to the private key for the GitHub application (PEM format)

    --webhook-secret              : The WebHook secret to use for the GitHub application
                                    Optional but recommended.

    -g, --resource-group          : Resource group to provision the resources in (it will be created if it doesn't exist)

    -l --location                 : Location to provision the resources in (default: [$LOCATION])

    --app-insights-location       : Location to provision application insights (default: [$APPINSIGHTS_LOCATION])

    --sp-name                     : Name of the service principal to create (default: [$SP_NAME])

                                    Warning: If you specify a name that already exists, the script will reuse it and
                                    WILL delete all credentials associated with it.

    -r, --repo                    : Repository to store the variables and secrets necessary to deploy the function.
                                    This will generate a Service Principal in the subscription with a federated OIDC credential and
                                    store it in the repo as a secret. The Service Principal will be granted the Contributor role in the
                                    resource group

                                    If not specified, you will need to manually generate the credentials necessary for deployment.
    --var-prefix                  : Prefix to use for the variables stored in the repo (default: [$VARPREFIX])

    --set-ip-restrictions         : (flag) Set IP restrictions on the function app to only allow traffic from the GitHub hook IP addresses.
                                    Fetches the values automatically from GitHub Meta API (default: false)

Description:

Examples:
  ${COMMAND} -a 123 -k certificate.pem -g deployhours-gate --set-ip-restrictions
  ${COMMAND} --app-id 123 --key-file certificate.pem -g deployhours-gate --function-name deployhours-gate-test --function-short-name deplyt --repo mona/lisa

EOM
  exit 0
}

####################################
# Default Values
# ##################################

LOCATION="eastus"
APPINSIGHTS_LOCATION=$LOCATION

####################################
# Read in the parameters if passed #
####################################
PARAMS=""
while [[ $# -gt 0 ]]; do
  case $1 in
    -h|--help)
      PrintUsage;
      ;;
    -g|--resource-group)
      RG=$2
      shift 2
      ;;
    -l|--location)
      LOCATION=$2
      shift 2
      ;;
    --app-insights-location)
      APPINSIGHTS_LOCATION=$2
      shift 2
      ;;
    -a|--app-id)
      APP_ID=$2
      shift 2
      ;;
    -k|--key-file)
      KEY_FILE=$2
      shift 2
      ;;
    -r|--repo)
      REPO=$2
      shift 2
      ;;
    --sp-name)
      SP_NAME=$2
      shift 2
      ;;
    --webhook-secret)
      WEBHOOK_SECRET=$2
      shift 2
      ;;
    --function-name)
      APP_NAME=$2
      shift 2
      ;;
    --function-short-name)
      APP_SHORT_NAME=$2
      shift 2
      ;;
    --var-prefix)
      VARPREFIX=$2
      shift 2
      ;;
    --set-ip-restrictions)
      SET_IP_RESTRICTION=true
      shift
      ;;
    --) # end argument parsing
      shift
      break
      ;;
    -*) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *) # preserve positional arguments
  PARAMS="$PARAMS $1"
  shift
  ;;
  esac
done

function validateParameters()
{
  local is_missing=0

  if [ -z "$APP_NAME" ]; then
    is_missing=1
    echo -e "${error_color}function name required."
  fi

  if [ -z "$APP_ID" ]; then
    is_missing=1
    echo -e "${error_color}application id required."
  fi

  if [ -z "$KEY_FILE" ]; then
    is_missing=1
    echo -e "${error_color}application private key required."
  fi

  if [ -z "$RG" ]; then
      is_missing=1
      echo -e "${error_color}resource group required."
  fi

  if [ -n "$REPO" ]; then
    if [[ $REPO != */* ]]; then
      echo -e "${error_color}Error: repo must be in the format owner/repo"
      is_missing=1
    fi
  fi

  if [ "$is_missing" -eq 1 ]; then
    echo -e "\n${error_color}Please see usage for more details (--help)."
    exit 1
  fi

  if [ ! -f "$KEY_FILE" ]; then
      echo -e "${error_color}Error: private key file not found: $KEY_FILE"
      exit 1
  fi

  # validate repos
  if [ -n "$REPO" ]; then
    if ! gh api "repos/$REPO" --jq .id > /dev/null
    then
      echo -e "${error_color}Error: repo not found or you don't have permissions: $REPO"
      exit 1
    fi
  fi
}

function validateRequirements()
{
    # TODO: only do this if repo has been specified
    # TODO: validate scopes?
    if ! command -v gh &> /dev/null
    then
        echo "gh could not be found. You need to install gh (see https://cli.github.com)"
        exit
    fi

    if ! command -v az &> /dev/null
    then
        echo "Azure CLI not found. You need to install Azure CLI  (see https://aka.ms/azcli)"
        exit
    fi

    if ! command -v jq &> /dev/null
    then
        echo "JQ not found. You need to install JQ (see https://stedolan.github.io/jq)"
        exit
    fi


    if ! az account show --query id > /dev/null
    then
        echo "You need to login to Azure CLI (see https://aka.ms/azcli)"
        exit
    fi
}

function printWarnings()
{
  if [ -z "$REPO" ]; then
    echo -e "${warn_color}Warning: You have ommited repo parameter. Will not generate a service principal."
    return
  fi
}

function printSubscription()
{
  local subscription
  subscription=$(az account show --query name --output tsv)
  echo -e "${info_color}\nUsing subscription:${yellow_color} $subscription"
}

function printGitHubUser()
{
  if [ -z "$REPO" ]; then
    return
  fi
  echo -e "${info_color}Using GitHub user: $(gh api user --jq .login)"
}

createFederatedCredential() {
  local appid=$1
  local federatedcredentialname=$2
  local subject=$3
  local description=$4
  local type=$5

  # check if federated credential already exists
  if az ad app federated-credential show --id "$appid" --federated-credential-id "$federatedcredentialname" --only-show-errors --query id 2>/dev/null; then
    echo -e "${warn_color}   Federated credential $federatedcredentialname already exists. Will skip variable creation."
    echo -e "\n${warn_color}   If you want to recreate the federated credential, delete it first, with following command:"
    echo -e "${info_color}az ad app federated-credential delete --id $appid --federated-credential-id $federatedcredentialname"
  else
    echo -e "${warn_color}   Warning: The federated credential can only be used to deploy from $type"
    credential=$(jq -c -n '{name: $name ,issuer:"https://token.actions.githubusercontent.com",
            subject: $subject,
            description: $description, audiences: ["api://AzureADTokenExchange"]}' \
        --arg subject "$subject" \
        --arg name "$federatedcredentialname" \
        --arg description "$description")

    az ad app federated-credential create --id "$appid" --parameters "$credential" --only-show-errors > /dev/null
  fi
}

createOrUpdateRepoVariable() {
  local name=$1
  local value=$2
  local repo=$3

  name=$(echo -n "$name" | tr '[:lower:]' '[:upper:]')

  if ! gh api "repos/$repo/actions/variables/$name" > /dev/null 2>&1; then
    echo -e "${info_color}  creating variable ${yellow_color}$name"
    gh api --method POST -H "X-GitHub-Api-Version: 2022-11-28" \
    "/repos/$repo/actions/variables" \
    -f name="$name" -f value="$value" > /dev/null
  else
    echo -e "${info_color}  updating variable ${yellow_color}$name"
      gh api --method PATCH -H "X-GitHub-Api-Version: 2022-11-28" \
    "/repos/$repo/actions/variables/$name" \
    -f name="$name" -f value="$value" > /dev/null
  fi
}

setRepoSecret() {
  local name=$1
  local value=$2
  local repo=$3

  name=$(echo -n "$name" | tr '[:lower:]' '[:upper:]')

  echo -e "${info_color}  setting secret ${yellow_color}$name"
  gh secret set "$name" -b "$value" -R "$repo" > /dev/null
}

function assign_role() {
  local scope=$1
  local app_id=$2
  local role=$3

  echo -e "${info_color} Assigning ${yellow_color}$role${info_color} role to ${yellow_color}$app_id${info_color} on [${yellow_color}$scope${info_color}]"

  local assignment_id
  assignment_id=$(az role assignment list --assignee "$app_id" --role "$role" --scope "$scope" --query '[].id' --output tsv)
  if [ -z "$assignment_id" ]; then
    az role assignment create --assignee "$app_id" --role "$role" --scope "$scope" > /dev/null
  else
    echo -e "${info_color}    Already assigned. Skipping it."
  fi
}

###

############# Begin

validateParameters
validateRequirements

printWarnings

printSubscription

# Create RG if it doesn't exist
echo -e "${info_color}Creating Resource Group ${yellow_color}$RG${info_color} in ${yellow_color}$LOCATION${info_color} if it doesn't exist"
resourceGroupId=$(az group create --name "$RG" --location "$LOCATION" --only-show-errors | jq -r .id)

# Deploy the resources
echo -e "\n${green_color}Deploying resources to ${yellow_color}$RG:"
echo -e "${info_color}  Function App Name:\t\t\t $APP_NAME"
echo -e "${info_color}  Function App Short Name:\t\t $APP_SHORT_NAME"
echo -e "${info_color}  Service Bus Queue Name:\t\t $SERVICE_BUS_QUEUE_NAME"
echo -e "${info_color}  App Insights Location:\t\t $APPINSIGHTS_LOCATION"
echo -e "${info_color}  GitHub Application ID:\t\t $APP_ID"
echo -e "${info_color}  GitHub Application Private Key:\t $KEY_FILE"
echo ""

hooksIps="[]"
if [ "$SET_IP_RESTRICTION" == true ]; then  
  hooksIps=$(gh api meta | jq -c .hooks)
  echo -e "${info_color}  Setting IP restriction to ${yellow_color}$hooksIps${info_color}"
else 
  echo -e "${info_color}  Not setting IP restriction"
fi
echo ""
deployOutput=$(az deployment group create --resource-group "$RG" \
    --template-file "${script_path}\..\IaC\Gate.bicep" \
    --query properties.outputs \
    --parameters appName="$APP_NAME" \
        appShortName="$APP_SHORT_NAME" \
        serviceBusQueueName="$SERVICE_BUS_QUEUE_NAME" \
        appInsightsLocation="$APPINSIGHTS_LOCATION" GHApplicationId="$APP_ID" \
        certificate=@"$KEY_FILE" \
        webHookSecret="$WEBHOOK_SECRET" \
        ghHooksIpAddresses="$hooksIps" \
    --query 'properties.outputs')

echo 'Deployment Outputs:'
echo "$deployOutput"

# create a service principal
if [ -n "$REPO" ]; then
    echo -e "${info_color}Creating service principal ${yellow_color}$SP_NAME"

    # TODO: should we just use az ad app create?
    created_sp=$(az ad sp create-for-rbac --name "$SP_NAME")

    appid=$(echo "$created_sp" | jq -r .appId)

    # We don't need credentials, we will just use OIDC. So let's delete them
    echo -e "${info_color}  Deleting credentials for ${yellow_color}$appid${info_color}"
    az ad app credential list --id "$appid" --query [].keyId --output tsv | while read -r credentialid ; do
        echo "    deleting credential with key $credentialid"
        az ad app credential delete --id "$appid" --key-id "$credentialid"
    done

    # add OIDC federated credential to SP
    echo -e "${info_color}Adding OIDC federated credential to service principal ${yellow_color}$SP_NAME"

    echo -e "${info_color}Creating federated credential for ${yellow_color}$appid"
    echo -e "${info_color}  Creating Federated Credential for repo $REPO default branch"
    defaultBranchName=$(gh api "repos/$REPO" | jq -r .default_branch)
    federatedcredentialname="$(echo -n "$REPO" | tr / -)-$defaultBranchName"
    createFederatedCredential "$appid" "$federatedcredentialname" "repo:$REPO:ref:refs/heads/$defaultBranchName" "repo federation for default branch" "$defaultBranchName branch"
    echo "  Creating Federated Credential for prod environment"
    environment="Prod-$APP_NAME"
    federatedcredentialname="$(echo -n "$REPO" | tr / -)-$environment"
    createFederatedCredential "$appid" "$federatedcredentialname" "repo:$REPO:environment:$environment" "repo federation for prod environment" "$environment environment"

    appFunctionId=$(echo "$deployOutput" | jq -r '.functionAppId.value')
    echo -e "\n${info_color}Setting RBAC permissions for the service principal."

    # RG Reader needed to be able to get the function app id for the deploy workaround being used in the deploy wf
    assign_role "$resourceGroupId" "$appid" "Reader"
    assign_role "$appFunctionId" "$appid" "Contributor"

    subscription=$(az account show)
    tenantId=$(echo "$subscription" | jq -r .tenantId)
    subscriptionId=$(echo "$subscription" | jq -r .id)

    echo -e "\n${info_color}Adding variables client-id,tenant-id and subscription-id to repo ${yellow_color}$REPO"

    if [ -n "$VARPREFIX" ]; then
      VARPREFIX="${VARPREFIX}_"
    fi

    createOrUpdateRepoVariable "${VARPREFIX}CLIENT_ID" "$appid" "$REPO"
    createOrUpdateRepoVariable "${VARPREFIX}APP_NAME" "$APP_NAME" "$REPO"
    setRepoSecret "${VARPREFIX}TENANT_ID" "$tenantId" "$REPO"
    setRepoSecret "${VARPREFIX}SUBSCRIPTION_ID" "$subscriptionId" "$REPO"    
fi

echo -e "\n${green_color}Done!"
