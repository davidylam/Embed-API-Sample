using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmbedAPISample
{
    class Program
    {
        private static bool useEmbedToken = true;
        private static bool useRLS = true;

        private static string authorityUrl = "https://login.microsoftonline.com/organizations/";
        private static string resourceUrl = "https://analysis.windows.net/powerbi/api";
        private static string apiUrl = "https://api.powerbi.com/";

        private static string tenantId = "c25fc350-5f5e-400c-9a8a-089b28b79eae";
        private static Guid groupId = Guid.Parse("7ce79c4c-3d1c-4ece-84c3-9ed5d469f694");

        //rls
        private static Guid reportId = Guid.Parse("95c491aa-b543-40f1-acee-a07ced16754a");
        private static Guid datasetId = Guid.Parse("69c42b79-ed2b-4b1a-af2d-a45754263699");

        //Team
        //private static Guid reportId = Guid.Parse("7147637d-d4f6-4df0-b5a0-ee07eb81e257");
        //private static Guid datasetId = Guid.Parse("4e4f90c3-8732-42b5-b49e-27359cf0eb21");

        /*
         * If in Power BI it has Manage Security Role:
         * set Line 14 to true
         * username must be in the table
         * Not sure if username column must exist? to match in .Net with dataset
         * 
         * *** DATASETS ***
                ed1f596d-d222-49b3-a636-30e1d8672aa8 | Table
                4528dd07-06e9-4ac6-b62a-614b3e3c7d3b | Dept
                5ea6b3f3-bb3e-4552-a3e2-8e2327179e00 | TestSecurity
                4e4f90c3-8732-42b5-b49e-27359cf0eb21 | NoUserNameInVisualTestSecurity
                05377481-d684-4d4e-9e61-0c99b241d74d | TeamSports
                90e767a6-5f06-4fac-b617-a1638d8dfb5f | TeamSports2
                9e28ef8e-a20e-4fd1-b9bb-19b031995046 | rls
                *** REPORTS ***
                fca7e42f-53c6-4b46-a9f7-ab515a8cf06f | TestPage | DatasetID = ed1f596d-d222-49b3-a636-30e1d8672aa8
                149da1c1-f333-42b1-8d83-294686fd072f | Dept | DatasetID = 4528dd07-06e9-4ac6-b62a-614b3e3c7d3b
                558226e6-9b02-4766-8275-dc177022ea8b | TestSecurity | DatasetID = 5ea6b3f3-bb3e-4552-a3e2-8e2327179e00
                17d0e728-6608-4e50-9ed2-25968fefa30c | NoUserNameInVisualTestSecurity | DatasetID = 4e4f90c3-8732-42b5-b49e-27359cf0eb21
                2930829a-6e45-492a-9415-a6ca8d8862bb | TeamSports | DatasetID = 05377481-d684-4d4e-9e61-0c99b241d74d
                7147637d-d4f6-4df0-b5a0-ee07eb81e257 | TeamSports2 | DatasetID = 90e767a6-5f06-4fac-b617-a1638d8dfb5f
                966559ea-5d59-4b20-a3cc-25253f032f99 | rls | DatasetID = 9e28ef8e-a20e-4fd1-b9bb-19b031995046
                *** DASHBOARDS ***
                74ba3ce8-6361-4a88-9f96-2b3bea7aef99 | TestSecurity.pbix
                2587fd0f-f2ab-4513-bf84-b40bf1336c80 | NoUserNameInVisualTestSecurity.pbix
                f2b29248-8c3a-4ba2-8e84-5b4e0d43b984 | TeamSports.pbix
                efb886a0-445e-4424-a439-801fc160cc8c | TeamSports2.pbix
                db29aeaf-d512-4ad6-a425-3453c1529062 | rls.pbix
         * 
         * */

        // **** Update the Client ID and Secret within Secrets.cs ****

        private static ClientCredential credential = null;
        private static AuthenticationResult authenticationResult = null;
        private static TokenCredentials tokenCredentials = null;

        static void Main(string[] args)
        {

            try
            {
                // Create a user password cradentials.
                credential = new ClientCredential(Secrets.ClientID, Secrets.ClientSecret);

                // Authenticate using created credentials
                Authorize().Wait();

                using (var client = new PowerBIClient(new Uri(apiUrl), tokenCredentials))
                {

                    #region Embed Token
                    EmbedToken embedToken = null;


                    if (useEmbedToken && !useRLS)
                    {
                        // **** Without RLS ****
                        embedToken = client.Reports.GenerateTokenInGroup(groupId, reportId, 
                            new GenerateTokenRequest(accessLevel: "View", datasetId: datasetId.ToString()));
                    }
                    else if(useEmbedToken && useRLS)
                    {
                        // **** With RLS ****

                        // Documentation: https://docs.microsoft.com/power-bi/developer/embedded/embedded-row-level-security
                        // Example: 
                        //
                        // Define Embed Token request:
                        //var generateTokenRequestParameters = new GenerateTokenRequest("View", null, 
                        //    identities: new List<EffectiveIdentity> { new EffectiveIdentity(username: "username", 
                        //        roles: new List<string> { "roleA", "roleB" }, 
                        //        datasets: new List<string> { "datasetId" }) });
                        // 
                        // Generate Embed Token:
                        //var tokenResponse = await client.Reports.GenerateTokenInGroupAsync("groupId", "reportId", 
                        //    generateTokenRequestParameters);

                        var rls = new EffectiveIdentity(username: "amy1234@majesco.com", null, new List<string> { datasetId.ToString() });

                        var rolesList = new List<string>();
                        rolesList.Add("user");
                        rls.Roles = rolesList;

                        embedToken = client.Reports.GenerateTokenInGroup(groupId, reportId, 
                            new GenerateTokenRequest(accessLevel: "View", datasetId: datasetId.ToString(), rls));
                    }
                    #endregion

                    #region Output Embed Token

                    if (useEmbedToken)
                    {
                        // Get a single report used for embedding
                        Report report = client.Reports.GetReportInGroup(groupId, reportId);

                        Console.WriteLine("\r*** EMBED TOKEN ***\r");

                        Console.Write("Report Id: ");

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(reportId);
                        Console.ResetColor();

                        Console.Write("Report Embed Url: ");

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(report.EmbedUrl);
                        Console.ResetColor();

                        Console.WriteLine("Embed Token Expiration: ");

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(embedToken.Expiration.ToString());
                        Console.ResetColor();


                        Console.WriteLine("Report Embed Token: ");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(embedToken.Token);
                        Console.ResetColor();
                    }
                    #endregion

                    #region Output Datasets
                    Console.WriteLine("\r*** DATASETS ***\r");

                    try
                    {
                        // List of Datasets
                        // This method calls for items in a Group/App Workspace. To get a list of items within your "My Workspace"
                        // call GetDatasets()
                        var datasetList = client.Datasets.GetDatasetsInGroup(groupId);

                        foreach (Dataset ds in datasetList.Value)
                        {
                            Console.WriteLine(ds.Id + " | " + ds.Name);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error fetching datasets: " + ex.Message);
                        Console.ResetColor();
                    }
                    #endregion

                    #region Output Reports
                    Console.WriteLine("\r*** REPORTS ***\r");

                    try
                    {
                        // List of reports
                        // This method calls for items in a Group/App Workspace. To get a list of items within your "My Workspace"
                        // call GetReports()
                        var reportList = client.Reports.GetReportsInGroup(groupId);

                        foreach (Report rpt in reportList.Value)
                        {
                            Console.WriteLine(rpt.Id + " | " + rpt.Name + " | DatasetID = " + rpt.DatasetId);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error fetching reports: " + ex.Message);
                        Console.ResetColor();
                    }
                    #endregion

                    #region Output Dashboards
                    Console.WriteLine("\r*** DASHBOARDS ***\r");

                    try
                    {
                        // List of reports
                        // This method calls for items in a Group/App Workspace. To get a list of items within your "My Workspace"
                        // call GetReports()
                        var dashboards = client.Dashboards.GetDashboardsInGroup(groupId);

                        foreach (Dashboard db in dashboards.Value)
                        {
                            Console.WriteLine(db.Id + " | " + db.DisplayName);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error fetching dashboards: " + ex.Message);
                        Console.ResetColor();
                    }
                    #endregion

                    #region Output Gateways
                    Console.WriteLine("\r*** Gateways ***\r");

                    try
                    {
                        var gateways = client.Gateways.GetGateways();

                        Console.WriteLine(gateways.Value[0].Name);

                        //foreach (Gateway g in gateways)
                        //{
                        //    Console.WriteLine(g.Name + " | " + g.GatewayStatus);
                        //}
                    }
                    catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error fetching gateways: " + ex.Message);
                        Console.ResetColor();
                    }
                    #endregion
                }

            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }

        }

        private static Task Authorize()
        {
            return Task.Run(async () => {
                authenticationResult = null;
                tokenCredentials = null;

                // TENANT ID is required when using a Service Principal
                var tenantSpecificURL = authorityUrl.Replace("organizations", tenantId);

                var authenticationContext = new AuthenticationContext(tenantSpecificURL);

                authenticationResult = await authenticationContext.AcquireTokenAsync(resourceUrl, credential);

                if (authenticationResult != null)
                {
                    tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");
                }
            });
        }






    }
}
