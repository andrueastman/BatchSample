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
            const string clientId = "d662ac70-7482-45af-9dc3-c3cde8eeede4";
            string[] scopes = new string[] { "User.Read", "Calendars.Read"};

            IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder
                .Create(clientId).WithRedirectUri("http://localhost:1234")
                .Build();

            InteractiveAuthenticationProvider authenticationProvider = new InteractiveAuthenticationProvider(publicClientApplication, scopes);

            /* Get the client */
            GraphServiceClient graphClient = new GraphServiceClient(authenticationProvider);
            var query = new HttpQuery(graphClient);

            await Run0(graphClient);
            await Run1(query, graphClient);
            await Run2(query, graphClient);
            await Run3(graphClient);

            await Run0(graphClient);
            await Run1(query, graphClient);
            await Run2(query, graphClient);
            await Run3(graphClient);

            await Run0(graphClient);
            await Run1(query, graphClient);
            await Run2(query, graphClient);
            await Run3(graphClient);

        }

        /// <summary>
        /// Run the request in the normal fashion.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        public static async Task Run0(GraphServiceClient graphClient)
        {
            /* Request version 0 */
            /* Uses the normal way type */
            var watch = System.Diagnostics.Stopwatch.StartNew();
            User user = await graphClient.Me.Request().GetAsync();
            Calendar calendar = await graphClient.Me.Calendar.Request().GetAsync();
            Drive drive = await graphClient.Me.Drive.Request().GetAsync();

            Console.WriteLine("Version 0");
            Console.WriteLine("Display Name user: " + user.DisplayName);
            Console.WriteLine("Calendar Owner Address: " + calendar.Owner.Address);
            Console.WriteLine("Display Drive Type: " + drive.DriveType);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

        /// <summary>
        /// Use the HttpQuery class to add requests and then execute them.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        public static async Task Run1(HttpQuery query, GraphServiceClient graphClient)
        {
            /* Request version 1 */
            /* Uses a callback */
            var firstModel = new ViewModel();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            query.AddRequest<User>(graphClient.Me.Request(), u => firstModel.Me = u);
            query.AddRequest<Calendar>(graphClient.Me.Calendar.Request(), cal => firstModel.Calendar = cal);
            query.AddRequest<Drive>(graphClient.Me.Drive.Request(), dr => firstModel.Drive = dr);

            await query.ExecuteAsync();//run them at the same time :)
            Console.WriteLine("Version 1");
            Console.WriteLine("Display Name user: " + firstModel.Me.DisplayName);
            Console.WriteLine("Display Owner Address: " + firstModel.Calendar.Owner.Address);
            Console.WriteLine("Display Drive Type: " + firstModel.Drive.DriveType);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

        /// <summary>
        /// Use the HttpQuery Class to populate a dynamic type to use.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        public static async Task Run2(HttpQuery query, GraphServiceClient graphClient)
        {
            /* Request version 2 */
            /* Uses the dynamic type */
            var watch = System.Diagnostics.Stopwatch.StartNew();
            dynamic result = await query.PopulateAsync(new
            {
                Me = graphClient.Me.Request(),
                Calendar = graphClient.Me.Calendar.Request(),
                Drive = graphClient.Me.Drive.Request()
            });

            Console.WriteLine("Version 2");
            Console.WriteLine("Display Name user: " + result.Me.displayName);
            Console.WriteLine("Calendar Owner Address: " + result.Calendar.owner.address);
            Console.WriteLine("Display Drive Type: " + result.Drive.driveType);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

        /// <summary>
        /// Use a response handler to launch a fire and forget fashioned call.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        public static async Task Run3(GraphServiceClient graphClient)
        {
            /* Request version 3 */
            /* Uses the dynamic type */
            var secondModel = new ViewModel();
            var responseHandler = new ResponseHandler();
            responseHandler.OnSuccess<User>(u => secondModel.Me = u);
            responseHandler.OnSuccess<Calendar>(cal => secondModel.Calendar = cal);
            responseHandler.OnSuccess<Drive>(dr => secondModel.Drive = dr);
            responseHandler.OnClientError(e => Console.WriteLine(e.Message));
            responseHandler.OnServerError(e => Console.WriteLine(e.Message));

            var watch = System.Diagnostics.Stopwatch.StartNew();
            await graphClient.Me.Request().GetAsync(responseHandler);
            await graphClient.Me.Calendar.Request().GetAsync(responseHandler);
            await graphClient.Me.Drive.Request().GetAsync(responseHandler);

            Console.WriteLine("Version 3");
            Console.WriteLine("Display Name user: " + secondModel.Me.DisplayName);
            Console.WriteLine("Calendar Owner Address: " + secondModel.Calendar.Owner.Address);
            Console.WriteLine("Display Drive Type: " + secondModel.Drive.DriveType);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

    }
}
