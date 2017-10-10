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
    public enum Side
    {
        Buy,
        Sell
    }
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
        RealtimeDataFeed mdataFeed;

        public API_Interface()
        {
            var credentials = new GdaxCredentials(sandboxAPIKey, sandboxAPIPassphrase, sandboxAPISecret);
            client = new GdaxClient(credentials)
            {
                UseSandbox = true
            };
        }

        public API_Interface(string apiKey, string apiSecret, string apiPassphrase)
        {
            SetCredentials(apiKey, apiPassphrase, apiSecret);
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

        public Order SubmitMarketOrder(string product, Side side, decimal quantity)
        {

        }

        public Order SubmitLimitOrder(string product, Side side, decimal quantity, decimal price)
        {

        }

        public Order SubmitStopOrder()
        {

        }
        public Order CancelLimitOrder()
        {

        }

        public List<Order> GetAllOrders()
        {

        }

        public Order GetOrder(ulong orderID)
        {

        }

        public Ledger GetPositions()
        {

        }

        public void SubscribeToTicker(string product, Action<RealtimeMessage> callbackMethod = null)
        {

        }

        /// <summary>
        /// Subscribes to Bid and Ask Level 2 data for a specific product.
        /// </summary>
        /// <param name="product"></param>
        /// <returns>A tuple with 'First' being the bids and 'Second' being the asks.</returns>
        Tuple<List<BidAskOrder>,List<BidAskOrder>> SubscribeToLevel2(string product)
        {

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
