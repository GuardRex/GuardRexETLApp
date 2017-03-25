using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Octokit;
using System.IO;
using System.Text.RegularExpressions;

namespace GuardRexETLApp
{
    class Program
    {
        const string gitHubUserLogin = @"<GITHUB_USER_LOGIN>";
        static string gitHubToken = "<GITHUB_PERSONAL_TOKEN>";
        const string saveFilepath = @"C:\Users\<USER>\Desktop";

        static void Main(string[] args)
        {
            Console.WriteLine("Start Date (m/d/yyyy) or leave blank to use last seven days ...");
            var startDateStr = Console.ReadLine();
            Console.WriteLine("End Date (m/d/yyyy) or leave blank to use today ...");
            var endDateStr = Console.ReadLine();
            Regex rgx = new Regex(@"^(?![\s\S])|(([1-9]|0[1-9]|1[012])[\/]([1-9]|0[1-9]|[12][0-9]|3[01])[\/](19|20)\d\d)$");

            if (rgx.IsMatch(startDateStr) && rgx.IsMatch(endDateStr))
            {
                AsyncContext.Run(() => MainAsync(args, startDateStr, endDateStr));
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Dates provided are not in the correct format. Program terminating!");
                Console.WriteLine();
                Console.WriteLine("Press any key to close ...");
                Console.ReadLine();
            }
        }

        static async void MainAsync(string[] args, string startDateStr, string endDateStr)
        {
            await Test(saveFilepath, startDateStr, endDateStr);
        }

        private static async Task Test(string saveFilepath, string startDateStr, string endDateStr)
        {
            try
            {
                var tokenAuth = new Credentials(gitHubToken);
                
                var client = new GitHubClient(new ProductHeaderValue("GuardRex-Pull-Request-ETL-Application"));
                client.Credentials = tokenAuth;

                var startDate = DateTime.Today.AddDays(-7);
                if (startDateStr.Length != 0)
                {
                    var tempDate = Convert.ToDateTime(startDateStr);
                    startDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, 0, 0, 0);
                }

                var endDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 23, 59, 59);
                if (endDateStr.Length != 0)
                {
                    var tempDate = Convert.ToDateTime(endDateStr);
                    endDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, 23, 59, 59);
                }

                saveFilepath = Path.Combine(saveFilepath, $"Data_{startDate.Month}-{startDate.Day}-{startDate.Year}_{endDate.Month}-{endDate.Day}-{endDate.Year}.txt");

                RepositoryCollection repos = new RepositoryCollection {
                    "dotnet/docs",
                    "aspnet/Docs"
                };

                var searchIssuesRequest = new SearchIssuesRequest
                {
                    Repos = repos,
                    Type = IssueTypeQualifier.PullRequest,
                    State = ItemState.Closed,
                    Assignee = gitHubUserLogin,
                    Closed = new DateRange(startDate, endDate)
                };

                var searchResults = await client.Search.SearchIssues(searchIssuesRequest);

                using (StreamWriter writer = File.CreateText(saveFilepath))
                {
                    writer.WriteLine("Pull Requests");
                    writer.WriteLine("-------------");

                    if (searchResults.TotalCount > 0)
                    {
                        foreach (var pr in searchResults.Items)
                        {
                            writer.WriteLine($"{pr.Title} - {pr.Url}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("None");
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Start Date: {startDate} End Date: {endDate} PRs written: {searchResults.Items.Count}");
                /*
                Console.WriteLine();
                var apiInfo = client.GetLastApiInfo();
                var rateLimit = apiInfo?.RateLimit;
                var howManyRequestsCanIMakePerHour = rateLimit?.Limit;
                var howManyRequestsDoIHaveLeft = rateLimit?.Remaining;
                var whenDoesTheLimitReset = rateLimit?.Reset;
                Console.WriteLine($"Requests allowed per hour: {howManyRequestsCanIMakePerHour}");
                Console.WriteLine($"Requests Remaining: {howManyRequestsDoIHaveLeft}");
                Console.WriteLine($"Time limit resets in: {whenDoesTheLimitReset}");
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to close ...");
            Console.ReadLine();
        }
    }
}
