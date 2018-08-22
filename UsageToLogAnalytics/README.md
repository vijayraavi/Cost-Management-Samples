[Moving Usage logs to Log Analytics]

Scenario: Customers would like to want their usage data to be moved to
Log Analytics.

Pre-requisites:

-   Visual Studio 2017 (Download from
    [here](https://visualstudio.microsoft.com/vs/whatsnew/).)

-   Azure Functions and WebJobs Tools (Download from
    [here](https://marketplace.visualstudio.com/items?itemName=VisualStudioWebandAzureTools.AzureFunctionsandWebJobsTools))

Step 1: Create a Log Analytics workspace in the subscription you want to
see the usage for

Go to the portal
<https://ms.portal.azure.com/#blade/HubsExtension/Resources/resourceType/Microsoft.OperationalInsights%2Fworkspaces>

![](media/image2.png)

Click Add button and fill the Workspace details.

![](media/image3.png)

2\. Enable SqlAuditLogs on the Sql Server Database on the subscription
you want. You can see that the newly created workspace.

![](media/image4.png)

![](media/image5.png)

Once the sql audit logs are being enabled to go into Log Analytics, lets
work on moving the usage data to Log Analytics.

4\. Get the code from Git Hub and build the application. There are 2
projects in that solution.

Function App, which is a time, triggered and the core dll. This function
app runs every 1 hour. You can change the frequency by changing the cron
expression. This if found in UCDDHourlyToOMS.cs file.

public static void Run(\[TimerTrigger(\"0 0 \*/1 \* \* \*\")\]TimerInfo
myTimer, TraceWriter log)

The Function App UCDDHourly2OMSFuncApp needs the following secrets to be
created in KeyVault.

1.  ucddhourlyconnstring:

    a.  The connection string of the storage account where the usage
        data was stored.

2.  Ucddhourlyblobcontainername: The blob container name

3.  omsworkspaceid

    i.  The oms workspaceId can be found in the advanced settings of the
        Log Analytics

4.  omsworkspacekey

    ii. This is the Primary key that can be found in the advanced
        settings of Log Analytics

5.  ![](media/image6.png)

6.  Get the Connection String of the storage account where the usage
    data is available.

7.  Open a new browser and go to the KeyVault where you would want to
    store and create the secrets.

8.  Once its created copy the Keyvault url and paste in the
    localsettings.json file in UCDDToOMSFun project.

9.  Now let's publish the Function App to Azure.

10. Open the project in VS2017 and find the UCDDToOMSFunc project.

![cid:image001.png\@01D432EF.47473AD0](media/image7.47473AD0)

11. Right click on the project and hit Publish. We first need to create
    a profile which has the subscription details, Resource Group,
    Storage Account and what APP Service Plan you want to use.

![cid:image002.png\@01D432EF.47473AD0](media/image8.47473AD0)

12. Click Create new profile

![](media/image9.png)

13. Select Create new Azure Function App

![cid:image004.png\@01D432EF.47473AD0](media/image10.47473AD0)

14. Give an appropriate name and select the subscription where it has to
    run.

15. ![cid:image005.png\@01D432EF.9BA102A0](media/image11.9BA102A0)

16. Select the right App Service Plan. If your data size compressed in
    around 10 MB (gzip compression), then selecting smaller SKUs is not
    enough for processing. So, pick the size depending on the size of
    usage data.

![cid:image006.png\@01D432F1.3A8B45A0](media/image12.3A8B45A0)

17. Hit Create

18. ![cid:image008.png\@01D432F1.3A8B45A0](media/image13.3A8B45A0)

19. It starts deploying the host services the Function App needs.

    ![cid:image009.png\@01D432F1.41D9FFE0](media/image14.41D9FFE0)

    Once deployment is completed you should be able to see the App
    Service and the FuncApp that got created. The function App is still
    empty, and no functions are deployed. You can search in the
    subscription for resource type "Function Apps"

    ![](media/image15.png)

    Now we need to go the App Settings in the portal for that Function
    App. Whatever settings we have in localsettings.json need to be
    copied to App Settings. Add "KeyVaultURL" setting.

    Next we need to enable Managed Service Identity to give Azure
    Identity to this Func App. Go to Platform Features and find

    ![](media/image16.png)

    ![cid:image016.png\@01D432F2.53CFF8C0](media/image17.53CFF8C0)

    Hit Save.

    Now we need to go to the Keyvault where we stored our secrets and
    give this newly created MSI of Func App access to secrets.

    Go to KeyVault -\> Access policies and click Add New

    ![cid:image017.png\@01D432F2.B2F03C70](media/image18.B2F03C70)

    And in Select Prinicpal search for Function App.

    ![cid:image018.png\@01D432F2.B2F03C70](media/image19.B2F03C70)

    And select Get and List in Secret permissions.

    ![cid:image019.png\@01D432F2.B2F03C70](media/image20.B2F03C70)

    And don't forget to save it.

    Now the Function App has access to Keyvault.

    Now let's enable App Insights for logging so that we can see the
    logs. Go to all Resources in azure portal home page and search for
    "Application Insights". Pick the correct the subscription and create
    new

    ![cid:image020.png\@01D432F3.12543FE0](media/image21.12543FE0)

    ![cid:image021.png\@01D432F3.12543FE0](media/image22.12543FE0)

    Once created click on the overview and get the instrumentation key
    and save it somewhere.

    ![](media/image23.png)

    Now we must provide this instrumentation key in the App Settings of
    the FuncApp.

Add this as setting name "APPINSIGHTS\_INSTRUMENTATIONKEY" and
instrumentation key which you copied earlier. And Save the App Settings.
After few seconds you should see Application Insights in Configured
features.

![cid:image024.png\@01D432F3.D40BECA0](media/image24.D40BECA0)

Now that the Host is setup and Keyvault access is granted lets deploy
the code from VS2017. Make sure you build the project in release mode
and right click on publish and select the profile which we created and
click Publish. You should see the status in Output window

![cid:image025.png\@01D432F4.4246C410](media/image25.4246C410)

Now you should see the function under Functions. If you want to see the
logs, go to the Monitor tab and see the runs.

After a successful run go to your log Analytics and under Custom logs
you should see "Usage_CL" table.
