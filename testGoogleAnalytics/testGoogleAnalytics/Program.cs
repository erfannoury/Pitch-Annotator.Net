using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Analytics.v3;
using Google.Apis.Auth.OAuth2;
using MarkdownLog;
using System.IO;
using System.Threading;

namespace testGoogleAnalytics
{
    class Program
    {
        static void Main(string[] args)
        {
            UserCredential credentials = null;
            AnalyticsService gaservice = null;
            try
            {
                var prog = new Program();
                prog.Authenticate(credentials).Wait();
                Console.WriteLine("UserID: {0}", credentials.UderId);
                prog.CreateService(gaservice, credentials);
                var features = gaservice.Features;
                Console.WriteLine(features.ToMarkdownBulletedList());
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                    Console.WriteLine(e.Message);
            }
        }
        /// <summary>
        /// This will authenticate the application using OAuth2.0
        /// </summary>
        /// <param name="credentials">UserCredentials object that contains the authenticated credentials information.</param>
        /// <returns></returns>
        private async Task Authenticate(UserCredential credentials)
        {
            using (var stream = new FileStream("C:/Users/Erfan/Documents/MEGA/Personal/client_secret_manual.json", FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("Can read stream: {0}", stream.CanRead);
                //credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, new[] { AnalyticsService.Scope.AnalyticsReadonly }, "erfan", CancellationToken.None, new FileDataStore("Analytics.CafeBaaaz"));
                //credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, new[] { AnalyticsService.Scope.AnalyticsReadonly }, "erfan", CancellationToken.None);
                credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets 
                {
                    ClientId = "151111364175-m5ica5esjs41pssgcngvlvsk5nsq7gsf.apps.googleusercontent.com",
                    ClientSecret = "Nsz5pEDWQJDRbty1-dmKC3CW"
                }, new[] { AnalyticsService.Scope.AnalyticsReadonly }, "erfan", CancellationToken.None);
            }
        }
        /// <summary>
        /// This will create a Google Analytics service using the credentials provided
        /// </summary>
        /// <param name="service">AnalyticsService object using which Google Analytics services are acceible.</param>
        /// <param name="cred">User Credentials to authenticate the access to the API.</param>
        /// <returns></returns>
        private void CreateService(AnalyticsService service, UserCredential cred)
        {
            service = new AnalyticsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = cred,
                ApplicationName = "API Project"
            });
        }

        private async Task ExecuteService(AnalyticsService service)
        {
            
        }
    }
}
