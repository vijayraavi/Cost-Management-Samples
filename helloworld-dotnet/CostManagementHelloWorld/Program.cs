using System;
using Microsoft.Azure.Management.Consumption;
using Microsoft.Azure.Management.Consumption.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System.Configuration;
using ARMClient.Authentication.AADAuthentication;
using ARMClient.Authentication.Utilities;
using ARMClient.Authentication.Contracts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CostManagementHelloWorld
{
    class Program
    {
        //Initialize all relevant objects
        static List<UsageDetail> usageDetails = new List<UsageDetail>();
        static Dictionary<string, List<UsageDetail>> usageDetailsByInstance = new Dictionary<string, List<UsageDetail>>();
        static List<PriceSheetProperties> priceSheet = new List<PriceSheetProperties>();
        static decimal? sumCost = new decimal?();

        static void Main(string[] args)
        {
            //Login with ARM using ARMClient.Authorization dll
            TokenCredentials creds = GetTokenCredentialsWithARMClient();

            // Create the consumption client and obtain data for the configured subscription
            using (ConsumptionManagementClient consumptionClient = new ConsumptionManagementClient(creds))
            {
                consumptionClient.SubscriptionId = ConfigurationManager.AppSettings["SubscriptionId"];

                //Get usage details and perform some basic processing
                ProcessUsageDetails(consumptionClient);

                //Sum the cost of one of your resources based on usage detail cost info
                SumTotalCostOfAResource();

                //Get the full price sheet
                GetFullPriceSheet(consumptionClient);

                //Create a new sample budget
                CreateTestBudget(consumptionClient);
            }

            Console.WriteLine("Hello World! We hope you enjoy using the Azure Consumption APIs.");
            Console.ReadKey();
        }

        /// <summary>
        /// A method that queries ARM to obtain a user bearer token to use with the Consumption client.
        /// </summary>
        /// <returns>The token credentials for the user</returns>
        static TokenCredentials GetTokenCredentialsWithARMClient()
        {
            //Login with ARM using ARMClient.Authorization dll
            var persistentAuthHelper = new PersistentAuthHelper();
            persistentAuthHelper.AzureEnvironments = AzureEnvironments.Prod;
            TokenCacheInfo cacheInfo = null;
            persistentAuthHelper.AzureEnvironments = Utils.GetDefaultEnv();

            //Acquire tokens
            persistentAuthHelper.AcquireTokens().Wait();
            cacheInfo = persistentAuthHelper.GetToken(ConfigurationManager.AppSettings["SubscriptionId"], null).Result;
            TokenCredentials creds = new TokenCredentials(cacheInfo.AccessToken, "Bearer");

            return creds;
        }

        /// <summary>
        /// Method that queries and processes usage details.
        /// </summary>
        /// <param name="consumptionClient">The Consumption client.</param>
        static void ProcessUsageDetails(ConsumptionManagementClient consumptionClient)
        {
            Console.WriteLine("Querying the Consumption API to get Usage Details.");
            Console.WriteLine("");

            //Grab the usage details for this month
            IPage<UsageDetail> usagePage = consumptionClient.UsageDetails.List("properties/meterDetails");
            ProcessUsagePage(usagePage);

            //Handle subsequent pages
            string nextPageLink = usagePage.NextPageLink;
            while (nextPageLink != null)
            {
                IPage<UsageDetail> nextUsagePage = consumptionClient.UsageDetails.ListNext(nextPageLink);
                ProcessUsagePage(nextUsagePage);
                nextPageLink = nextUsagePage.NextPageLink;
            }

            Console.WriteLine(usageDetails.Count + " usage detail records have been found! Placing them into a generic list" +
                " and a dictionary by instanceid");
            Console.WriteLine("");
        }

        /// <summary>
        /// Method that process usage pages and places them into a generic list and dictionary by instanceId
        /// </summary>
        /// <param name="usagePage">The usage page</param>
        static void ProcessUsagePage(IPage<UsageDetail> usagePage)
        {
            IEnumerator enumerator = usagePage.GetEnumerator();

            while (enumerator.MoveNext())
            {
                UsageDetail currentDetail = (UsageDetail)enumerator.Current;

                //Add the usage detail to the list
                usageDetails.Add(currentDetail);

                //Add the usage detail to the dictionary
                if (usageDetailsByInstance.ContainsKey(currentDetail.InstanceId))
                {
                    usageDetailsByInstance[currentDetail.InstanceId].Add(currentDetail);
                }
                else
                {
                    List<UsageDetail> usageDetailsListForResource = new List<UsageDetail>();
                    usageDetailsListForResource.Add(currentDetail);
                    usageDetailsByInstance.Add(currentDetail.InstanceId, usageDetailsListForResource);
                }
            }
        }

        /// <summary>
        /// Method that summarizes the cost of every usage detail record that pertains to one resource
        /// </summary>
        static void SumTotalCostOfAResource()
        {
            //Calculate the cost for the first usage detail resource in the general list
            sumCost = 0;
            foreach (UsageDetail info in usageDetailsByInstance[usageDetails.First().InstanceId])
            {
                sumCost += info.PretaxCost;
            }

            Console.WriteLine("The total cost of resource " + usageDetails.First().InstanceId +
                " based on cost in the usage detail record is: "
                + sumCost);
            Console.WriteLine("");
        }

        /// <summary>
        /// Method that creates a test budget
        /// </summary>
        /// <param name="consumptionClient"></param>
        static void CreateTestBudget(ConsumptionManagementClient consumptionClient)
        {
            Console.WriteLine("Querying the Consumption API to create a sample budget for you!");
            Console.WriteLine("");

            //Create a new budget object with the calculated cost above
            Budget newBudget = new Budget
            {
                Amount = (decimal)sumCost,
                Category = "Cost",
                Filters = new Filters
                {
                    Resources = new List<string>
                    {
                        usageDetails.First().InstanceId
                    }
                },
                TimeGrain = "Monthly",
                TimePeriod = new BudgetTimePeriod
                {
                    StartDate = DateTime.UtcNow.AddDays(-DateTime.UtcNow.Day + 1),
                    EndDate = DateTime.UtcNow.AddMonths(2)
                }

                /* Notifications for this budget can also be configured, uncomment the code below to add one
                ,Notifications = new Dictionary<string, Notification>()
                {
                    {
                        "NotificationName", new Notification
                        {
                            Enabled = true,
                            Threshold = (decimal)0.90,
                            ContactEmails = new List<string> { "YOUREMAIL@EMAIL.com"},
                            ContactGroups = null,
                            ContactRoles = null,
                            OperatorProperty = "GreaterThan"
                        }
                    }
                }
                */
            };

            Console.WriteLine("What would you like your budget to be named? Please type your response below: ");
            string budgetName = Console.ReadLine();

            //Create the budget
            consumptionClient.Budgets.CreateOrUpdate(
                budgetName,
                newBudget);

            Console.WriteLine("");
            Console.WriteLine("New budget created named " + budgetName + "!");
            Console.WriteLine("");
        }

        /// <summary>
        /// Method that queries to obtain the full price sheet.
        /// </summary>
        /// <param name="consumptionClient"></param>
        static void GetFullPriceSheet(ConsumptionManagementClient consumptionClient)
        {
            Console.WriteLine("Querying the Consumption API to get the Price Sheet for you!");
            Console.WriteLine("");

            //Get price first price sheet result and put the properties into a list
            PriceSheetResult priceSheetResult = consumptionClient.PriceSheet.Get();
            foreach (PriceSheetProperties properties in priceSheetResult.Pricesheets)
            {
                priceSheet.Add(properties);
            }

            //Process subsequest price sheet results
            while (priceSheetResult.NextLink != "")
            {
                Uri nextPriceSheetLink = new Uri(priceSheetResult.NextLink);
                var query = HttpUtility.ParseQueryString(nextPriceSheetLink.Query);
                string skipToken = query.Get("$skiptoken");

                priceSheetResult = consumptionClient.PriceSheet.Get(null, skipToken);
                foreach (PriceSheetProperties properties in priceSheetResult.Pricesheets)
                {
                    priceSheet.Add(properties);
                }
            }

            Console.WriteLine("Obtained the price sheet! There are currently " + priceSheet.Count + " different meter rates!");
            Console.WriteLine("");
        }
    }
}
