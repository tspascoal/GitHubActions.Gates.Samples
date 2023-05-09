#!/usr/bin/env bash

set -o pipefail

if [ $# -ne 2 ]; then
    echo "Usage: $(dirname "$0") <resource group> <function name>"
    exit 1
fi

if ! az account show --query id > /dev/null
    then
        echo "You need to login to Azure CLI (see https://aka.ms/azcli)"
        exit
fi

if az --version | grep -i windows > /dev/null
    then
        echo "Warning running this script in WSL with Azure CLI installed in Windows may not work."
fi

resourcegroup=$1
functionname=$2

RULENAME="ghhook" # keep this in sync with bicep file
hooksIps=$(gh api meta --jq .hooks[])

existingIps=$(az functionapp config access-restriction show --name "$functionname" --resource-group "$resourcegroup" --query 'ipSecurityRestrictions')

echo "GH WebHooks IPs: "
echo -e "$hooksIps"

echo -e "\nAdding GH Web Hook IP restrictions to $functionname"

echo "$hooksIps" | while read -t 3600 -r hookIP; do

    echo "  Adding IP restrictions for $hookIP"

    if [ "$(echo "$existingIps" | jq -r --arg ip "$hookIP" '.[] | select(.ip_address == $ip) | .ip_address')" != "" ]; then
        echo "    Already exists. Skipping."
        continue
    fi

    echo "    Adding"

    az functionapp config access-restriction add \
        --priority "900" \
        --action allow \
        --ip-address "$hookIP" \
        --description "Allow request from GitHub.com webhooks" \
        --name "$functionname" \
        --resource-group "$resourcegroup" \
        --rule-name "$RULENAME" \
        --only-show-errors > /dev/null || true    
done 
# <<< "$hooksIps"

echo -e "\nRemoving Older GH Web Hook IP restrictions from $functionname"

while read -r hookIP; do

    if [ "Any" == "$hookIP" ] || [ "$hookIP" = "" ]; then
        continue
    fi

    echo " Checking IP restrictions for $hookIP"

    if [[ $hooksIps == *"$hookIP"* ]]; then
        echo "    IP $hookIP is still a valid GH hook IP. Skipping."
        continue
    fi

    echo "    Removing IP $hookIP"
    az functionapp config access-restriction remove \
        --action allow \
        --ip-address "$hookIP" \
        --name "$functionname" \
        --resource-group "$resourcegroup" \
        --rule-name "$RULENAME" \
        --only-show-errors > /dev/null

done <<< "$(echo "$existingIps" | jq -r --arg nameprefix "$RULENAME" '.[] | select(.name != null) | select(.action="Allow") | select(.name | startswith($nameprefix)) | .ip_address')"

echo -e "\nadding deny all rule"

az resource update \
  --resource-group "$resourcegroup" \
  --name "$functionname" \
  --resource-type "Microsoft.Web/sites" \
  --set properties.siteConfig.ipSecurityRestrictionsDefaultAction=Deny > /dev/null

echo -e "\nDone"

