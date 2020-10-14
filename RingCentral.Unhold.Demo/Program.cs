using System;
using System.Threading.Tasks;
using dotenv.net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RingCentral.Unhold.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            DotEnv.Config(true);

            Task.Run(async () =>
            {
                using (var rc = new RestClient(
                    Environment.GetEnvironmentVariable("RINGCENTRAL_CLIENT_ID"),
                    Environment.GetEnvironmentVariable("RINGCENTRAL_CLIENT_SECRET"),
                    Environment.GetEnvironmentVariable("RINGCENTRAL_SERVER_URL")
                ))
                {
                    await rc.Authorize(
                        Environment.GetEnvironmentVariable("RINGCENTRAL_USERNAME"),
                        Environment.GetEnvironmentVariable("RINGCENTRAL_EXTENSION"),
                        Environment.GetEnvironmentVariable("RINGCENTRAL_PASSWORD")
                    );
                    var eventFilters = new[]
                    {
                        "/restapi/v1.0/account/~/extension/~/telephony/sessions"
                    };
                    var subscription = new Subscription(rc, eventFilters, async message =>
                    {
                        dynamic telephonySessionsEvent = JObject.Parse(message);
                        Console.WriteLine(JsonConvert.SerializeObject(telephonySessionsEvent, Formatting.Indented));
                        var party = telephonySessionsEvent.body.parties[0];
                        if (party.status.code == "Hold")
                        {
                            Console.WriteLine("The phone call is held");
                            Console.WriteLine("The app is going to unhold it in 3 seconds");
                            await Task.Delay(3000);
                            Console.WriteLine(telephonySessionsEvent.body.telephonySessionId);
                            var callParty = await rc.Restapi().Account().Telephony()
                                .Sessions(telephonySessionsEvent.body.telephonySessionId.ToString()).Parties(party.id.ToString()).Unhold()
                                .Post();
                            Console.WriteLine("The call has been unheld by the app");
                            Console.WriteLine(JsonConvert.SerializeObject(callParty, Formatting.Indented));
                        }
                    });
                    await subscription.Subscribe();
                    await Task.Delay(999999999);
                }
            }).GetAwaiter().GetResult();
        }
    }
}