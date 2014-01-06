using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cryptsy.OrderInfo;
using Cryptsy.SampleResponse1JsonTypes;
using Example;
using MarketOrders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cryptsy
{
    class Program
    {
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static string key = "";
        static string sKey = "";

        static double sellFee = 0.997f;
        static double buyFee = 0.998f;
        static bool emergencyStop = false;
        private const short roundsForBalanceInfo = 50;
        private static int maxSimuOrdersPerCoin = 3;
        private static double cleanTime = 1;
        private const int roundTimeOutWhenCleaning = 20;
        private static int cleanCount = 0;
        private static DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );

        public static string ByteToString( byte[] buff )
        {
            string sbinary = "";

            for( int i = 0; i < buff.Length; i++ )
            {
                sbinary += buff[i].ToString( "X2" ); // hex format
            }
            return ( sbinary );
        }

        static void Main( string[] args )
        {
            minimumBTCEarnings = 1.005d;
            transactionDone = 0;
            //ARGS maxOrders / cleanTime in Hours / margin in BTC 
            if( args[0] != null )
            {
                marketString = args[0];
            }
            if( args.Length > 1 )
            {
                if( args[1] != null )
                {
                    maxSimuOrdersPerCoin = int.Parse( args[1] );
                }
                if( args[2] != null )
                {
                    cleanTime = double.Parse( args[2] );
                }
                if( args[3] != null )
                {
                    minimumBTCEarnings = double.Parse( args[3] );
                }
            }
            Console.Title = marketString + " trader with maxOrders=" + maxSimuOrdersPerCoin + " cleanTime=" + cleanTime + " marginBTC=" + minimumBTCEarnings.ToString();
            WebClient client = new WebClient();     // The methods needed taking the content of the URL
            client.CachePolicy = new RequestCachePolicy( RequestCacheLevel.BypassCache );


            int count = 0;
            int countClean = 0;
            SampleResponse1 response;
            Markets markets = null;
            try
            {
                string result = client.DownloadString( "http://pubapi.cryptsy.com/api.php?method=marketdatav2" );    // Putting the JSON content of the URL into a string
                JObject obj = JObject.Parse( result );
                response = JsonConvert.DeserializeObject<SampleResponse1>( result );
                markets = response.Return.Markets;
            }
            catch( Exception e )
            {
                Console.WriteLine( "Fetching data failed! (round " + count++ + ")" );
                System.Threading.Thread.Sleep( 5000 );
            }

            //START LOOP 
            while( !emergencyStop && markets != null )
            {
                if( marketString.Equals( "XPM" ) )
                {
                    handleXPMMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "MEC" ) )
                {
                    handleMECMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "DGC" ) )
                {
                    handleDGCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "PXC" ) )
                {
                    handlePXCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "YAC" ) )
                {
                    handleYACMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "GLD" ) )
                {
                    handleGLDMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "WDC" ) )
                {
                    handleWDCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "DOGE" ) )
                {
                    handleDOGEMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "MONITOR" ) )
                {
                    handleMonitor( ref count );
                }
            }
        }

        private static void handleMonitor( ref int count )
        {
            try
            {
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    DOGEStart = double.Parse( info.Return.BalancesAvailable.DOGE );
                    xpmStart = double.Parse( info.Return.BalancesAvailable.XPM );
                    mecStart = double.Parse( info.Return.BalancesAvailable.MEC );
                    DGCStart = double.Parse( info.Return.BalancesAvailable.DGC );
                    PXCStart = double.Parse( info.Return.BalancesAvailable.PXC );
                    YACStart = double.Parse( info.Return.BalancesAvailable.YAC );
                    GLDStart = double.Parse( info.Return.BalancesAvailable.GLD );
                    WDCStart = double.Parse( info.Return.BalancesAvailable.WDC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                DOGECurrent = double.Parse( info.Return.BalancesAvailable.DOGE ) + double.Parse( info.Return.BalancesHold.DOGE );
                xpmCurrent = double.Parse( info.Return.BalancesAvailable.XPM ) + double.Parse( info.Return.BalancesHold.XPM );
                mecCurrent = double.Parse( info.Return.BalancesAvailable.MEC ) + double.Parse( info.Return.BalancesHold.MEC );
                DGCCurrent = double.Parse( info.Return.BalancesAvailable.DGC ) + double.Parse( info.Return.BalancesHold.DGC );
                PXCCurrent = double.Parse( info.Return.BalancesAvailable.PXC ) + double.Parse( info.Return.BalancesHold.PXC );
                YACCurrent = double.Parse( info.Return.BalancesAvailable.YAC ) + double.Parse( info.Return.BalancesHold.YAC );
                GLDCurrent = double.Parse( info.Return.BalancesAvailable.GLD ) + double.Parse( info.Return.BalancesHold.GLD );
                WDCCurrent = double.Parse( info.Return.BalancesAvailable.WDC ) + double.Parse( info.Return.BalancesHold.WDC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + count + " rounds -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "DOGE start: " + DOGEStart.ToString() + " current: " + DOGECurrent.ToString() + " difference: " + ( DOGECurrent - DOGEStart ).ToString() );
                    Console.WriteLine( "XPM start: " + xpmStart.ToString() + " current: " + xpmCurrent.ToString() + " difference: " + ( xpmCurrent - xpmStart ).ToString() );
                    Console.WriteLine( "MEC start: " + mecStart.ToString() + " current: " + mecCurrent.ToString() + " difference: " + ( mecCurrent - mecStart ).ToString() );
                    Console.WriteLine( "DGC start: " + DGCStart.ToString() + " current: " + DGCCurrent.ToString() + " difference: " + ( DGCCurrent - DGCStart ).ToString() );
                    Console.WriteLine( "PXC start: " + PXCStart.ToString() + " current: " + PXCCurrent.ToString() + " difference: " + ( PXCCurrent - PXCStart ).ToString() );
                    Console.WriteLine( "YAC start: " + YACStart.ToString() + " current: " + YACCurrent.ToString() + " difference: " + ( YACCurrent - YACStart ).ToString() );
                    Console.WriteLine( "GLD start: " + GLDStart.ToString() + " current: " + GLDCurrent.ToString() + " difference: " + ( GLDCurrent - GLDStart ).ToString() );
                    Console.WriteLine( "WDC start: " + WDCStart.ToString() + " current: " + WDCCurrent.ToString() + " difference: " + ( WDCCurrent - WDCStart ).ToString() );
                    Console.WriteLine();
                }
                count++;
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
            }
        }

        private static void handleDOGEMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.DOGELTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.DOGEBTC.Marketid ) )};



                Orders DOGEltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders DOGEBTCmarket = taskArray[2].Result;

                double DOGEAmount = Math.Ceiling( 0.1d / ( ( double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 500;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    DOGEStart = double.Parse( info.Return.BalancesAvailable.DOGE );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                DOGECurrent = double.Parse( info.Return.BalancesAvailable.DOGE ) + double.Parse( info.Return.BalancesHold.DOGE );
                DOGEAvailable = double.Parse( info.Return.BalancesAvailable.DOGE );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "DOGE start: " + DOGEStart.ToString() + " current: " + DOGECurrent.ToString() + " difference: " + ( DOGECurrent - DOGEStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleDOGE = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( DOGEltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar DOGE
                double countDOGE = ( countLTC / 1.002d ) / double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //DOGE naar BTC
                double countBTC = countDOGE * double.Parse( DOGEBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( DOGEBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 DOGE instaat
                if( priceNeededOfferStrat > double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.DOGEBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( myOrders[0].Result.Return.Length == 0 && countClean == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                            cleanCount = 0;
                        }
                        else if( cleanCount == 0 )
                        {
                            backUpOrders = myOrders[0].Result;
                            cancelOrdersOnMarket( markets.DOGEBTC.Marketid );
                            cleanCount++;
                        }
                        else if( cleanCount == 10 )
                        {
                            for( int x = 0; x < backUpOrders.Return.Length; x++ )
                            {
                                placeOrder( markets.DOGEBTC.Marketid, "Sell", double.Parse( backUpOrders.Return[x].Quantity ), btcPrice );
                            }
                            cleanCount++;
                        }
                        else
                        {
                            cleanCount++;
                        }

                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.DOGEBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL DOGE" );

                            //Buy new DOGE
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.DOGELTC.Marketid, "Buy", DOGEAmount, double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                            currentPrice = double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * DOGEAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && getOrdersByMarketID( markets.DOGELTC.Marketid ).Return.Length == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD DOGE RESELL AND NEW ORDERS" );
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DOGEBTC.Marketid, "Sell", DOGEAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.DOGELTC.Marketid, "Buy", DOGEAmount, currentPrice ) )};
                                if( orderArray[2].Result.Success.Equals( "1" ) )
                                {
                                    currentOrder = orderArray[2].Result;
                                }
                                else
                                {
                                    currentOrder = null;
                                }
                            }
                            else
                            {
                                Console.WriteLine( "SOLD DOGE RESELL AND MAX REACHED" );
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DOGEBTC.Marketid, "Sell", DOGEAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( DOGEltcmarket.Return.Buyorders[0].Quantity ) > DOGEAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.DOGELTC.Marketid );
                            currentOrder = placeOrder( markets.DOGELTC.Marketid, "Buy", DOGEAmount, double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( DOGEltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * DOGEAmount;


                        }
                        if( DOGEAvailable - DOGEStart > DOGEAmount )
                        {
                            Console.WriteLine( "Selling partial bought DOGE" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DOGEBTC.Marketid, "Sell", DOGEAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        if( countClean == 20 && false )
                        {
                            Console.WriteLine( "Doing cleanup!" );

                            if( ( currentPrice - double.Parse( DOGEltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                            {
                                Console.WriteLine( "Found gap between 1 and 2 price" );
                                cancelOrdersOnMarket( markets.DOGELTC.Marketid );
                                currentOrder = placeOrder( markets.DOGELTC.Marketid, "Buy", DOGEAmount, double.Parse( DOGEltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                                if( !currentOrder.Success.Equals( "1" ) )
                                {
                                    currentOrder = null;
                                }
                                currentPrice = double.Parse( DOGEltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                                ltcSpend = ( double.Parse( DOGEltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * DOGEAmount;
                            }
                            countClean = 0;
                        }
                        btcPrice = double.Parse( DOGEBTCmarket.Return.Buyorders[0].Buyprice );
                        ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                    }
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.DOGELTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DOGEBTC.Marketid, "Sell", DOGEAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new DOGE
                        Console.WriteLine( "SOLD DOGE AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.DOGELTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling DOGE orders!" );
                cancelOrdersOnMarket( markets.DOGELTC.Marketid );
                currentOrder = null;
            }
        }


        private static void handleWDCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.WDCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.WDCBTC.Marketid ) )};



                Orders WDCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders WDCBTCmarket = taskArray[2].Result;

                double WDCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 1;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    WDCStart = double.Parse( info.Return.BalancesAvailable.WDC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                WDCCurrent = double.Parse( info.Return.BalancesAvailable.WDC ) + double.Parse( info.Return.BalancesHold.WDC );
                WDCAvailable = double.Parse( info.Return.BalancesAvailable.WDC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "WDC start: " + WDCStart.ToString() + " current: " + WDCCurrent.ToString() + " difference: " + ( WDCCurrent - WDCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleWDC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( WDCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar WDC
                double countWDC = ( countLTC / 1.002d ) / double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //WDC naar BTC
                double countBTC = countWDC * double.Parse( WDCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( WDCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 WDC instaat
                if( priceNeededOfferStrat > double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    int currentSellToBTCOrders = getOrdersByMarketID( markets.WDCBTC.Marketid ).Return.Length;
                    if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                    {
                        Console.WriteLine( " INITIAL WDC" );

                        //Buy new WDC
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                        currentPrice = double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * WDCAmount;
                        if( orderArray[0].Result.Success.Equals( "1" ) )
                        {
                            currentOrder = orderArray[0].Result;
                        }

                    }
                    else if( currentOrder != null && getOrdersByMarketID( markets.WDCLTC.Marketid ).Return.Length == 0 )
                    {
                        transactionDone++;
                        countClean = 0;

                        if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( "SOLD WDC RESELL AND NEW ORDERS" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, currentPrice ) )};
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine( "SOLD WDC RESELL AND MAX REACHED" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            currentOrder = null;
                        }
                    }

                    if( currentOrder != null && currentPrice < double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( WDCltcmarket.Return.Buyorders[0].Quantity ) > WDCAmount ) )
                    {

                        Console.WriteLine( "Found higher price" );
                        cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                        currentOrder = placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                        if( !currentOrder.Success.Equals( "1" ) )
                        {
                            currentOrder = null;
                        }
                        currentPrice = double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * WDCAmount;


                    }
                    if( WDCAvailable - WDCStart > WDCAmount )
                    {
                        Console.WriteLine( "Selling partial bought WDC" );
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                            currentOrder = placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * WDCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( WDCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.WDCLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new WDC
                        Console.WriteLine( "SOLD WDC AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling WDC orders!" );
                cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleGLDMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.GLDLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.GLDBTC.Marketid ) )};



                Orders GLDltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders GLDBTCmarket = taskArray[2].Result;

                double GLDAmount = Math.Ceiling( 0.1d / ( ( double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 1;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    GLDStart = double.Parse( info.Return.BalancesAvailable.GLD );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                GLDCurrent = double.Parse( info.Return.BalancesAvailable.GLD ) + double.Parse( info.Return.BalancesHold.GLD );
                GLDAvailable = double.Parse( info.Return.BalancesAvailable.GLD );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "GLD start: " + GLDStart.ToString() + " current: " + GLDCurrent.ToString() + " difference: " + ( GLDCurrent - GLDStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleGLD = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( GLDltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar GLD
                double countGLD = ( countLTC / 1.002d ) / double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //GLD naar BTC
                double countBTC = countGLD * double.Parse( GLDBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( GLDBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 GLD instaat
                if( priceNeededOfferStrat > double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    int currentSellOrdersToBTC = getOrdersByMarketID( markets.GLDBTC.Marketid ).Return.Length;
                    if( currentOrder == null && currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                    {
                        Console.WriteLine( " INITIAL GLD" );

                        //Buy new GLD
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                        currentPrice = double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * GLDAmount;
                        if( orderArray[0].Result.Success.Equals( "1" ) )
                        {
                            currentOrder = orderArray[0].Result;
                        }

                    }
                    else if( currentOrder != null && getOrdersByMarketID( markets.GLDLTC.Marketid ).Return.Length == 0 )
                    {
                        transactionDone++;
                        countClean = 0;

                        if( currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( "SOLD GLD RESELL AND NEW ORDERS" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, currentPrice ) )};
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine( "SOLD GLD RESELL AND MAX REACHED" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            currentOrder = null;
                        }
                    }

                    if( currentOrder != null && currentPrice < double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( GLDltcmarket.Return.Buyorders[0].Quantity ) > GLDAmount ) )
                    {

                        Console.WriteLine( "Found higher price" );
                        cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                        currentOrder = placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                        if( !currentOrder.Success.Equals( "1" ) )
                        {
                            currentOrder = null;
                        }
                        currentPrice = double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * GLDAmount;


                    }
                    if( GLDAvailable - GLDStart > GLDAmount )
                    {
                        Console.WriteLine( "Selling partial bought GLD" );
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                            currentOrder = placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * GLDAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( GLDBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.GLDLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new GLD
                        Console.WriteLine( "SOLD GLD AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling GLD orders!" );
                cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleYACMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.YACLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.YACBTC.Marketid ) )};



                Orders YACltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders YACBTCmarket = taskArray[2].Result;

                double YACAmount = Math.Ceiling( 0.1d / ( ( double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 1;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    YACStart = double.Parse( info.Return.BalancesAvailable.YAC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                YACCurrent = double.Parse( info.Return.BalancesAvailable.YAC ) + double.Parse( info.Return.BalancesHold.YAC );
                YACAvailable = double.Parse( info.Return.BalancesAvailable.YAC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "YAC start: " + YACStart.ToString() + " current: " + YACCurrent.ToString() + " difference: " + ( YACCurrent - YACStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleYAC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( YACltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar YAC
                double countYAC = ( countLTC / 1.002d ) / double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //YAC naar BTC
                double countBTC = countYAC * double.Parse( YACBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( YACBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 YAC instaat
                if( priceNeededOfferStrat > double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    int currentSellOrdersToBTC = getOrdersByMarketID( markets.YACBTC.Marketid ).Return.Length;
                    if( currentOrder == null && currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                    {
                        Console.WriteLine( " INITIAL YAC" );

                        //Buy new YAC
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.YACLTC.Marketid, "Buy", YACAmount, double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                        currentPrice = double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * YACAmount;
                        if( orderArray[0].Result.Success.Equals( "1" ) )
                        {
                            currentOrder = orderArray[0].Result;
                        }

                    }
                    else if( currentOrder != null && getOrdersByMarketID( markets.YACLTC.Marketid ).Return.Length == 0 )
                    {
                        transactionDone++;
                        countClean = 0;

                        if( currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( "SOLD YAC RESELL AND NEW ORDERS" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.YACBTC.Marketid, "Sell", YACAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.YACLTC.Marketid, "Buy", YACAmount, currentPrice ) )};
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine( "SOLD YAC RESELL AND MAX REACHED" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.YACBTC.Marketid, "Sell", YACAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            currentOrder = null;
                        }
                    }

                    if( currentOrder != null && currentPrice < double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( YACltcmarket.Return.Buyorders[0].Quantity ) > YACAmount ) )
                    {

                        Console.WriteLine( "Found higher price" );
                        cancelOrdersOnMarket( markets.YACLTC.Marketid );
                        currentOrder = placeOrder( markets.YACLTC.Marketid, "Buy", YACAmount, double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                        if( !currentOrder.Success.Equals( "1" ) )
                        {
                            currentOrder = null;
                        }
                        currentPrice = double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( YACltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * YACAmount;


                    }
                    if( YACAvailable - YACStart > YACAmount )
                    {
                        Console.WriteLine( "Selling partial bought YAC" );
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.YACBTC.Marketid, "Sell", YACAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( YACltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.YACLTC.Marketid );
                            currentOrder = placeOrder( markets.YACLTC.Marketid, "Buy", YACAmount, double.Parse( YACltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( YACltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( YACltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * YACAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( YACBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.YACLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.YACBTC.Marketid, "Sell", YACAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new YAC
                        Console.WriteLine( "SOLD YAC AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.YACLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling YAC orders!" );
                cancelOrdersOnMarket( markets.YACLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handlePXCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.PXCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.PXCBTC.Marketid ) )};



                Orders PXCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders PXCBTCmarket = taskArray[2].Result;

                double PXCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 1;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    PXCStart = double.Parse( info.Return.BalancesAvailable.PXC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                PXCCurrent = double.Parse( info.Return.BalancesAvailable.PXC ) + double.Parse( info.Return.BalancesHold.PXC );
                PXCAvailable = double.Parse( info.Return.BalancesAvailable.PXC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "PXC start: " + PXCStart.ToString() + " current: " + PXCCurrent.ToString() + " difference: " + ( PXCCurrent - PXCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possiblePXC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( PXCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar PXC
                double countPXC = ( countLTC / 1.002d ) / double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //PXC naar BTC
                double countBTC = countPXC * double.Parse( PXCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( PXCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 PXC instaat
                if( priceNeededOfferStrat > double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    int currentSellOrdersToBTC = getOrdersByMarketID( markets.PXCBTC.Marketid ).Return.Length;
                    if( currentOrder == null && currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                    {
                        Console.WriteLine( " INITIAL PXC" );

                        //Buy new PXC
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                        currentPrice = double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * PXCAmount;
                        if( orderArray[0].Result.Success.Equals( "1" ) )
                        {
                            currentOrder = orderArray[0].Result;
                        }

                    }
                    else if( currentOrder != null && getOrdersByMarketID( markets.PXCLTC.Marketid ).Return.Length == 0 )
                    {
                        transactionDone++;
                        countClean = 0;

                        if( currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( "SOLD PXC RESELL AND NEW ORDERS" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, currentPrice ) )};
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine( "SOLD PXC RESELL AND MAX REACHED" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            currentOrder = null;
                        }
                    }

                    if( currentOrder != null && currentPrice < double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( PXCltcmarket.Return.Buyorders[0].Quantity ) > PXCAmount ) )
                    {

                        Console.WriteLine( "Found higher price" );
                        cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                        currentOrder = placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                        if( !currentOrder.Success.Equals( "1" ) )
                        {
                            currentOrder = null;
                        }
                        currentPrice = double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * PXCAmount;


                    }
                    if( PXCAvailable - PXCStart > PXCAmount )
                    {
                        Console.WriteLine( "Selling partial bought PXC" );
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                            currentOrder = placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * PXCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( PXCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.PXCLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new PXC
                        Console.WriteLine( "SOLD PXC AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling PXC orders!" );
                cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleDGCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.DGCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.DGCBTC.Marketid ) )};



                Orders DGCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders DGCBTCmarket = taskArray[2].Result;

                double DGCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 1;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    DGCStart = double.Parse( info.Return.BalancesAvailable.DGC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                DGCCurrent = double.Parse( info.Return.BalancesAvailable.DGC ) + double.Parse( info.Return.BalancesHold.DGC );
                DGCAvailable = double.Parse( info.Return.BalancesAvailable.DGC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "DGC start: " + DGCStart.ToString() + " current: " + DGCCurrent.ToString() + " difference: " + ( DGCCurrent - DGCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleDGC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( DGCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar DGC
                double countDGC = ( countLTC / 1.002d ) / double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //DGC naar BTC
                double countBTC = countDGC * double.Parse( DGCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( DGCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 DGC instaat
                if( priceNeededOfferStrat > double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    int currentSellOrdersToBTC = getOrdersByMarketID( markets.DGCBTC.Marketid ).Return.Length;
                    if( currentOrder == null && currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                    {
                        Console.WriteLine( " INITIAL DGC" );

                        //Buy new DGC
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                        currentPrice = double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * DGCAmount;
                        if( orderArray[0].Result.Success.Equals( "1" ) )
                        {
                            currentOrder = orderArray[0].Result;
                        }

                    }
                    else if( currentOrder != null && getOrdersByMarketID( markets.DGCLTC.Marketid ).Return.Length == 0 )
                    {
                        transactionDone++;
                        countClean = 0;

                        if( currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( "SOLD DGC RESELL AND NEW ORDERS" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, currentPrice ) )};
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine( "SOLD DGC RESELL AND MAX REACHED" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            currentOrder = null;
                        }
                    }

                    if( currentOrder != null && currentPrice < double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( DGCltcmarket.Return.Buyorders[0].Quantity ) > DGCAmount ) )
                    {
                        Console.WriteLine( "Found higher price" );
                        cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                        currentOrder = placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                        if( !currentOrder.Success.Equals( "1" ) )
                        {
                            currentOrder = null;
                        }
                        currentPrice = double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * DGCAmount;

                    }
                    if( DGCAvailable - DGCStart > DGCAmount )
                    {
                        Console.WriteLine( "Selling partial bought DGC" );
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                            currentOrder = placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * DGCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( DGCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.DGCLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new DGC
                        Console.WriteLine( "SOLD DGC AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling DGC orders!" );
                cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleMECMarket( ref int count, ref int countClean, Markets markets )
        {

            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.MECLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.MECBTC.Marketid ) )};



                Orders mecltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders MECBTCmarket = taskArray[2].Result;

                double mecAmount = Math.Ceiling( 0.1d / ( ( double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d ) ) + 1;



                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    mecStart = double.Parse( info.Return.BalancesAvailable.MEC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                mecCurrent = double.Parse( info.Return.BalancesAvailable.MEC ) + double.Parse( info.Return.BalancesHold.MEC );
                mecAvailable = double.Parse( info.Return.BalancesAvailable.MEC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "MEC start: " + mecStart.ToString() + " current: " + mecCurrent.ToString() + " difference: " + ( mecCurrent - mecStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleMEC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( mecltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar MEC
                double countMEC = ( countLTC / 1.002d ) / double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //MEC naar BTC
                double countBTC = countMEC * double.Parse( MECBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( MECBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 MEC instaat
                if( priceNeededOfferStrat > double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    int currentSellOrdersToBTC = getOrdersByMarketID( markets.DGCBTC.Marketid ).Return.Length;
                    if( currentOrder == null && currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                    {
                        Console.WriteLine( " INITIAL MEC" );

                        //Buy new MEC
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.MECLTC.Marketid, "Buy", mecAmount, double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                        currentPrice = double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * mecAmount;
                        if( orderArray[0].Result.Success.Equals( "1" ) )
                        {
                            currentOrder = orderArray[0].Result;
                        }

                    }
                    else if( currentOrder != null && getOrdersByMarketID( markets.MECLTC.Marketid ).Return.Length == 0 )
                    {
                        transactionDone++;
                        countClean = 0;

                        if( currentSellOrdersToBTC < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( "SOLD MEC RESELL AND NEW ORDERS" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", mecAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.MECLTC.Marketid, "Buy", mecAmount, currentPrice ) )};
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }
                        else
                        {
                            Console.WriteLine( "SOLD MEC RESELL AND MAX REACHED" );
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", mecAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            currentOrder = null;
                        }
                    }

                    if( currentOrder != null && currentPrice < double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( mecltcmarket.Return.Buyorders[0].Quantity ) > mecAmount ) )
                    {

                        Console.WriteLine( "Found higher price" );
                        cancelOrdersOnMarket( markets.MECLTC.Marketid );
                        currentOrder = placeOrder( markets.MECLTC.Marketid, "Buy", mecAmount, double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                        if( !currentOrder.Success.Equals( "1" ) )
                        {
                            currentOrder = null;
                        }
                        currentPrice = double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                        ltcSpend = ( double.Parse( mecltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d * mecAmount;


                    }
                    if( mecAvailable - mecStart > mecAmount )
                    {
                        Console.WriteLine( "Selling partial bought MEC" );
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", mecAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( mecltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.MECLTC.Marketid );
                            currentOrder = placeOrder( markets.MECLTC.Marketid, "Buy", mecAmount, double.Parse( mecltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( mecltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( mecltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d * mecAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( MECBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.MECLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", mecAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new MEC
                        Console.WriteLine( "SOLD MEC AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.MECLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling MEC orders!" );
                cancelOrdersOnMarket( markets.MECLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleXPMMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.XPMLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.XPMBTC.Marketid ) )};



                Orders xpmltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders XPMBTCmarket = taskArray[2].Result;


                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    xpmStart = double.Parse( info.Return.BalancesAvailable.XPM );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                xpmCurrent = double.Parse( info.Return.BalancesAvailable.XPM ) + double.Parse( info.Return.BalancesHold.XPM );
                xpmAvailable = double.Parse( info.Return.BalancesAvailable.XPM );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "XPM start: " + xpmStart.ToString() + " current: " + xpmStart.ToString() + " difference: " + ( xpmCurrent - xpmStart ).ToString() );
                }

                count++;

                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleXPM = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( xpmltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar XPM
                double countXPM = ( countLTC / 1.002d ) / double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;

                //XPM naar BTC
                double countBTC = countXPM * double.Parse( XPMBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( XPMBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 xpm instaat
                if( priceNeededOfferStrat > double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.XPMLTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( myOrders[0].Result.Return.Length == 0 && countClean == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            backUpOrders = myOrders[0].Result;
                            cancelOrdersOnMarket( markets.XPMBTC.Marketid );
                        }
                        if( cleanCount == 10 )
                        {
                            for( int x = 0; x < backUpOrders.Return.Length; x++ )
                            {
                                placeOrder( markets.XPMBTC.Marketid, "Sell", double.Parse( backUpOrders.Return[x].Quantity ), btcPrice );
                            }
                        }
                        cleanCount++;
                    }
                    else
                    {
                        if( currentOrder == null )
                        {
                            Console.WriteLine( " INITIAL XPM" );

                            //Buy new XPM
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.XPMLTC.Marketid, "Buy", 1, double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                            currentPrice = double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && myOrders[0].Result.Return.Length == 0 )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", 1, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.XPMLTC.Marketid, "Buy", 1, currentPrice ) )};

                            //Buy new XPM
                            transactionDone++;
                            countClean = 0;
                            Console.WriteLine( "SOLD XPM RESELL AND NEW ORDERS" );
                            if( orderArray[2].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[2].Result;
                            }
                            else
                            {
                                currentOrder = null;
                            }
                        }

                        if( ( currentOrder != null && currentPrice < double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) ) || ( currentOrder != null && currentPrice == double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( xpmltcmarket.Return.Buyorders[0].Quantity ) > 1d ) )
                        {
                            Console.WriteLine( "Found higher price" );


                            cancelOrdersOnMarket( markets.XPMLTC.Marketid );

                            currentOrder = placeOrder( markets.XPMLTC.Marketid, "Buy", 1, double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d );
                            currentPrice = double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d;
                            ltcSpend = ( double.Parse( xpmltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) * 1.002d;
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                        }
                        if( xpmAvailable - xpmStart > 1 )
                        {
                            Console.WriteLine( "Selling partial bought XPM" );
                            Task<OrderResponse>[] orderArray2 = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", 1, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        if( countClean == 20 && false )
                        {
                            Console.WriteLine( "Doing cleanup!" );

                            if( ( currentPrice - double.Parse( xpmltcmarket.Return.Buyorders[1].Buyprice ) ) > 0.00000002d )
                            {
                                Console.WriteLine( "Found gap between 1 and 2 price" );
                                cancelOrdersOnMarket( markets.XPMLTC.Marketid );
                                currentOrder = placeOrder( markets.XPMLTC.Marketid, "Buy", 1, double.Parse( xpmltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d );
                                if( !currentOrder.Success.Equals( "1" ) )
                                {
                                    currentOrder = null;
                                }
                                currentPrice = double.Parse( xpmltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d;
                                ltcSpend = ( double.Parse( xpmltcmarket.Return.Buyorders[1].Buyprice ) + 0.00000002d ) * 1.002d;
                            }
                            countClean = 0;
                        }
                        btcPrice = double.Parse( XPMBTCmarket.Return.Buyorders[0].Buyprice );
                        ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                    }
                }
                else if( currentOrder != null )
                {
                    if( getOrdersByMarketID( markets.XPMLTC.Marketid ).Return.Length == 0 )
                    {
                        Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", 1, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        countClean = 0;
                        transactionDone++;
                        //Buy new XPM
                        Console.WriteLine( "SOLD XPM AND NEW ORDERS BEFORE CANCEL" );
                    }
                    Console.WriteLine( "Cancel orders!! Not profitable!" );
                    cancelOrdersOnMarket( markets.XPMLTC.Marketid );
                    currentOrder = null;
                    countClean = 0;
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling XPM orders!" );
                cancelOrdersOnMarket( markets.XPMLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void cancelOrdersOnMarket( string marketID )
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData =
                "method=cancelmarketorders&marketid=" + marketID +
                "&nonce=" + nonce;

            byte[] data = encoding.GetBytes( postData );

            HMACSHA512 crypt = new HMACSHA512( encoding.GetBytes( sKey ) );
            string signedData = ByteToString( crypt.ComputeHash( data ) ).ToLower();

            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/x-www-form-urlencoded";
            httpWReq.ContentLength = data.Length;
            httpWReq.Headers.Add( "Key", key );
            httpWReq.Headers.Add( "Sign", signedData );
            httpWReq.Timeout = 10000;

            HttpWebResponse response2;
            try
            {

                using( Stream stream = httpWReq.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                }

                response2 = (HttpWebResponse)httpWReq.GetResponse();
                string responseString = new StreamReader( response2.GetResponseStream() ).ReadToEnd();
            }
            catch( Exception e )
            {
                Console.WriteLine( "Cancelling failed!" );
                cancelOrdersOnMarket( marketID );
            }
        }

        private static MyTrades getTradesByMarketID( string marketID, int amount )
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData =
                "method=mytrades&marketid=" + marketID +
                "&limit=" + amount +
                "&nonce=" + nonce;

            byte[] data = encoding.GetBytes( postData );

            HMACSHA512 crypt = new HMACSHA512( encoding.GetBytes( sKey ) );
            string signedData = ByteToString( crypt.ComputeHash( data ) ).ToLower();

            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/x-www-form-urlencoded";
            httpWReq.ContentLength = data.Length;
            httpWReq.Headers.Add( "Key", key );
            httpWReq.Headers.Add( "Sign", signedData );
            httpWReq.Timeout = 10000;
            HttpWebResponse response2;
            string responseString;
            try
            {

                using( Stream stream = httpWReq.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                }

                response2 = (HttpWebResponse)httpWReq.GetResponse();
                responseString = new StreamReader( response2.GetResponseStream() ).ReadToEnd();
            }
            catch( Exception e )
            {
                Console.WriteLine( "Getting trades failed" );
                return getTradesByMarketID( marketID, amount );
            }
            MyTrades orders = JsonConvert.DeserializeObject<MyTrades>( responseString );
            return orders;
        }

        private static MyOrders getOrdersByMarketID( string marketID )
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData =
                "method=myorders&marketid=" + marketID +
                "&nonce=" + nonce;

            byte[] data = encoding.GetBytes( postData );

            HMACSHA512 crypt = new HMACSHA512( encoding.GetBytes( sKey ) );
            string signedData = ByteToString( crypt.ComputeHash( data ) ).ToLower();

            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/x-www-form-urlencoded";
            httpWReq.ContentLength = data.Length;
            httpWReq.Headers.Add( "Key", key );
            httpWReq.Headers.Add( "Sign", signedData );
            httpWReq.Timeout = 10000;
            HttpWebResponse response2;
            string responseString;
            try
            {

                using( Stream stream = httpWReq.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                }


                response2 = (HttpWebResponse)httpWReq.GetResponse();
                responseString = new StreamReader( response2.GetResponseStream() ).ReadToEnd();
            }
            catch( Exception e )
            {

                Console.WriteLine( "Getting orders failed" );
                return getOrdersByMarketID( marketID );
            }

            MyOrders orders = JsonConvert.DeserializeObject<MyOrders>( responseString );
            return orders;
        }
        private static OrderResponse placeOrder( string marketId, string orderType, double quantity, double price )
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData =
                "method=createorder&marketid=" + marketId +
                "&ordertype=" + orderType +
                "&quantity=" + quantity +
                "&price=" + Convert.ToDouble( price ) +
                "&nonce=" + nonce;

            byte[] data = encoding.GetBytes( postData );

            HMACSHA512 crypt = new HMACSHA512( encoding.GetBytes( sKey ) );
            string signedData = ByteToString( crypt.ComputeHash( data ) ).ToLower();

            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/x-www-form-urlencoded";
            httpWReq.ContentLength = data.Length;
            httpWReq.Headers.Add( "Key", key );
            httpWReq.Headers.Add( "Sign", signedData );
            string responseString = "";
            HttpWebResponse response2;
            try
            {

                using( Stream stream = httpWReq.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                }
                response2 = (HttpWebResponse)httpWReq.GetResponse();
                responseString = new StreamReader( response2.GetResponseStream() ).ReadToEnd();
            }
            catch( Exception e )
            {
                Console.WriteLine( "Place order failed" );
            }

            OrderResponse orders = JsonConvert.DeserializeObject<OrderResponse>( responseString );
            return orders;
        }

        private static Balances getInfo()
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData =
                "method=getinfo&marketid=" +
                "&nonce=" + nonce;

            byte[] data = encoding.GetBytes( postData );

            HMACSHA512 crypt = new HMACSHA512( encoding.GetBytes( sKey ) );
            string signedData = ByteToString( crypt.ComputeHash( data ) ).ToLower();

            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/x-www-form-urlencoded";
            httpWReq.ContentLength = data.Length;
            httpWReq.Headers.Add( "Key", key );
            httpWReq.Headers.Add( "Sign", signedData );
            httpWReq.Timeout = 10000;
            HttpWebResponse response2;
            string responseString;
            try
            {

                using( Stream stream = httpWReq.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                }

                response2 = (HttpWebResponse)httpWReq.GetResponse();
                responseString = new StreamReader( response2.GetResponseStream() ).ReadToEnd();
            }
            catch( Exception e )
            {
                Console.WriteLine( "Get info failed" );
                return getInfo();
            }
            Balances orders = JsonConvert.DeserializeObject<Balances>( responseString );
            return orders;
        }

        private static Orders getAllOrdersByMarketID( string id )
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData = "method=marketorders&marketid=" + id + "&nonce=" + nonce;

            byte[] data = encoding.GetBytes( postData );

            HMACSHA512 crypt = new HMACSHA512( encoding.GetBytes( sKey ) );
            string signedData = ByteToString( crypt.ComputeHash( data ) ).ToLower();

            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/x-www-form-urlencoded";
            httpWReq.ContentLength = data.Length;
            httpWReq.Headers.Add( "Key", key );
            httpWReq.Headers.Add( "Sign", signedData );
            httpWReq.Timeout = 10000;
            HttpWebResponse response2;
            string responseString;
            try
            {

                using( Stream stream = httpWReq.GetRequestStream() )
                {
                    stream.Write( data, 0, data.Length );
                }

                response2 = (HttpWebResponse)httpWReq.GetResponse();
                responseString = new StreamReader( response2.GetResponseStream() ).ReadToEnd();
            }
            catch( Exception e )
            {

                Console.WriteLine( "Get all orders failed" );

                return getAllOrdersByMarketID( id );
            }

            Orders orders = JsonConvert.DeserializeObject<Orders>( responseString );
            return orders;
        }

        public static OrderResponse currentOrder
        {
            get;
            set;
        }

        public static double currentPrice
        {
            get;
            set;
        }

        public static double ltcSpend
        {
            get;
            set;
        }

        public static double btcStart
        {
            get;
            set;
        }

        public static double ltcStart
        {
            get;
            set;
        }

        public static double mecStart
        {
            get;
            set;
        }

        public static double btcCurrent
        {
            get;
            set;
        }

        public static double ltcCurrent
        {
            get;
            set;
        }

        public static double mecCurrent
        {
            get;
            set;
        }

        public static int transactionDone
        {
            get;
            set;
        }

        public static double btcPrice
        {
            get;
            set;
        }

        public static double ltcPrice
        {
            get;
            set;
        }

        public static string marketString
        {
            get;
            set;
        }

        public static double xpmStart
        {
            get;
            set;
        }

        public static double xpmCurrent
        {
            get;
            set;
        }

        public static double xpmAvailable
        {
            get;
            set;
        }

        public static double mecAvailable
        {
            get;
            set;
        }

        public static double minimumBTCEarnings
        {
            get;
            set;
        }

        public static double DGCStart
        {
            get;
            set;
        }

        public static double DGCCurrent
        {
            get;
            set;
        }

        public static double DGCAvailable
        {
            get;
            set;
        }

        public static double YACStart
        {
            get;
            set;
        }

        public static double YACCurrent
        {
            get;
            set;
        }

        public static double YACAvailable
        {
            get;
            set;
        }

        public static double WDCStart
        {
            get;
            set;
        }

        public static double WDCCurrent
        {
            get;
            set;
        }

        public static double WDCAvailable
        {
            get;
            set;
        }

        public static double PXCStart
        {
            get;
            set;
        }

        public static double PXCCurrent
        {
            get;
            set;
        }

        public static double PXCAvailable
        {
            get;
            set;
        }

        public static double GLDStart
        {
            get;
            set;
        }

        public static double GLDCurrent
        {
            get;
            set;
        }

        public static double GLDAvailable
        {
            get;
            set;
        }

        public static double DOGEStart
        {
            get;
            set;
        }

        public static double DOGECurrent
        {
            get;
            set;
        }

        public static double DOGEAvailable
        {
            get;
            set;
        }

        public static MyOrders backUpOrders
        {
            get;
            set;
        }
    }
}
