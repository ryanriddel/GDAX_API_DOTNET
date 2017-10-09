using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gdax;
using Gdax.Models;
using WebSocket4Net;

namespace gdax_rsquared
{
    public class RealtimeDataFeed 
    {
        private const String Product = "BTC-USD";
        private readonly Object _askLock = new Object();
        private readonly Object _bidLock = new Object();

        private readonly Object _spreadLock = new Object();

        GdaxClient gdaxClient;

        public RealtimeDataFeed(GdaxClient client) 
        {
            gdaxClient = client;
            this._sells = new List<BidAskOrder>();
            this._buys = new List<BidAskOrder>();

            this.Asks = new List<BidAskOrder>();
            this.Bids = new List<BidAskOrder>();

            
        }

        public void StartFeed()
        {
            this.ResetStateWithFullOrderBook();
        }
        private List<BidAskOrder> _sells
        {
            get; set;
        }
        private List<BidAskOrder> _buys
        {
            get; set;
        }

        public List<BidAskOrder> Asks
        {
            get; set;
        }
        public List<BidAskOrder> Bids
        {
            get; set;
        }

        public Decimal Spread
        {
            get
            {
                lock (this._spreadLock)
                {
                    if (!this.Bids.Any() || !this.Asks.Any())
                    {
                        return 0;
                    }

                    var maxBuy = this.Bids.Select(x => x.Price).Max();
                    var minSell = this.Asks.Select(x => x.Price).Min();

                    return minSell - maxBuy;
                }
            }
        }

        public event EventHandler Updated;
        List<BidAskOrder> convertBidsToBuys(OrderBook ob)
        {
            List<BidAskOrder> baoList = new List<BidAskOrder>();
            for(int i=0; i<ob.Bids.Count; i++)
            {
                BidAskOrder bao = new BidAskOrder();
                bao.Id = ob.Bids[i][0];
                bao.Price = Convert.ToDecimal(ob.Bids[i][1]);
                bao.Size = Convert.ToDecimal(ob.Bids[i][2]);
                baoList.Add(bao);
            }
            return baoList;
        }
        List<BidAskOrder> convertAsksToSells(OrderBook ob)
        {
            List<BidAskOrder> baoList = new List<BidAskOrder>();
            for (int i = 0; i < ob.Asks.Count; i++)
            {
                BidAskOrder bao = new BidAskOrder();
                bao.Id = ob.Asks[i][0];
                bao.Price = Convert.ToDecimal(ob.Asks[i][1]);
                bao.Size = Convert.ToDecimal(ob.Asks[i][2]);
                baoList.Add(bao);
            }
            return baoList;
        }

        private async void ResetStateWithFullOrderBook()
        {

            OrderBook ob = await gdaxClient.GetOrderBook(Product, Gdax.Models.OrderBookLevel.Level3);
            
            lock (this._spreadLock)
            {
                lock (this._askLock)
                {
                    lock (this._bidLock) 
                    {
                        this._buys = convertBidsToBuys(ob);
                        this._sells = convertAsksToSells(ob);

                        this.Bids = this._buys.ToList();
                        this.Asks = this._sells.ToList();
                    }
                }
            }

            this.OnUpdated();
            
        }

        private void OnUpdated()
        {
            this.Updated?.Invoke(this, new EventArgs());
        }
        

        private void OnOrderBookEventReceived(RealtimeMessage message)
        {
            if (message is RealtimeReceived)
            {
                var receivedMessage = message as RealtimeReceived;
                this.OnReceived(receivedMessage);
            }

            else if (message is RealtimeOpen)
            {
            }

            else if (message is RealtimeDone)
            {
                var doneMessage = message as RealtimeDone;
                this.OnDone(doneMessage);
            }

            else if (message is RealtimeMatch)
            {
            }

            else if (message is RealtimeChange)
            {
            }

            this.OnUpdated();
        }

        private void OnReceived(RealtimeReceived receivedMessage)
        {
            var order = new BidAskOrder
            {
                Id = receivedMessage.OrderId,
                Price = receivedMessage.Price,
                Size = receivedMessage.Size
            };


            lock (this._spreadLock)
            {
                if (receivedMessage.Side == "buy")
                {
                    lock (this._bidLock)
                    {
                        this._buys.Add(order);
                        this.Bids = this._buys.ToList();
                    }
                }

                else if (receivedMessage.Side == "sell")
                {
                    lock (this._askLock)
                    {
                        this._sells.Add(order);
                        this.Asks = this._sells.ToList();
                    }
                }
            }
        }

        private void OnDone(RealtimeDone message)
        {
            lock (this._spreadLock)
            {
                lock (this._askLock)
                {
                    lock (this._bidLock)
                    {
                        this._buys.RemoveAll(b => b.Id == message.OrderId);
                        this._sells.RemoveAll(a => a.Id == message.OrderId);

                        this.Bids = this._buys.ToList();
                        this.Asks = this._sells.ToList();
                    }
                }
            }
        }

