## Create a SharePoint App
1. Create a new app by opening this url - https://{site-url}/_layouts/15/appregnew.aspx
2. Copy the ClientID and ClientSecret 
3. Create with Full Control permissions (XML below) and trust the App, by opening this page - https://{site-url}/_layouts/15/appinv.aspx
```XML
<AppPermissionRequests AllowAppOnlyPolicy="true">
    <AppPermissionRequest Scope="http://sharepoint/content/sitecollection" Right="FullControl" />
</AppPermissionRequests>
```

## Deploy the Azure Function
1. From VS Code deploy the az-func to an Azure Function App
2. After the deployment is completed, add the following App Settings (in the Configuration Section)
- ClientId : <Copied from step 1 above>
- ClientSecret : <Copied from step 1 above>
3. Copy the function URL and append the following URI

https://{function-url}/api/ProcessItemEvents

## Register the Event Receiver
1. Open PowerShell and load the latest version PnP.PowerShell module
2. Connect to the required site using the following cmdlet
```Powershell
Connect-PnPOnline -Url "https://{site-url}" -ClientId "{client-id}" -ClientSecret "{client-secret}"
```
3. Run the following cmdlet to register
```Powershell
#Example : Registring ItemAdding event
Add-PnPEventReceiver -List "Invoices" -Name "InvoicesItemAddingRER" -Url "https://{function-url}/api/ProcessItemEvents" -EventReceiverType ItemAdding -SequenceNumber 1000 -Synchronization Synchronous

#Example : Registring ItemAdded event
Add-PnPEventReceiver -List "Invoices" -Name "InvoicesItemAddedRER" -Url "https://{function-url}/api/ProcessItemEvents" -EventReceiverType ItemAdded -SequenceNumber 1000 -Synchronization Asynchronous
```