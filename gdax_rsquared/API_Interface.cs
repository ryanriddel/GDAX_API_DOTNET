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
        public string PRODUCT_BTCperUSD = "BTC-USD";
        public const string sandboxRestURL = "https://api-public.sandbox.gdax.com";
        public const string sandboxWebsocketURL = "wss://ws-feed-public.sandbox.gdax.com";
        private const string sandboxAPIKey = "cd3295c7efebd973e65083ad8059be6d";
        private const string sandboxAPISecret = "+haqm+0GMl4AwXOBMPo03s2ICTx4+kqG8Tyw+JcmPA4zDfa9l1rs4ZwmC6y8Ty0JslhdezQqtUjjZcNaiLpF/w==";
        private const string sandboxAPIPassphrase = "ztg3s5dyi5";
        private const string sandboxApplicationName = "rsquaredtest";
        public const string liveRestURL = "https://api.gdax.com";

        private string api_secret = sandboxAPISecret;
        private string api_passphrase = sandboxAPIPassphrase;
        private string api_key = sandboxAPIKey;
        private string application_name = sandboxApplicationName;

        
        GdaxClient client;
        RealtimeDataFeed mdataFeed = null;

        /// <summary>
        /// Initializes the API using the sandbox connection
        /// </summary>
        public API_Interface()
        {
            try
            {
                SetCredentials(sandboxAPIKey, sandboxAPIPassphrase, sandboxAPISecret, true);
                mdataFeed = new RealtimeDataFeed(client);
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Initializes the API using user-defined login credentials, which will connect to the non-sandbox gdax
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiSecret"></param>
        /// <param name="apiPassphrase"></param>
        public API_Interface(string apiKey, string apiSecret, string apiPassphrase)
        {
            try
            {
                SetCredentials(apiKey, apiPassphrase, apiSecret);
                mdataFeed = new RealtimeDataFeed(client);
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        public void SetCredentials(string apiKey, string apiSecret, string apiPassphrase, bool useSandbox = false)
        {
            var credentials = new GdaxCredentials(apiKey, apiPassphrase, apiSecret);

            client = new GdaxClient(credentials)
            {
                UseSandbox = useSandbox
            };

            Console.WriteLine("GDAX authentication successful.");
        }

        public async Task<Order> SubmitMarketOrder(string product, Side side, decimal quantity)
        {
            Order order = await client.PlaceMarketOrder(side, product, quantity);
            return order;
        }

        public async Task<Order> SubmitLimitOrder(string product, Side side, decimal quantity, decimal price)
        {
           Order limitOrder = await client.PlaceLimitOrder(side, product, quantity, price);
            return limitOrder;
        }

        public async Task<Order> SubmitStopOrder(string product, Side side, decimal quantity, decimal price)
        {
            Order stopOrder = await client.PlaceStopOrder(side, product, price, quantity);
            return stopOrder;
        }
        public async Task CancelLimitOrder(Guid orderID)
        {
            await client.CancelOrder(orderID.ToString());
        }

        public async Task<List<Guid>> CancelAllOrders()
        {
            IList<Guid> lg = await client.CancelAllOrders();
            return lg.ToList<Guid>();
        }
        
        public async Task<OrderBook> GetLevel2Book(string product)
        {
            OrderBook ob = await client.GetOrderBook(product, OrderBookLevel.Level2);
            return ob;

        }

        public async Task<List<Account>> GetAccounts()
        {
            IList<Account> al = await client.GetAccounts();
            return al.ToList<Account>();
        }

        public async Task<List<Ledger>> GetAccountHistory(Guid accountID)
        {
            PagedResults<Ledger, int?> pr = await client.GetAccountHistory(accountID);
            return pr.Results.ToList<Ledger>();
        }

        public async Task UnsubscribeTicker(string product)
        {
            mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""unsubscribe"",""channels"":[""ticker""], ""product_ids"" : ["" " + product + " \"]}}"));
        }

        public async Task UnsubscribeLevel2(string product)
        {
            mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""unsubscribe"",""channels"":[""level2""], ""product_ids"" : ["" " + product + " \"]}}"));
        }

        public async Task SubscribeToTicker(string product, Action<RealtimeMessage> callbackMethod = null)
        {
            mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""subscribe"",""channels"":[""ticker""], ""product_ids"" : ["" " + product + " \"]}}"));
        }

        /// <summary>
        /// Subscribes to Bid and Ask Level 2 data for a specific product.
        /// </summary>
        /// <param name="product"></param>
        /// <returns>A tuple with 'First' being the bids and 'Second' being the asks.</returns>
          public async Task SubscribeToLevel2(string product)
          {
            mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""subscribe"",""channels"":[""heartbeat""], ""product_ids"" : [""" + product + "\"\"]}}"));

          }
        public async Task Run()
        {

            Console.WriteLine("Press any key to begin the test.");
            

            ISystemClock clk = new Gdax.Internal.SystemClock();
            
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
            mdataFeed.OnMarketDataMessageReceived += MdataFeed_Updated;
            mdataFeed.Subscribe("BTC-USD");
            Console.ReadKey();

            
            return;
        }

        public void onMsgReceived(RealtimeMessage msg)
        {
            Console.WriteLine("MktData: " + msg.Type + "   " + msg.Sequence + "    " + msg.Price);
        }
        private void MdataFeed_Updated(RealtimeMessage msg, List<BidAskOrder> bids, List<BidAskOrder> asks)
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