        public async void Subscribe(String product, Action<RealtimeMessage> onMessageReceived)
        {
            if (String.IsNullOrWhiteSpace(product))
            {
                throw new ArgumentNullException(nameof(product));
            }

            if (onMessageReceived == null)
            {
                throw new ArgumentNullException(nameof(onMessageReceived), "Message received callback must not be null.");
            }

            var uri = new Uri("wss://ws-feed.exchange.coinbase.com");
            var webSocketClient = new WebSocket4Net.WebSocket("wss://ws-feed.gdax.com");
            
            var cancellationToken = new CancellationToken();
            var requestString = String.Format(@"{{""type"": ""subscribe"",""channels"":[""heartbeat""], ""product_ids"" : [""ETH-EUR""]}}");
            //var requestString = String.Format(@"{{""type"": ""subscribe"",""channels"":[""heartbeat""]}}");
            var requestBytes = Encoding.UTF8.GetBytes(requestString);
            arm = onMessageReceived;

            webSocketClient.DataReceived += WebSocketClient_DataReceived;
            webSocketClient.MessageReceived += WebSocketClient_MessageReceived;
            webSocketClient.EnableAutoSendPing = true;
            webSocketClient.Error += WebSocketClient_Error;
            webSocketClient.Opened += WebSocketClient_Opened;
            webSocketClient.Closed += WebSocketClient_Closed;
            webSocketClient.ReceiveBufferSize = 1024 * 1024 * 5;
            webSocketClient.EnableAutoSendPing = true;
            webSocketClient.NoDelay = true;
            
            webSocketClient.Open();

            while (webSocketClient.State == WebSocket4Net.WebSocketState.Connecting)
            {
                Console.WriteLine("Waiting...");
                System.Threading.Thread.Sleep(100);
            }
            Console.WriteLine("Connected!");

            Console.WriteLine("Status: " + webSocketClient.State.ToString());
                var subscribeRequest = new ArraySegment<Byte>(requestBytes);
                List<ArraySegment<Byte>> blist = new List<ArraySegment<byte>>();
                blist.Add(subscribeRequest);
            //webSocketClient.Send(requestString);

            requestString = String.Format(@"{{""type"": ""subscribe"",""product_ids"":[""BTC-USD""], ""channels"":[""full""]}}");
            webSocketClient.Send(requestString);

            Console.WriteLine("Handshaked: " + webSocketClient.Handshaked.ToString() + ", " + webSocketClient.State.ToString());

            

            /*webSocketClient.Send(@"{
                ""type"": ""snapshot"",
    ""product_ids"": [""BTC -EUR""],
    ""bids"": [[""1"", ""2""]],
    ""asks"": [[""2"", ""3""]]}");*/

            for(int i=0; i<5; i++)
            {
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine(webSocketClient.State.ToString());
            }
}

        private void WebSocketClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Message received: " + e.Message.ToString());
            try
            {
                var jsonResponse = e.Message;
                var jToken = JToken.Parse(jsonResponse);
                if (jToken == null)
                    return;

                var typeToken = jToken["type"];
                if (typeToken == null || jToken["sequence"] == null || jToken["price"] == null)
                {
                    return;
                }


                var type = typeToken.Value<String>();
                RealtimeMessage realtimeMessage = null;

                switch (type)
                {
                    case "received":
                        realtimeMessage = new RealtimeReceived(jToken);
                        break;
                    case "open":
                        realtimeMessage = new RealtimeOpen(jToken);
                        break;
                    case "done":
                        realtimeMessage = new RealtimeDone(jToken);
                        break;
                    case "match":
                        realtimeMessage = new RealtimeMatch(jToken);
                        break;
                    case "change":
                        realtimeMessage = new RealtimeChange(jToken);
                        break;
                    default:
                        break;
                }

                if (realtimeMessage == null)
                {
                    return;
                }

                this.OnOrderBookEventReceived(realtimeMessage);
                if (arm != null)
                    arm(realtimeMessage);
            }
            catch(Exception a)
            {
                Console.WriteLine("Bad msg");
            }
        }

        private void WebSocketClient_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("The socket closed...");
        }

        private void WebSocketClient_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("Holy shit it opened!");
        }

        private void WebSocketClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.InnerException);
        }

        volatile Action<RealtimeMessage> arm;
        private void WebSocketClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("MESSAGE RECEIVED!");
            try
            {
                var jsonResponse = Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);

                if (jsonResponse == null)
                    return;

                var jToken = JToken.Parse(jsonResponse);

                if (jToken == null)
                    return;

                var typeToken = jToken["type"];
                if (typeToken == null || jToken["sequence"] == null || jToken["price"] == null)
                {
                    return;
                }

                var type = typeToken.Value<String>();
                RealtimeMessage realtimeMessage = null;

                switch (type)
                {
                    case "received":
                        realtimeMessage = new RealtimeReceived(jToken);
                        break;
                    case "open":
                        realtimeMessage = new RealtimeOpen(jToken);
                        break;
                    case "done":
                        realtimeMessage = new RealtimeDone(jToken);
                        break;
                    case "match":
                        realtimeMessage = new RealtimeMatch(jToken);
                        break;
                    case "change":
                        realtimeMessage = new RealtimeChange(jToken);
                        break;
                    default:
                        break;
                }

                if (realtimeMessage == null)
                {
                    return;
                }

                this.OnOrderBookEventReceived(realtimeMessage);
                if (arm != null)
                    arm(realtimeMessage);
            }
            catch(Exception a)
            {
                Console.WriteLine("Whoops");
            }
        }
    }

    public class BidAskOrder
    {
        public Decimal Price { get; set; }
        public Decimal Size { get; set; }
        public String Id { get; set; }
    }
}
