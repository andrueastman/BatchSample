using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace EnhancedBatch
{
    class Program
    {
        private static Stopwatch _publicWatch;

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
            /* Get a valid token in cache */
            await AcquireTokenToCache(graphClient);
            /* Create a HttpQuery for use */
            HttpQuery query = new HttpQuery(graphClient);

            /* Run the four versions */
            await Run0(graphClient);
            await Run1(query, graphClient);
            await Run2(query, graphClient);
            Run3(graphClient);

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
            _publicWatch = Stopwatch.StartNew();
            User user = await graphClient.Me.Request().GetAsync();
            Calendar calendar = await graphClient.Me.Calendar.Request().GetAsync();
            Drive drive = await graphClient.Me.Drive.Request().GetAsync();

            Console.WriteLine("Version 0");
            Console.WriteLine("Display Name user: " + user.DisplayName);
            Console.WriteLine("Calendar Owner Address: " + calendar.Owner.Address);
            Console.WriteLine("Display Drive Type: " + drive.DriveType);
            _publicWatch.Stop();
            var elapsedMs = _publicWatch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

        /// <summary>
        /// Use the HttpQuery class to add requests and then execute them.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        private static async Task Run1(HttpQuery query, GraphServiceClient graphClient)
        {
            /* Request version 1 */
            /* Uses a callback */
            ViewModel model = new ViewModel();
            _publicWatch = Stopwatch.StartNew();
            query.AddRequest<User>(graphClient.Me.Request(), u => model.Me = u);
            query.AddRequest<Calendar>(graphClient.Me.Calendar.Request(), cal => model.Calendar = cal);
            query.AddRequest<Drive>(graphClient.Me.Drive.Request(), dr => model.Drive = dr);

            await query.ExecuteAsync();//run them at the same time :)
            Console.WriteLine("Version 1");
            Console.WriteLine("Display Name user: " + model.Me.DisplayName);
            Console.WriteLine("Display Owner Address: " + model.Calendar.Owner.Address);
            Console.WriteLine("Display Drive Type: " + model.Drive.DriveType);
            _publicWatch.Stop();
            var elapsedMs = _publicWatch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

        /// <summary>
        /// Use the HttpQuery Class to populate a dynamic type to use.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        private static async Task Run2(HttpQuery query, GraphServiceClient graphClient)
        {
            /* Request version 2 */
            /* Uses the dynamic type */
            _publicWatch = Stopwatch.StartNew();
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
            _publicWatch.Stop();
            var elapsedMs = _publicWatch.ElapsedMilliseconds;
            Console.WriteLine($"Elapsed Time {elapsedMs}");
            Console.WriteLine("\r\n\r\n");
        }

        /// <summary>
        /// Use a response handler to launch a fire and forget fashioned call.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        private static void Run3(GraphServiceClient graphClient)
        {
            /* Request version 3 */
            /* Uses the dynamic type */
            ViewModel viewModel = new ViewModel();
            //register an event handler for the model
            viewModel.PropertyChanged += ModelPropertyChanged;
            ResponseHandler responseHandler = new ResponseHandler();
            responseHandler.OnSuccess<User>(u => viewModel.Me = u);
            responseHandler.OnSuccess<Calendar>(cal => viewModel.Calendar = cal);
            responseHandler.OnSuccess<Drive>(dr => viewModel.Drive = dr);
            responseHandler.OnClientError(e => Console.WriteLine(e.Message));
            responseHandler.OnServerError(e => Console.WriteLine(e.Message));

            _publicWatch = Stopwatch.StartNew();
            graphClient.Me.Request().SendGet(responseHandler);
            graphClient.Me.Calendar.Request().SendGet(responseHandler);
            graphClient.Me.Drive.Request().SendGet(responseHandler);

            Console.WriteLine("Version 3");
            Console.WriteLine("Requests Fired Away. Awaiting responses :)");
            Console.ReadKey();//wait for the responses
            Console.ReadKey();//wait for the responses
        }

        /// <summary>
        /// Event handler for the ViewModel class to display certain properties
        /// and elapsed time on the console
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ViewModel model)
            {
                switch (e.PropertyName)
                {
                    case nameof(ViewModel.Drive):
                        Console.WriteLine("Display Drive Type: " + model.Drive.DriveType);
                        break;
                    case nameof(ViewModel.Me):
                        Console.WriteLine("Display Name user: " + model.Me.DisplayName);
                        break;
                    case nameof(ViewModel.Calendar):
                        Console.WriteLine("Calendar Owner Address: " + model.Calendar.Owner.Address);
                        break;
                }

                if (null != model.Drive && null != model.Calendar && null != model.Me)
                {
                    _publicWatch.Stop();
                    var elapsedMs = _publicWatch.ElapsedMilliseconds;
                    Console.WriteLine($"Elapsed Time {elapsedMs}");
                    Console.WriteLine("\r\n\r\n");
                }
            }
        }

        /// <summary>
        /// This is a barrier synchronization mechanism/hack to acquire the token to have in cache
        /// so that requests being sent out in parallel by this instance do not necessarily have to spend time fetching tokens
        /// and use the local copy instead.
        /// </summary>
        /// <returns></returns>
        private static async Task AcquireTokenToCache(GraphServiceClient graphClient)
        {
            //Just authenticate a dummy message but no need to send it out coz we just need a valid token in the cache
            HttpRequestMessage dummyRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/");
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(dummyRequestMessage);
        }
    }
}
