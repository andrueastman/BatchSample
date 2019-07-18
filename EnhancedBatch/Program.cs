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

            User user = null;
            Photo photo = null;
            
            var user2 = graphClient.Me.Request().GetAsync().Result;

            var query = new HttpQuery(graphClient);
            query.AddRequest<User>(graphClient.Me.Request(), u => user = u);
            query.AddRequest<Photo>(graphClient.Me.Calendar.Events.Request(), p => photo = p);
            //query.AddRequest<MailFolderMessagesCollectionPage>(graphClient.Me.MailFolders.Inbox.Messages.Request(), m => mail = m);

            query.ExecuteAsync();

            Console.WriteLine("Display Name: " + user.DisplayName);
            Console.WriteLine("Photo: " + photo.TakenDateTime);

        }
    }
}
