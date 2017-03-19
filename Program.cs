using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Octokit;

namespace GuardRexETLApp
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
        {
            await Test();
        }

        private static async Task Test()
        {
            try
            {
                var tokenAuth = new Credentials("token");
                
                var client = new GitHubClient(new ProductHeaderValue("GuardRex-Pull-Request-ETL-Application"));
                client.Credentials = tokenAuth;

                var pullRequestRequest = new PullRequestRequest
                {
                    State = ItemStateFilter.Closed
                };

                var pullRequestsForDotnetDocs = await client.PullRequest.GetAllForRepository("dotnet", "docs", pullRequestRequest);

                var filteredPullRequests = pullRequestsForDotnetDocs.Where(t => t.Assignee?.Login == "GuardRex");

                foreach (var pr in filteredPullRequests)
                {
                    Console.WriteLine($"Title: {pr.Title}");
                    Console.WriteLine($"Url: {pr.Url}");
                    Console.WriteLine($"MergedAt: {pr.MergedAt} ClosedAt: {pr.ClosedAt}");
                    Console.WriteLine();
                }

                Console.WriteLine();
                var apiInfo = client.GetLastApiInfo();
                var rateLimit = apiInfo?.RateLimit;
                var howManyRequestsCanIMakePerHour = rateLimit?.Limit;
                var howManyRequestsDoIHaveLeft = rateLimit?.Remaining;
                var whenDoesTheLimitReset = rateLimit?.Reset;
                Console.WriteLine($"Requests allowed per hour: {howManyRequestsCanIMakePerHour}");
                Console.WriteLine($"Requests Remaining: {howManyRequestsDoIHaveLeft}");
                Console.WriteLine($"Time limit resets in: {whenDoesTheLimitReset}");
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
