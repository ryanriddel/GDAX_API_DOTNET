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
    public delegate void MarketDataMessageHandler(RealtimeMessage msg, List<BidAskOrder> bids, List<BidAskOrder> asks);

    public class RealtimeDataFeed 
    {
        private const String Product = "BTC-USD";
        private readonly Object _askLock = new Object();
        private readonly Object _bidLock = new Object();

        private readonly Object _spreadLock = new Object();

        GdaxClient gdaxClient;
        
        
        public event MarketDataMessageHandler OnMarketDataMessageReceived;
        public event MarketDataMessageHandler OnLimitOrderOpened;
        public event MarketDataMessageHandler OnOrderCancelled;
        public event MarketDataMessageHandler OnOrderFilled;
        public event MarketDataMessageHandler OnOrderModified;
        public event MarketDataMessageHandler OnOrderMatched;

        private WebSocket4Net.WebSocket webSocketClient;

        public Dictionary<string, Book> productBook = new Dictionary<string, Book>();
        private Dictionary<string, Book> _productBook = new Dictionary<string, Book>();

        public RealtimeDataFeed(GdaxClient client) 
        {
            gdaxClient = client;

            ConnectWebsocket();
        }

        
        private void OnOrderBookEventReceived(RealtimeMessage message)
        {
            if (message is RealtimeReceived)
            {
                var receivedMessage = message as RealtimeReceived;
                this.OnReceived(receivedMessage);

                if (OnMarketDataMessageReceived != null)
                    OnMarketDataMessageReceived.Invoke(receivedMessage, productBook[receivedMessage.ProductID].Bids, productBook[receivedMessage.ProductID].Asks);
            }

            else if (message is RealtimeOpen)
            {
                //limit order has been opened

            if(OnLimitOrderOpened != null)
                    OnLimitOrderOpened.Invoke(message, productBook[message.ProductID].Bids, productBook[message.ProductID].Asks);
            }

            else if (message is RealtimeDone)
            {
                
                var doneMessage = message as RealtimeDone;
                this.OnDone(doneMessage);

                if (doneMessage.Reason == "filled")
                {
                    if(OnOrderFilled != null)
                        OnOrderFilled.Invoke(message, productBook[message.ProductID].Bids, productBook[message.ProductID].Asks);
                }
                else
                {
                    //order has been cancelled
                    if(OnOrderCancelled != null)
                    OnOrderCancelled.Invoke(message, productBook[message.ProductID].Bids, productBook[message.ProductID].Asks);
                }
                
            }

            else if (message is RealtimeMatch)
            {
                //new trade happened
                if(OnOrderMatched != null)
                    OnOrderMatched.Invoke(message, productBook[message.ProductID].Bids, productBook[message.ProductID].Asks);
            }

            else if (message is RealtimeChange)
            {
                //an order has been modified
                if(OnOrderModified != null)
                    OnOrderModified.Invoke(message, productBook[message.ProductID].Bids, productBook[message.ProductID].Asks);
                return;
            }
            
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
                        _productBook[receivedMessage.ProductID].Bids.Add(order);
                        productBook[receivedMessage.ProductID].Bids = _productBook[receivedMessage.ProductID].Bids.ToList();
                    }
                }

                else if (receivedMessage.Side == "sell")
                {
                    lock (this._askLock)
                    {
                        _productBook[receivedMessage.ProductID].Asks.Add(order);
                        productBook[receivedMessage.ProductID].Asks = _productBook[receivedMessage.ProductID].Asks.ToList();
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
                        _productBook[message.ProductID].Bids.RemoveAll(b => b.Id == message.OrderId);
                        _productBook[message.ProductID].Asks.RemoveAll(a => a.Id == message.OrderId);

                        productBook[message.ProductID].Bids = _productBook[message.ProductID].Bids.ToList();
                        productBook[message.ProductID].Asks = _productBook[message.ProductID].Asks.ToList();
                    }
                }
            }
            
        }

        public async void ConnectWebsocket()
        {
            if (webSocketClient.State == WebSocket4Net.WebSocketState.Open)
                return;

            webSocketClient = new WebSocket4Net.WebSocket("wss://ws-feed.gdax.com");
            
            webSocketClient.MessageReceived += WebSocketClient_MessageReceived;


            webSocketClient.EnableAutoSendPing = true;
            webSocketClient.Error += WebSocketClient_Error;
            webSocketClient.Opened += WebSocketClient_Opened;
            webSocketClient.Closed += WebSocketClient_Closed;
            webSocketClient.ReceiveBufferSize = 1024 * 1024 * 5;
            webSocketClient.EnableAutoSendPing = true;
            webSocketClient.NoDelay = true;

            webSocketClient.Open();

            for (int i = 0; i < 5 && (webSocketClient.State == WebSocket4Net.WebSocketState.Connecting); i++)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (webSocketClient.State == WebSocket4Net.WebSocketState.Open)
                Console.WriteLine("Market data websocket connected!");
            else
                Console.WriteLine("Market data websocket failed to open");
        }

        public async Task AddSubscription(String product, string requestString = "")
        {
            if (String.IsNullOrWhiteSpace(product))
            {
                throw new ArgumentNullException(nameof(product));
            }
            if(product.Length != 7 || product.Contains("-") == false)
            {
                throw new Exception("Malformed product: " + product + ".  It should have seven characters, one of which is a dash.");
            }

            if(productBook.ContainsKey(product) == false)
                productBook[product] = new Book(product);
            
            
            Console.WriteLine("Websocket request: " + requestString);
            if (requestString == "")
                requestString = String.Format(@"{{""type"": ""subscribe"",""channels"":[""heartbeat""], ""product_ids"" : [""ETH-EUR""]}}");
            
            
            //requestString = String.Format(@"{{""type"": ""subscribe"",""product_ids"":[""BTC-USD""], ""channels"":[""full""]}}");
            webSocketClient.Send(requestString);
            
}

        private void WebSocketClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
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
            Console.WriteLine("Socket successfully opened!");
        }

        private void WebSocketClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine("Websocket error: " + e.Exception.InnerException);
        }

        
        List<BidAskOrder> convertBidsToBuys(OrderBook ob)
        {
            List<BidAskOrder> baoList = new List<BidAskOrder>();
            for (int i = 0; i < ob.Bids.Count; i++)
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
    }

    public class BidAskOrder
    {
        public Decimal Price { get; set; }
        public Decimal Size { get; set; }
        public String Id { get; set; }
    }

    public class Book
    {
        public string product;
        public List<BidAskOrder> Bids;
        public List<BidAskOrder> Asks;

        public Book(string _product)
        {
            product = _product;

            Bids = new List<BidAskOrder>();
            Asks = new List<BidAskOrder>();
        }
        public Book(string _product, List<BidAskOrder> bids, List<BidAskOrder> asks)
        {
            product = _product;
            Bids = bids;
            Asks = asks;
            Bids = new List<BidAskOrder>();
            Asks = new List<BidAskOrder>();
        }
    }


}
