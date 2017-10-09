using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Gdax;
using Gdax.Internal;
using System.Security.Cryptography;
using System.Threading;
using Gdax.Models;
using System.Net.WebSockets;

namespace gdax_rsquared
{
    class API_Interface
    {

        public const string sandboxRestURL = "https://api-public.sandbox.gdax.com";
        public const string sandboxWebsocketURL = "wss://ws-feed-public.sandbox.gdax.com";
        public const string sandboxAPIKey = "cd3295c7efebd973e65083ad8059be6d";
        public const string sandboxAPISecret = "+haqm+0GMl4AwXOBMPo03s2ICTx4+kqG8Tyw+JcmPA4zDfa9l1rs4ZwmC6y8Ty0JslhdezQqtUjjZcNaiLpF/w==";
        public const string sandboxAPIPassphrase = "ztg3s5dyi5";
        public const string applicationName = "rsquaredtest";
        public const string liveRestURL = "https://api.gdax.com";

        GdaxClient client;
        RealtimeDataFeed mdataFeed;

        public API_Interface()
        {

        }

        public async Task Run()
        {

            Console.WriteLine("Press any key to begin the test.");
            Console.ReadKey();
            var credentials = new GdaxCredentials(sandboxAPIKey, sandboxAPIPassphrase, sandboxAPISecret);
            Console.WriteLine("Authentication successful");



            client = new GdaxClient(credentials)
            {
                UseSandbox = true
            };

            ISystemClock clk = new Gdax.Internal.SystemClock();


            GdaxAuthenticationHandler authHandler = new GdaxAuthenticationHandler(credentials);
            authHandler.InnerHandler = new NoopHandler();

            HttpClient http = new HttpClient(authHandler);

            /*
            IList<Account> accounts;

            try
            {

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            accounts = await client.GetAccounts();

            Console.WriteLine("Beginning tests...");
            Console.Write("Accounts Test:   ");
            foreach (Account acct in accounts)
                Console.Write("Profile ID: " + acct.ProfileId + ", Balance: " + acct.Balance + "       ");

            Console.WriteLine("Time Test:");


            var time = await client.GetServerTime();

            Console.WriteLine(time.Epoch.ToString());
            Console.WriteLine("Market Order Tests: ");
            try
            {

                Order order = await client.PlaceMarketOrder(Side.Buy, "BTC-USD", 0.314m);
                Console.WriteLine();
                Console.Write("Market order placed:");
                Console.WriteLine("Status: " + order.Status + ", Fill Quantity: " + order.FilledSize.ToString() + ", Order ID: " + order.OrderId.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Order limitOrder = await client.PlaceLimitOrder(Side.Buy, "BTC-USD", new decimal(0.2718), 500);

            Console.WriteLine("Limit order placed:");
            Console.WriteLine("Status: " + limitOrder.Status + ", Rem Size: " + (limitOrder.Size - limitOrder.FilledSize).ToString());

            await client.CancelOrder(limitOrder.OrderId.ToString());

            Console.WriteLine("Cancelled order id=(" + limitOrder.OrderId.ToString() + ").  Current status of limit order is: " + limitOrder.Status);

            

            Console.WriteLine("Getting order book...");
            OrderBook ordBook = await client.GetOrderBook("BTC-USD", OrderBookLevel.Level2);

            Console.Write("Bids:   ");
            for (int i = 0; i < ordBook.Bids.Count; i++)
                Console.Write(ordBook.Bids[i] + "     ");

            Console.WriteLine('\n');

            Console.Write("Asks:   ");
            for (int i = 0; i < ordBook.Asks.Count; i++)
                Console.Write(ordBook.Asks[i] + "     ");


            Console.WriteLine("Getting account history:");
            PagedResults<Ledger, int?> hist = await client.GetAccountHistory(accounts[0].Id);

            foreach (Ledger led in hist.Results)
            {
                Console.WriteLine("Ledger ID: " + led.Id + ", Order ID: " + led.Details.OrderId + "Product ID: " + led.Details.ProductId + ", Transfer Type: " + led.Details.TransferType);
            }*/

            RealtimeDataFeed mdataFeed = new RealtimeDataFeed(client);
            mdataFeed.Updated += MdataFeed_Updated;
            mdataFeed.Subscribe("BTC-USD", onMsgReceived);
            Console.ReadKey();

            
            return;
        }

        public void onMsgReceived(RealtimeMessage msg)
        {
            Console.WriteLine("MktData: " + msg.Type + "   " + msg.Sequence + "    " + msg.Price);
        }
        private void MdataFeed_Updated(object sender, EventArgs e)
        {
            
        }

        private class NoopHandler : HttpMessageHandler
        {
            protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(new HttpResponseMessage
                {
                    RequestMessage = request
                });
            }
        }
        


    }
}
