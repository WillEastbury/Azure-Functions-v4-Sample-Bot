REM This cli deployment script is not complete
REM But this should be enough for you to work out how you should deploy the instances and what needs to be present

REM Usage
REM Deploy.bat resourcegroupname location botappname botpassword azuresubscriptionid

set ResGrpName=%1
set ResLocation=%2
set botappname=%3
set botpswd=%4
set azrsub=%5

az login 
az account set --subscription "%azrsub%"

az group create --name %ResGrpName% --location %ResLocation%
az storage account create --resource-group %ResGrpName% --name %botappname% --https-only -l %ResLocation% --kind StorageV2 --sku Standard_LRS
az ad app create --display-name %botappname% --password %botpswd% --available-to-other-tenants
az cognitiveservices account create -n %botappname%  -g %ResGrpName% --kind SpeechServices --sku F0 -l %ResLocation% --yes
az functionapp create --resource-group %ResGrpName% --name %botappname% --storage-account %botappname% --os-type Windows --runtime dotnet --consumption-plan-location %ResLocation%
az bot create --resource-group %ResGrpName% --appid "d0e6a143-ee9f-4426-a83d-ea82a89c3200" --kind registration --name %botappname% --password %botpswd% --location %ResLocation% -sku F0 -version V4 --endpoint

az cosmosdb create --resource-group %ResGrpName% --name %botappname%
az cosmosdb database create --resource-group %ResGrpName% --name %botappname% --db-name %botappname% 

az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings GameFile="/GameData.json"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings DirectLineSecret="sample"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings MicrosoftAppId="value"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings MicrosoftAppPassword="value"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings CosmosDBEndpoint="https://%ResLocation%.documents.azure.com:443/"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings CosmosAuthKey=value
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings CosmosCollectionId="botstate"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings CosmosDatabaseId="botstate"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings DirectLineConversationEndpoint="https://directline.botframework.com/v3/directline/conversations"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings CognitiveSecret="sample"
az webapp config appsettings set -g %ResGrpName% -n %botappname% --settings CognitiveTokenEndpoint="https://%ResLocation%.api.cognitive.microsoft.com/sts/v1.0/issueToken"
