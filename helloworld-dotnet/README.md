# CostManagementHelloWorld

A sample console application that calls the Azure Consumption REST APIs to retrieve and perform simple processing for users who wish to begin developing on top of the APIs.  The Microsoft Azure Consumption APIs give you programmatic access to cost and usage data for your Azure resources. These APIs currently only support Enterprise Enrollments and Web Direct Subscriptions (with a few exceptions). The APIs are continually updated to support other types of Azure subscriptions. To learn more about the Consumption APIs and Azure Cost Management, visit the overview article [Azure Consumption API Overview](https://docs.microsoft.com/en-us/rest/api/consumption/).

**NOTE:** This sample app will only work for customers with EA subscriptions.

## Features

This project framework provides the following features:

* Code to obtain your usage details for the month
* Code to obtain your price sheet
* Code to calculate the cost of a single resource
* Code to create your first budget

## Getting Started

### Prerequisites

To run this sample you will need:
  * Visual Studio 2013 or higher
  * An Internet connection
  * An Azure subscription (a free trial is sufficient)
  * One or more Azure resources created within your subscription

You will also need to be comfortable with the following tasks:
  * Using the Azure portal (or working with your administrator) to create resources and/or determine your subscription id
  * Using Git and Github to bring the sample code down to your local machine
  * Using Visual Studio to edit configuration files, build, and run the sample

Every Azure subscription has an associated Azure Active Directory (AAD) tenant. If you don't already have an Azure subscription, you can get a free subscription by signing up at https://azure.microsoft.com.

### Installation

Clone or download the Cost Management Samples repository.
From your shell (ie: Git Bash, etc.) or command line, run the following command :

    git clone https://github.com/Azure-Samples/Cost-Management-Samples.git

### Quickstart

Step 1: Determine your subscription id.
You will need to determine a subscription that you own that can be analyzed by the application. To do this, follow the steps below.
  1. Log in to the [Azure Portal](https://portal.azure.com) with an account that has Administrator rights to one or more subscriptions.
  2. Click on the "Subscriptions" button within the ribbon on the left hand side of the page. If the button is not present, click the "All Services" button and find the "Subscriptions" button in the tile that gets generated. 
  3. On the Subscriptions page you will see 1 or more subscriptions along with their SubscriptionId. Note the SubscriptionId value for a subscription that has resources created within.

Step 2: Edit and build the project in the 'helloworld-dotnet' folder in Visual Studio.
After you've downloaded the sample app, you will need to go into the local sub directory in which the Visual Studio solution is stored (typically in <your-git-root-directory>\Cost-Management-Samples\helloworld-dotnet), and open the CostManagementHelloWorld.sln Visual Studio solution. Upon opening, navigate to the app.config file and update the following key/value pair with your SubscriptionId.

    <add key="SubscriptionId" value="INSERT SUBSCRIPTION GUID HERE" />

Step 3: Run the application.
Once finished with step 2 you should be able to run the app and see the output. The app will gather all of your Usage Detail records for the current month, calculate the cost of a single resource in your subscription, get the full price sheet for your enterprise, and create a test budget for you.