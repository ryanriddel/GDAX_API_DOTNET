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

        public Dictionary<string, Book> productBook = new Dictionary<string, Book>();

        /// <summary>
        /// Initializes the API using the sandbox connection
        /// </summary>
        public API_Interface()
        {
            try
            {
                SetCredentials(sandboxAPIKey, sandboxAPISecret, api_passphrase, true);
                mdataFeed = new RealtimeDataFeed(client);
                mdataFeed.ConnectWebsocket();
                productBook = mdataFeed.productBook;
            }
            catch (Exception e)
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
                SetCredentials(apiKey, apiSecret, apiPassphrase);
                mdataFeed = new RealtimeDataFeed(client);
                mdataFeed.ConnectWebsocket();
                productBook = mdataFeed.productBook;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        public GdaxClient GetClient()
        {
            return client;
        }

        

        public async Task RunTest()
        {

            Console.WriteLine("Time Test:");
            //var time = await client.GetServerTime();
            //Console.WriteLine(time.Epoch.ToString());


            Console.WriteLine("Accounts Test:   ");
            List<Account> accounts = await GetAccounts();
            foreach (Account acct in accounts)
                Console.WriteLine("Profile ID: " + acct.ProfileId + ", Balance: " + acct.Balance + ",  Available: " + acct.Available);
            Console.Write("\n\n");

            Console.ReadKey();



            Console.WriteLine("Market Order Tests: ");
            Order order = await SubmitMarketOrder("BTC-USD", Side.Buy,  0.314m);
            Console.Write("Market order placed:   ");
            Console.WriteLine("Status: " + order.Status + ", Fill Quantity: " + order.FilledSize.ToString() + ", Order ID: " + order.OrderId.ToString());
            Console.WriteLine("\n");

            Console.ReadKey();

            Order limitOrder = await SubmitLimitOrder("BTC-USD", Side.Buy, new decimal(0.2718), 500m);
            Console.Write("Limit order placed: ");
            Console.WriteLine("Status: " + limitOrder.Status + ", Rem Size: " + (limitOrder.Size - limitOrder.FilledSize).ToString());
            Console.WriteLine("\n");

            Console.ReadKey();

            Console.WriteLine("Cancel limit order test:");
            await CancelLimitOrder(limitOrder.OrderId);
            Console.WriteLine("Cancelled order id=(" + limitOrder.OrderId.ToString() + ").  Current status of limit order is: " + limitOrder.Status);
            Console.WriteLine("\n");

            Console.ReadKey();


            Console.WriteLine("Getting BTC-USD level 2 order book...");
            OrderBook ordBook = await GetLevel2Book("BTC-USD");
            
            Console.Write("Bids:   ");
            for (int i = 0; i < ordBook.Bids.Count; i++)
                for(int j=0; j<ordBook.Bids[i].Length; j++)
                Console.Write(ordBook.Bids[i][j] + "     ");
            Console.WriteLine("\n\n");
            
            Console.Write("Asks:   ");
            for (int i = 0; i < ordBook.Asks.Count; i++)
                for (int j = 0; j < ordBook.Asks[i].Length; j++)
                    Console.Write(ordBook.Asks[i][j] + "     ");
            Console.WriteLine("\n\n");

            Console.ReadKey();




            Console.WriteLine("Getting account history...");
            List<Ledger> hist = await GetAccountHistory(accounts[0].Id);
            foreach (Ledger led in hist)
            {
                Console.WriteLine("Ledger ID: " + led.Id + ", Order ID: " + led.Details.OrderId + "Product ID: " + led.Details.ProductId + ", Transfer Type: " + led.Details.TransferType);
            }


            Console.WriteLine("Live market data test:");
            Console.Write("Press any key to continue....");
            Console.ReadKey();

            Console.WriteLine("Level 2 subscription test:");

            Book book = await SubscribeToLevel2(PRODUCT_BTCperUSD);

            Console.WriteLine("Press enter to continue");
            Console.ReadKey();

            await UnsubscribeLevel2(PRODUCT_BTCperUSD);
            Console.WriteLine("Level 2 unsubscribed");
            Console.ReadKey();

            Console.WriteLine("Subscribing to ticker...");
            Thread.Sleep(1000);

            await SubscribeToTicker("BTC-USD", new TickerMessageReceivedHandler(delegate (string eventtime, string productID, string price, string side,
                string lastSize, string bestBid, string bestAsk)
            {
                Console.WriteLine("Time: " + eventtime + ", " + "Product ID: " + productID + ", Price: " + price + ", Side: " + side);
                Console.WriteLine("Last size: " + lastSize + ", Best Bid: " + bestBid + ", " + bestAsk);
                Console.WriteLine();
            }));


            Console.WriteLine("Press enter");
            Console.ReadKey();

            await UnsubscribeTicker("BTC-USD");

            Thread.Sleep(1000);
            Console.WriteLine("Unsubscribed.\n");

            

           
            Console.WriteLine("\n\n\nTest Complete");
            Console.ReadKey();
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
            await mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""unsubscribe"",""channels"":[""ticker""], ""product_ids"" : [""" + product + "\"]}}"));
        }

        public async Task UnsubscribeLevel2(string product)
        {
            await mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""unsubscribe"",""channels"":[""level2""], ""product_ids"" : [""" + product + "\"]}}"));
        }

        public async Task SubscribeToTicker(string product, TickerMessageReceivedHandler callbackMethod = null)
        {

            await mdataFeed.AddSubscription(product,
                String.Format(@"{{""type"": ""subscribe"",""channels"":[""ticker""], ""product_ids"" : [""" + product + "\"]}}"));

            if (callbackMethod != null)
                mdataFeed.OnTicker += callbackMethod;

            return;
        }
        
        /// <summary>
        /// Subscribes to Bid and Ask Level 2 data for a specific product.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="callbackMethod">This is an optional callback method that gets called when a message is received.  Remember, you can also subscribe to the market data feed's events.</param>
        /// <returns>A tuple with 'First' being the bids and 'Second' being the asks.</returns>
        public async Task<Book> SubscribeToLevel2(string product, MarketDataMessageHandler callbackMethod = null)
        {
        Book b = await mdataFeed.AddSubscription(product,
            String.Format(@"{{""type"": ""subscribe"",""channels"":[""level2""], ""product_ids"" : [""" + product + "\"]}}"));

        if (callbackMethod != null)
            mdataFeed.OnMarketDataMessageReceived += callbackMethod;
        return b;

        }
        

        public void onMsgReceived(RealtimeMessage msg)
        {
        }
        private void MdataFeed_Updated(RealtimeMessage msg, List<BidAskOrder> bids, List<BidAskOrder> asks)
        {
            
        }
        
        


    }
}
