using System;
using System.IO;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace EnhancedBatch
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Do the auth stuff first */
            string clientId = "d662ac70-7482-45af-9dc3-c3cde8eeede4";
            string[] scopes = new string[] { "User.Read", "Calendars.Read"};

            IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder
                .Create(clientId).WithRedirectUri("http://localhost:1234")
                .Build();

            InteractiveAuthenticationProvider authenticationProvider = new InteractiveAuthenticationProvider(publicClientApplication, scopes);

            /* Get the client */
            GraphServiceClient graphClient = new GraphServiceClient(authenticationProvider);

            var query = new HttpQuery(graphClient);

            /* Request version 1 */
            /* Uses a callback */
            User user = null;
            User user1 = null;
            query.AddRequest<User>(graphClient.Me.Request(), u => user = u);
            query.AddRequest<User>(graphClient.Me.Request(), u => user1 = u);

            query.ExecuteAsync();
            Console.WriteLine("Version 1");
            Console.WriteLine("Display Name user: " + user.DisplayName);
            Console.WriteLine("Display Name user1: " + user1.DisplayName);
            Console.WriteLine("\r\n\r\n");

            /* Request version 2 */
            /* Uses the dynamic type */
            dynamic result = query.PopulateAsync(new
            {
                Me = graphClient.Me.Request(),
                Calendar = graphClient.Me.Calendar.Request()
            });

            Console.WriteLine("Version 2");
            Console.WriteLine("Display Name user: " + result.Me.displayName);
            Console.WriteLine("Calendar Owner Address: " + result.Calendar.owner.address);
            Console.WriteLine("\r\n\r\n");
        }
    }
}
