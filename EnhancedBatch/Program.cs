using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace EnhancedBatch
{
    class Program
    {
        public static async Task Main()
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
            var firstModel = new ViewModel();
            query.AddRequest<User>(graphClient.Me.Request(), u => firstModel.Me = u);
            query.AddRequest<Calendar>(graphClient.Me.Calendar.Request(), cal => firstModel.Calendar = cal);

            query.ExecuteAsync();//run them at the same time :)

            Console.WriteLine("Version 1");
            Console.WriteLine("Display Name user: " + firstModel.Me.DisplayName);
            Console.WriteLine("Display Owner Address: " + firstModel.Calendar.Owner.Address);
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

            /* Request version 3 */
            /* Uses the dynamic type */
            var secondModel = new ViewModel();
            var responseHandler = new ResponseHandler();
            responseHandler.OnSuccess<User>(u => secondModel.Me = u);
            responseHandler.OnSuccess<Calendar>(cal => secondModel.Calendar = cal);
            responseHandler.OnClientError(e => Console.WriteLine(e.Message));
            responseHandler.OnServerError(e => Console.WriteLine(e.Message));

            await graphClient.Me.Request().GetAsync(responseHandler);
            await graphClient.Me.Calendar.Request().GetAsync(responseHandler);

            Console.WriteLine("Version 3");
            Console.WriteLine("Display Name user: " + secondModel.Me.DisplayName);
            Console.WriteLine("Calendar Owner Address: " + secondModel.Calendar.Owner.Address);
            Console.WriteLine("\r\n\r\n");
        }
    }
}
