using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

using Gdax;
using Gdax.Internal;
using System.Security.Cryptography;
using System.Threading;
using Gdax.Models;

namespace gdax_rsquared
{
    class Program
    {



        static void Main(string[] args)
        {
            API_Interface newtest = new API_Interface();

            newtest.RunTest().Wait();

            Console.WriteLine("Its done.");
            Console.ReadKey();
        }
    }
}
