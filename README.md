This is an Architecture Reference project for Azure Function.

Add Microsoft.Extensions.Configuration.Abstractions NuGet package to use configurations

To run it locally
Open Visual Studio as Admin
Start Microsoft Azure Storage Emulator as Admin
	Open cmd as Admin
	cd "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator"
	AzureStorageEmulator.exe start

First local run
    		Open cmd as Admin
			run - "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" init /forceCreate
			If that errors out with "Error: Cannot create database 'AzureStorageEmulatorDb<some number>'", open Windows Explorer, navigate to %USERPROFILE%, delete any 'AzureStorageEmulatorDb' files that may be there, try again
			(https://stackoverflow.com/questions/53673763/azure-storage-emulator-fails-to-init-with-the-database-azurestorageemulatordb5)

Configuring TimerTrigger
TimerTrigger takes a six field CRON expression
https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#cron-expressions
{second} {minute} {hour} {day} {month} {day-of-week}
Examples
"0 15 0 * * *"	at 12:15 AM every day
"0 */5 * * * *" once every 5 minutes
"*/10 * * * * *" once every 10 seconds

In Azure
Create new Function App
<env>-<app>-function-name
Hosting Plan -- Consumption Plan
Runtime Stack - .NET Core
Storage - use <app>azurefunctions<env>

Download Publish Profile
Right-click on the project, click Publish
Select Existing
Import Publish Profile
Try running the Publish
Navigate to the portal to make sure the function has deployed successfully

function.json schema
http://json.schemastore.org/function