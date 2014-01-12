using System;
using System.Configuration;
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
        static string key = ConfigurationManager.AppSettings["pubKey"];
        static string sKey = ConfigurationManager.AppSettings["privKey"];

        static double sellFee = 0.997f;
        static double buyFee = 0.998f;
        static bool emergencyStop = false;
        private const short roundsForBalanceInfo = 100;
        private static int maxSimuOrdersPerCoin = 3;
        private static double cleanTime = 1;
        private const int roundTimeOutWhenCleaning = 50;
        private static int cleanCount = 0;
        private static DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
        private static double maxLTC = 3.5;
        private static double raiseJump = 0.00000100;

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
                if( marketString.Equals( "XPMLTC" ) )
                {
                    handleXPMLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "MECLTC" ) )
                {
                    handleMECLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "DGCLTC" ) )
                {
                    handleDGCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "PXCLTC" ) )
                {
                    handlePXCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "GLDLTC" ) )
                {
                    handleGLDLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "WDCLTC" ) )
                {
                    handleWDCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "ANCLTC" ) )
                {
                    handleANCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "CGBLTC" ) )
                {
                    handleCGBLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "CNCLTC" ) )
                {
                    handleCNCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "PPCLTC" ) )
                {
                    handlePPCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "SBCLTC" ) )
                {
                    handleSBCLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "NETLTC" ) )
                {
                    handleNETLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "QRKLTC" ) )
                {
                    handleQRKLTCMarket( ref count, ref countClean, markets );
                }
                else if( marketString.Equals( "ZETLTC" ) )
                {
                    handleZETLTCMarket( ref count, ref countClean, markets );
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
                    XPMStart = double.Parse( info.Return.BalancesAvailable.XPM );
                    MECStart = double.Parse( info.Return.BalancesAvailable.MEC );
                    DGCStart = double.Parse( info.Return.BalancesAvailable.DGC );
                    PXCStart = double.Parse( info.Return.BalancesAvailable.PXC );
                    GLDStart = double.Parse( info.Return.BalancesAvailable.GLD );
                    WDCStart = double.Parse( info.Return.BalancesAvailable.WDC );
                    ANCStart = double.Parse( info.Return.BalancesAvailable.ANC );
                    CGBStart = double.Parse( info.Return.BalancesAvailable.CGB );
                    CNCStart = double.Parse( info.Return.BalancesAvailable.CNC );
                    PPCStart = double.Parse( info.Return.BalancesAvailable.PPC );
                    SBCStart = double.Parse( info.Return.BalancesAvailable.SBC );
                    NETStart = double.Parse( info.Return.BalancesAvailable.NET );
                    QRKStart = double.Parse( info.Return.BalancesAvailable.QRK );
                    ZETStart = double.Parse( info.Return.BalancesAvailable.ZET );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                XPMCurrent = double.Parse( info.Return.BalancesAvailable.XPM ) + double.Parse( info.Return.BalancesHold.XPM );
                MECCurrent = double.Parse( info.Return.BalancesAvailable.MEC ) + double.Parse( info.Return.BalancesHold.MEC );
                DGCCurrent = double.Parse( info.Return.BalancesAvailable.DGC ) + double.Parse( info.Return.BalancesHold.DGC );
                PXCCurrent = double.Parse( info.Return.BalancesAvailable.PXC ) + double.Parse( info.Return.BalancesHold.PXC );
                GLDCurrent = double.Parse( info.Return.BalancesAvailable.GLD ) + double.Parse( info.Return.BalancesHold.GLD );
                WDCCurrent = double.Parse( info.Return.BalancesAvailable.WDC ) + double.Parse( info.Return.BalancesHold.WDC );
                ANCCurrent = double.Parse( info.Return.BalancesAvailable.ANC ) + double.Parse( info.Return.BalancesHold.ANC );
                CGBCurrent = double.Parse( info.Return.BalancesAvailable.CGB ) + double.Parse( info.Return.BalancesHold.CGB );
                CNCCurrent = double.Parse( info.Return.BalancesAvailable.CNC ) + double.Parse( info.Return.BalancesHold.CNC );
                PPCCurrent = double.Parse( info.Return.BalancesAvailable.PPC ) + double.Parse( info.Return.BalancesHold.PPC );
                SBCCurrent = double.Parse( info.Return.BalancesAvailable.SBC ) + double.Parse( info.Return.BalancesHold.SBC );
                NETCurrent = double.Parse( info.Return.BalancesAvailable.NET ) + double.Parse( info.Return.BalancesHold.NET );
                QRKCurrent = double.Parse( info.Return.BalancesAvailable.QRK ) + double.Parse( info.Return.BalancesHold.QRK );
                ZETCurrent = double.Parse( info.Return.BalancesAvailable.ZET ) + double.Parse( info.Return.BalancesHold.ZET );

                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + count + " rounds -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "XPM start: " + XPMStart.ToString() + " current: " + XPMCurrent.ToString() + " difference: " + ( XPMCurrent - XPMStart ).ToString() );
                    Console.WriteLine( "MEC start: " + MECStart.ToString() + " current: " + MECCurrent.ToString() + " difference: " + ( MECCurrent - MECStart ).ToString() );
                    Console.WriteLine( "DGC start: " + DGCStart.ToString() + " current: " + DGCCurrent.ToString() + " difference: " + ( DGCCurrent - DGCStart ).ToString() );
                    Console.WriteLine( "PXC start: " + PXCStart.ToString() + " current: " + PXCCurrent.ToString() + " difference: " + ( PXCCurrent - PXCStart ).ToString() );
                    Console.WriteLine( "GLD start: " + GLDStart.ToString() + " current: " + GLDCurrent.ToString() + " difference: " + ( GLDCurrent - GLDStart ).ToString() );
                    Console.WriteLine( "WDC start: " + WDCStart.ToString() + " current: " + WDCCurrent.ToString() + " difference: " + ( WDCCurrent - WDCStart ).ToString() );
                    Console.WriteLine( "ANC start: " + ANCStart.ToString() + " current: " + ANCCurrent.ToString() + " difference: " + ( ANCCurrent - ANCStart ).ToString() );
                    Console.WriteLine( "CGB start: " + CGBStart.ToString() + " current: " + CGBCurrent.ToString() + " difference: " + ( CGBCurrent - CGBStart ).ToString() );
                    Console.WriteLine( "CNC start: " + CNCStart.ToString() + " current: " + CNCCurrent.ToString() + " difference: " + ( CNCCurrent - CNCStart ).ToString() );
                    Console.WriteLine( "PPC start: " + PPCStart.ToString() + " current: " + PPCCurrent.ToString() + " difference: " + ( PPCCurrent - PPCStart ).ToString() );
                    Console.WriteLine( "SBC start: " + SBCStart.ToString() + " current: " + SBCCurrent.ToString() + " difference: " + ( SBCCurrent - SBCStart ).ToString() );
                    Console.WriteLine( "NET start: " + NETStart.ToString() + " current: " + NETCurrent.ToString() + " difference: " + ( NETCurrent - NETStart ).ToString() );
                    Console.WriteLine( "QRK start: " + QRKStart.ToString() + " current: " + QRKCurrent.ToString() + " difference: " + ( QRKCurrent - QRKStart ).ToString() );
                    Console.WriteLine( "ZET start: " + ZETStart.ToString() + " current: " + ZETCurrent.ToString() + " difference: " + ( ZETCurrent - ZETStart ).ToString() );
                    Console.WriteLine();
                }
                count++;
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
            }
        }
        #region BUY WITH LTC markets


        private static void handleZETLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.ZETLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.ZETBTC.Marketid ) )};



                Orders ZETltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders ZETBTCmarket = taskArray[2].Result;

                double ZETAmount = Math.Ceiling( 0.1d / ( ( double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    ZETStart = double.Parse( info.Return.BalancesAvailable.ZET );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                ZETCurrent = double.Parse( info.Return.BalancesAvailable.ZET ) + double.Parse( info.Return.BalancesHold.ZET );
                ZETAvailable = double.Parse( info.Return.BalancesAvailable.ZET );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "ZET start: " + ZETStart.ToString() + " current: " + ZETCurrent.ToString() + " difference: " + ( ZETCurrent - ZETStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleZET = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( ZETltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar ZET
                double countZET = ( countLTC / 1.002d ) / double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //ZET naar BTC
                double countBTC = countZET * double.Parse( ZETBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( ZETBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 ZET instaat
                if( priceNeededOfferStrat > double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.ZETBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.ZETBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL ZET" );

                            //Buy new ZET
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ZETLTC.Marketid, "Buy", ZETAmount, double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * ZETAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.ZETLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD ZET RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.ZETLTC.Marketid, "Buy", ZETAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.ZETLTC.Marketid, "Buy", ZETAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD ZET RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( ZETltcmarket.Return.Buyorders[0].Quantity ) > ZETAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.ZETLTC.Marketid );
                            currentOrder = placeOrder( markets.ZETLTC.Marketid, "Buy", ZETAmount, double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( ZETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * ZETAmount;


                        }
                        if( ZETAvailable - ZETStart > ZETAmount )
                        {
                            Console.WriteLine( "Selling partial bought ZET" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( ZETltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.ZETLTC.Marketid );
                            currentOrder = placeOrder( markets.ZETLTC.Marketid, "Buy", ZETAmount, double.Parse( ZETltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( ZETltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( ZETltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * ZETAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( ZETBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.ZETLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD ZET AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ZETBTC.Marketid, "Sell", ZETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.ZETLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling ZET orders!" );
                cancelOrdersOnMarket( markets.ZETLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleQRKLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.QRKLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.QRKBTC.Marketid ) )};



                Orders QRKltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders QRKBTCmarket = taskArray[2].Result;

                double QRKAmount = Math.Ceiling( 0.1d / ( ( double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    QRKStart = double.Parse( info.Return.BalancesAvailable.QRK );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                QRKCurrent = double.Parse( info.Return.BalancesAvailable.QRK ) + double.Parse( info.Return.BalancesHold.QRK );
                QRKAvailable = double.Parse( info.Return.BalancesAvailable.QRK );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "QRK start: " + QRKStart.ToString() + " current: " + QRKCurrent.ToString() + " difference: " + ( QRKCurrent - QRKStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleQRK = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( QRKltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar QRK
                double countQRK = ( countLTC / 1.002d ) / double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //QRK naar BTC
                double countBTC = countQRK * double.Parse( QRKBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( QRKBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 QRK instaat
                if( priceNeededOfferStrat > double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.QRKBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.QRKBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL QRK" );

                            //Buy new QRK
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.QRKLTC.Marketid, "Buy", QRKAmount, double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * QRKAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.QRKLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD QRK RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.QRKLTC.Marketid, "Buy", QRKAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.QRKLTC.Marketid, "Buy", QRKAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD QRK RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( QRKltcmarket.Return.Buyorders[0].Quantity ) > QRKAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.QRKLTC.Marketid );
                            currentOrder = placeOrder( markets.QRKLTC.Marketid, "Buy", QRKAmount, double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( QRKltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * QRKAmount;


                        }
                        if( QRKAvailable - QRKStart > QRKAmount )
                        {
                            Console.WriteLine( "Selling partial bought QRK" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( QRKltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.QRKLTC.Marketid );
                            currentOrder = placeOrder( markets.QRKLTC.Marketid, "Buy", QRKAmount, double.Parse( QRKltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( QRKltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( QRKltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * QRKAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( QRKBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.QRKLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD QRK AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.QRKBTC.Marketid, "Sell", QRKAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.QRKLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling QRK orders!" );
                cancelOrdersOnMarket( markets.QRKLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleNETLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.NETLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.NETBTC.Marketid ) )};



                Orders NETltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders NETBTCmarket = taskArray[2].Result;

                double NETAmount = Math.Ceiling( 0.1d / ( ( double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    NETStart = double.Parse( info.Return.BalancesAvailable.NET );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                NETCurrent = double.Parse( info.Return.BalancesAvailable.NET ) + double.Parse( info.Return.BalancesHold.NET );
                NETAvailable = double.Parse( info.Return.BalancesAvailable.NET );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "NET start: " + NETStart.ToString() + " current: " + NETCurrent.ToString() + " difference: " + ( NETCurrent - NETStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleNET = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( NETltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar NET
                double countNET = ( countLTC / 1.002d ) / double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //NET naar BTC
                double countBTC = countNET * double.Parse( NETBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( NETBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 NET instaat
                if( priceNeededOfferStrat > double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.NETBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.NETBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL NET" );

                            //Buy new NET
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.NETLTC.Marketid, "Buy", NETAmount, double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * NETAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.NETLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD NET RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.NETLTC.Marketid, "Buy", NETAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.NETLTC.Marketid, "Buy", NETAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD NET RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( NETltcmarket.Return.Buyorders[0].Quantity ) > NETAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.NETLTC.Marketid );
                            currentOrder = placeOrder( markets.NETLTC.Marketid, "Buy", NETAmount, double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( NETltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * NETAmount;


                        }
                        if( NETAvailable - NETStart > NETAmount )
                        {
                            Console.WriteLine( "Selling partial bought NET" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( NETltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.NETLTC.Marketid );
                            currentOrder = placeOrder( markets.NETLTC.Marketid, "Buy", NETAmount, double.Parse( NETltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( NETltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( NETltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * NETAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( NETBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.NETLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD NET AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.NETBTC.Marketid, "Sell", NETAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.NETLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling NET orders!" );
                cancelOrdersOnMarket( markets.NETLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleSBCLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.SBCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.SBCBTC.Marketid ) )};



                Orders SBCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders SBCBTCmarket = taskArray[2].Result;

                double SBCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    SBCStart = double.Parse( info.Return.BalancesAvailable.SBC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                SBCCurrent = double.Parse( info.Return.BalancesAvailable.SBC ) + double.Parse( info.Return.BalancesHold.SBC );
                SBCAvailable = double.Parse( info.Return.BalancesAvailable.SBC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "SBC start: " + SBCStart.ToString() + " current: " + SBCCurrent.ToString() + " difference: " + ( SBCCurrent - SBCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleSBC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( SBCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar SBC
                double countSBC = ( countLTC / 1.002d ) / double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //SBC naar BTC
                double countBTC = countSBC * double.Parse( SBCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( SBCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 SBC instaat
                if( priceNeededOfferStrat > double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.SBCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.SBCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL SBC" );

                            //Buy new SBC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.SBCLTC.Marketid, "Buy", SBCAmount, double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * SBCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.SBCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD SBC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.SBCLTC.Marketid, "Buy", SBCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.SBCLTC.Marketid, "Buy", SBCAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD SBC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( SBCltcmarket.Return.Buyorders[0].Quantity ) > SBCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.SBCLTC.Marketid );
                            currentOrder = placeOrder( markets.SBCLTC.Marketid, "Buy", SBCAmount, double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( SBCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * SBCAmount;


                        }
                        if( SBCAvailable - SBCStart > SBCAmount )
                        {
                            Console.WriteLine( "Selling partial bought SBC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( SBCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.SBCLTC.Marketid );
                            currentOrder = placeOrder( markets.SBCLTC.Marketid, "Buy", SBCAmount, double.Parse( SBCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( SBCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( SBCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * SBCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( SBCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.SBCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD SBC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.SBCBTC.Marketid, "Sell", SBCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.SBCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling SBC orders!" );
                cancelOrdersOnMarket( markets.SBCLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handlePPCLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.PPCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.PPCBTC.Marketid ) )};



                Orders PPCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders PPCBTCmarket = taskArray[2].Result;

                double PPCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) );

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    PPCStart = double.Parse( info.Return.BalancesAvailable.PPC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                PPCCurrent = double.Parse( info.Return.BalancesAvailable.PPC ) + double.Parse( info.Return.BalancesHold.PPC );
                PPCAvailable = double.Parse( info.Return.BalancesAvailable.PPC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "PPC start: " + PPCStart.ToString() + " current: " + PPCCurrent.ToString() + " difference: " + ( PPCCurrent - PPCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possiblePPC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( PPCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar PPC
                double countPPC = ( countLTC / 1.002d ) / double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //PPC naar BTC
                double countBTC = countPPC * double.Parse( PPCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( PPCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 PPC instaat
                if( priceNeededOfferStrat > double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.PPCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.PPCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL PPC" );

                            //Buy new PPC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PPCLTC.Marketid, "Buy", PPCAmount, double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * PPCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.PPCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD PPC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.PPCLTC.Marketid, "Buy", PPCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.PPCLTC.Marketid, "Buy", PPCAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD PPC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( PPCltcmarket.Return.Buyorders[0].Quantity ) > PPCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.PPCLTC.Marketid );
                            currentOrder = placeOrder( markets.PPCLTC.Marketid, "Buy", PPCAmount, double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( PPCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * PPCAmount;


                        }
                        if( PPCAvailable - PPCStart > PPCAmount )
                        {
                            Console.WriteLine( "Selling partial bought PPC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( PPCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.PPCLTC.Marketid );
                            currentOrder = placeOrder( markets.PPCLTC.Marketid, "Buy", PPCAmount, double.Parse( PPCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( PPCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( PPCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * PPCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( PPCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.PPCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD PPC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PPCBTC.Marketid, "Sell", PPCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.PPCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling PPC orders!" );
                cancelOrdersOnMarket( markets.PPCLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleCNCLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.CNCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.CNCBTC.Marketid ) )};



                Orders CNCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders CNCBTCmarket = taskArray[2].Result;

                double CNCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    CNCStart = double.Parse( info.Return.BalancesAvailable.CNC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                CNCCurrent = double.Parse( info.Return.BalancesAvailable.CNC ) + double.Parse( info.Return.BalancesHold.CNC );
                CNCAvailable = double.Parse( info.Return.BalancesAvailable.CNC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "CNC start: " + CNCStart.ToString() + " current: " + CNCCurrent.ToString() + " difference: " + ( CNCCurrent - CNCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleCNC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( CNCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar CNC
                double countCNC = ( countLTC / 1.002d ) / double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //CNC naar BTC
                double countBTC = countCNC * double.Parse( CNCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( CNCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 CNC instaat
                if( priceNeededOfferStrat > double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.CNCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.CNCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL CNC" );

                            //Buy new CNC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CNCLTC.Marketid, "Buy", CNCAmount, double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * CNCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.CNCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD CNC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.CNCLTC.Marketid, "Buy", CNCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.CNCLTC.Marketid, "Buy", CNCAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD CNC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( CNCltcmarket.Return.Buyorders[0].Quantity ) > CNCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.CNCLTC.Marketid );
                            currentOrder = placeOrder( markets.CNCLTC.Marketid, "Buy", CNCAmount, double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( CNCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * CNCAmount;


                        }
                        if( CNCAvailable - CNCStart > CNCAmount )
                        {
                            Console.WriteLine( "Selling partial bought CNC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( CNCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.CNCLTC.Marketid );
                            currentOrder = placeOrder( markets.CNCLTC.Marketid, "Buy", CNCAmount, double.Parse( CNCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( CNCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( CNCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * CNCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( CNCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.CNCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD CNC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CNCBTC.Marketid, "Sell", CNCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.CNCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling CNC orders!" );
                cancelOrdersOnMarket( markets.CNCLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleCGBLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.CGBLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.CGBBTC.Marketid ) )};



                Orders CGBltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders CGBBTCmarket = taskArray[2].Result;

                double CGBAmount = Math.Ceiling( 0.1d / ( ( double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    CGBStart = double.Parse( info.Return.BalancesAvailable.CGB );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                CGBCurrent = double.Parse( info.Return.BalancesAvailable.CGB ) + double.Parse( info.Return.BalancesHold.CGB );
                CGBAvailable = double.Parse( info.Return.BalancesAvailable.CGB );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "CGB start: " + CGBStart.ToString() + " current: " + CGBCurrent.ToString() + " difference: " + ( CGBCurrent - CGBStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleCGB = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( CGBltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar CGB
                double countCGB = ( countLTC / 1.002d ) / double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //CGB naar BTC
                double countBTC = countCGB * double.Parse( CGBBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( CGBBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 CGB instaat
                if( priceNeededOfferStrat > double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.CGBBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.CGBBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL CGB" );

                            //Buy new CGB
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CGBLTC.Marketid, "Buy", CGBAmount, double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * CGBAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.CGBLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD CGB RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.CGBLTC.Marketid, "Buy", CGBAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.CGBLTC.Marketid, "Buy", CGBAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD CGB RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( CGBltcmarket.Return.Buyorders[0].Quantity ) > CGBAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.CGBLTC.Marketid );
                            currentOrder = placeOrder( markets.CGBLTC.Marketid, "Buy", CGBAmount, double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( CGBltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * CGBAmount;


                        }
                        if( CGBAvailable - CGBStart > CGBAmount )
                        {
                            Console.WriteLine( "Selling partial bought CGB" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( CGBltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.CGBLTC.Marketid );
                            currentOrder = placeOrder( markets.CGBLTC.Marketid, "Buy", CGBAmount, double.Parse( CGBltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( CGBltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( CGBltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * CGBAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( CGBBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.CGBLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD CGB AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.CGBBTC.Marketid, "Sell", CGBAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.CGBLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling CGB orders!" );
                cancelOrdersOnMarket( markets.CGBLTC.Marketid );
                currentOrder = null;
            }
        }

        private static void handleANCLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.ANCLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.ANCBTC.Marketid ) )};



                Orders ANCltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders ANCBTCmarket = taskArray[2].Result;

                double ANCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) );

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    ANCStart = double.Parse( info.Return.BalancesAvailable.ANC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                ANCCurrent = double.Parse( info.Return.BalancesAvailable.ANC ) + double.Parse( info.Return.BalancesHold.ANC );
                ANCAvailable = double.Parse( info.Return.BalancesAvailable.ANC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "ANC start: " + ANCStart.ToString() + " current: " + ANCCurrent.ToString() + " difference: " + ( ANCCurrent - ANCStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleANC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( ANCltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar ANC
                double countANC = ( countLTC / 1.002d ) / double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //ANC naar BTC
                double countBTC = countANC * double.Parse( ANCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( ANCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 ANC instaat
                if( priceNeededOfferStrat > double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.ANCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.ANCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL ANC" );

                            //Buy new ANC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ANCLTC.Marketid, "Buy", ANCAmount, double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * ANCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.ANCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD ANC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.ANCLTC.Marketid, "Buy", ANCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.ANCLTC.Marketid, "Buy", ANCAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD ANC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( ANCltcmarket.Return.Buyorders[0].Quantity ) > ANCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.ANCLTC.Marketid );
                            currentOrder = placeOrder( markets.ANCLTC.Marketid, "Buy", ANCAmount, double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( ANCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * ANCAmount;


                        }
                        if( ANCAvailable - ANCStart > ANCAmount )
                        {
                            Console.WriteLine( "Selling partial bought ANC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( ANCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.ANCLTC.Marketid );
                            currentOrder = placeOrder( markets.ANCLTC.Marketid, "Buy", ANCAmount, double.Parse( ANCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( ANCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( ANCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * ANCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( ANCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.ANCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD ANC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.ANCBTC.Marketid, "Sell", ANCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.ANCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
                }
            }
            catch( Exception e )
            {
                Console.WriteLine( "Something went wrong in loop!" );
                Console.WriteLine( "Cancelling ANC orders!" );
                cancelOrdersOnMarket( markets.ANCLTC.Marketid );
                currentOrder = null;
            }
        }


        private static void handleWDCLTCMarket( ref int count, ref int countClean, Markets markets )
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

                double WDCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

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
                double countWDC = ( countLTC / 1.002d ) / double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //WDC naar BTC
                double countBTC = countWDC * double.Parse( WDCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( WDCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 WDC instaat
                if( priceNeededOfferStrat > double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.WDCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.WDCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL WDC" );

                            //Buy new WDC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * WDCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.WDCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD WDC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
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

                            }
                            else
                            {
                                Console.WriteLine( "SOLD WDC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( WDCltcmarket.Return.Buyorders[0].Quantity ) > WDCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                            currentOrder = placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( WDCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * WDCAmount;


                        }
                        if( WDCAvailable - WDCStart > WDCAmount )
                        {
                            Console.WriteLine( "Selling partial bought WDC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                            currentOrder = placeOrder( markets.WDCLTC.Marketid, "Buy", WDCAmount, double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( WDCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * WDCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( WDCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.WDCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD WDC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.WDCBTC.Marketid, "Sell", WDCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.WDCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
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

        private static void handleGLDLTCMarket( ref int count, ref int countClean, Markets markets )
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

                double GLDAmount = Math.Ceiling( 0.1d / ( ( double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

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
                double countGLD = ( countLTC / 1.002d ) / double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //GLD naar BTC
                double countBTC = countGLD * double.Parse( GLDBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( GLDBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 GLD instaat
                if( priceNeededOfferStrat > double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.GLDBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.GLDBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL GLD" );

                            //Buy new GLD
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * GLDAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.GLDLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD GLD RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
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

                            }
                            else
                            {
                                Console.WriteLine( "SOLD GLD RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( GLDltcmarket.Return.Buyorders[0].Quantity ) > GLDAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                            currentOrder = placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( GLDltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * GLDAmount;


                        }
                        if( GLDAvailable - GLDStart > GLDAmount )
                        {
                            Console.WriteLine( "Selling partial bought GLD" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                            currentOrder = placeOrder( markets.GLDLTC.Marketid, "Buy", GLDAmount, double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( GLDltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * GLDAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( GLDBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.GLDLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD GLD AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.GLDBTC.Marketid, "Sell", GLDAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.GLDLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
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

        private static void handlePXCLTCMarket( ref int count, ref int countClean, Markets markets )
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

                double PXCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

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
                double countPXC = ( countLTC / 1.002d ) / double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //PXC naar BTC
                double countBTC = countPXC * double.Parse( PXCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( PXCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 PXC instaat
                if( priceNeededOfferStrat > double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.PXCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.PXCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL PXC" );

                            //Buy new PXC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * PXCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.PXCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD PXC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
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

                            }
                            else
                            {
                                Console.WriteLine( "SOLD PXC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( PXCltcmarket.Return.Buyorders[0].Quantity ) > PXCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                            currentOrder = placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( PXCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * PXCAmount;


                        }
                        if( PXCAvailable - PXCStart > PXCAmount )
                        {
                            Console.WriteLine( "Selling partial bought PXC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                            currentOrder = placeOrder( markets.PXCLTC.Marketid, "Buy", PXCAmount, double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( PXCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * PXCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( PXCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.PXCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD PXC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.PXCBTC.Marketid, "Sell", PXCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.PXCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
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

        private static void handleDGCLTCMarket( ref int count, ref int countClean, Markets markets )
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

                double DGCAmount = Math.Ceiling( 0.1d / ( ( double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

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
                double countDGC = ( countLTC / 1.002d ) / double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //DGC naar BTC
                double countBTC = countDGC * double.Parse( DGCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( DGCBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 DGC instaat
                if( priceNeededOfferStrat > double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.DGCBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.DGCBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL DGC" );

                            //Buy new DGC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * DGCAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.DGCLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD DGC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
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

                            }
                            else
                            {
                                Console.WriteLine( "SOLD DGC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( DGCltcmarket.Return.Buyorders[0].Quantity ) > DGCAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                            currentOrder = placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( DGCltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * DGCAmount;


                        }
                        if( DGCAvailable - DGCStart > DGCAmount )
                        {
                            Console.WriteLine( "Selling partial bought DGC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                            currentOrder = placeOrder( markets.DGCLTC.Marketid, "Buy", DGCAmount, double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( DGCltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * DGCAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( DGCBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.DGCLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD DGC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.DGCBTC.Marketid, "Sell", DGCAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.DGCLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
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

        private static void handleMECLTCMarket( ref int count, ref int countClean, Markets markets )
        {

            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.MECLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.MECBTC.Marketid ) )};



                Orders MECltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders MECBTCmarket = taskArray[2].Result;

                double MECAmount = Math.Ceiling( 0.1d / ( ( double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) ) + 1;

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    MECStart = double.Parse( info.Return.BalancesAvailable.MEC );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                MECCurrent = double.Parse( info.Return.BalancesAvailable.MEC ) + double.Parse( info.Return.BalancesHold.MEC );
                MECAvailable = double.Parse( info.Return.BalancesAvailable.MEC );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "MEC start: " + MECStart.ToString() + " current: " + MECCurrent.ToString() + " difference: " + ( MECCurrent - MECStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleMEC = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( MECltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar MEC
                double countMEC = ( countLTC / 1.002d ) / double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //MEC naar BTC
                double countBTC = countMEC * double.Parse( MECBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( MECBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 MEC instaat
                if( priceNeededOfferStrat > double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.MECBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.MECBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL MEC" );

                            //Buy new MEC
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.MECLTC.Marketid, "Buy", MECAmount, double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + 0.00000002d ) ) };

                            currentPrice = double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * MECAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.MECLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD MEC RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.MECLTC.Marketid, "Buy", MECAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.MECLTC.Marketid, "Buy", MECAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD MEC RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( MECltcmarket.Return.Buyorders[0].Quantity ) > MECAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.MECLTC.Marketid );
                            currentOrder = placeOrder( markets.MECLTC.Marketid, "Buy", MECAmount, double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( MECltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * MECAmount;


                        }
                        if( MECAvailable - MECStart > MECAmount )
                        {
                            Console.WriteLine( "Selling partial bought MEC" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( MECltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.MECLTC.Marketid );
                            currentOrder = placeOrder( markets.MECLTC.Marketid, "Buy", MECAmount, double.Parse( MECltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( MECltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( MECltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * MECAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( MECBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.MECLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD MEC AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.MECBTC.Marketid, "Sell", MECAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.MECLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
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

        private static void handleXPMLTCMarket( ref int count, ref int countClean, Markets markets )
        {
            try
            {
                DateTime loopStart = DateTime.Now;
                Task<Orders>[] taskArray = { Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.XPMLTC.Marketid )),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.LTCBTC.Marketid ) ),
                                     Task<Orders>.Factory.StartNew(() => getAllOrdersByMarketID( markets.XPMBTC.Marketid ) )};



                Orders XPMltcmarket = taskArray[0].Result;
                Orders ltcbtcmarket = taskArray[1].Result;
                Orders XPMBTCmarket = taskArray[2].Result;

                double XPMAmount = Math.Ceiling( 0.1d / ( ( double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d ) );

                Console.WriteLine( "" );
                Balances info = getInfo();

                if( count == 0 )
                {
                    btcStart = double.Parse( info.Return.BalancesAvailable.BTC );
                    ltcStart = double.Parse( info.Return.BalancesAvailable.LTC );
                    XPMStart = double.Parse( info.Return.BalancesAvailable.XPM );
                }
                btcCurrent = double.Parse( info.Return.BalancesAvailable.BTC ) + double.Parse( info.Return.BalancesHold.BTC );
                ltcCurrent = double.Parse( info.Return.BalancesAvailable.LTC ) + double.Parse( info.Return.BalancesHold.LTC );
                XPMCurrent = double.Parse( info.Return.BalancesAvailable.XPM ) + double.Parse( info.Return.BalancesHold.XPM );
                XPMAvailable = double.Parse( info.Return.BalancesAvailable.XPM );
                if( count % roundsForBalanceInfo == 0 )
                {
                    Console.WriteLine( "---------------- BALANCE INFO after " + transactionDone + " transactions -----------" );
                    Console.WriteLine( "BTC start: " + btcStart.ToString() + " current: " + btcCurrent.ToString() + " difference: " + ( btcCurrent - btcStart ).ToString() );
                    Console.WriteLine( "LTC start: " + ltcStart.ToString() + " current: " + ltcCurrent.ToString() + " difference: " + ( ltcCurrent - ltcStart ).ToString() );
                    Console.WriteLine( "XPM start: " + XPMStart.ToString() + " current: " + XPMCurrent.ToString() + " difference: " + ( XPMCurrent - XPMStart ).ToString() );
                }
                count++;
                Console.WriteLine( "" );
                Console.WriteLine( "---------------- ROUND " + count + " ----" + DateTime.Now.Subtract( loopStart ).TotalMilliseconds + "s-----------" );

                //BTC naar LTC
                double countLTC = ( 1.002d ) / double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                double possibleXPM = ( double.Parse( ltcbtcmarket.Return.Sellorders[0].Quantity ) * buyFee ) / double.Parse( XPMltcmarket.Return.Sellorders[0].Sellprice );

                //LTC naar XPM
                double countXPM = ( countLTC / 1.002d ) / double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;

                //XPM naar BTC
                double countBTC = countXPM * double.Parse( XPMBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d;
                Console.WriteLine( "Profit margin if spend 1BTC -> " + countBTC );
                double priceNeededOfferStrat = ( countLTC / 1.002d ) / ( minimumBTCEarnings / ( double.Parse( XPMBTCmarket.Return.Buyorders[0].Buyprice ) * 0.997d ) );

                //zorg dat er een trade van 1 XPM instaat
                if( priceNeededOfferStrat > double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump )
                {
                    Task<MyOrders>[] myOrders = { Task<MyOrders>.Factory.StartNew( () => getOrdersByMarketID( markets.XPMBTC.Marketid ) ) };
                    countClean++;
                    //check if you have already ordered 1 at the current best price
                    if( TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" ).Subtract( now ).TotalHours > cleanTime )
                    {
                        Console.WriteLine( "SHOULD CLEAN" );
                        if( cleanCount == roundTimeOutWhenCleaning || ( countSellOrders( myOrders[0].Result.Return ) == 0 ) )
                        {
                            now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId( DateTime.UtcNow, "Eastern Standard Time" );
                        }
                        else if( cleanCount == 0 )
                        {
                            for( int i = 0; i < myOrders[0].Result.Return.Length; i++ )
                            {
                                if( myOrders[0].Result.Return[i].Ordertype.Equals( "Sell" ) )
                                {
                                    cancelOrder( myOrders[0].Result.Return[i].Orderid );
                                }
                            }
                        }
                    }
                    else
                    {
                        //check if you have already ordered 1 at the current best price
                        int currentSellToBTCOrders = getOrdersByMarketID( markets.XPMBTC.Marketid ).Return.Length;
                        if( currentOrder == null && currentSellToBTCOrders < maxSimuOrdersPerCoin )
                        {
                            Console.WriteLine( " INITIAL XPM" );

                            //Buy new XPM
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.XPMLTC.Marketid, "Buy", XPMAmount, double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) ) };

                            currentPrice = double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * XPMAmount;
                            if( orderArray[0].Result.Success.Equals( "1" ) )
                            {
                                currentOrder = orderArray[0].Result;
                            }

                        }
                        else if( currentOrder != null && countBuyOrders( getOrdersByMarketID( markets.XPMLTC.Marketid ).Return ) == 0 )
                        {
                            transactionDone++;
                            countClean = 0;

                            if( currentSellToBTCOrders < maxSimuOrdersPerCoin )
                            {
                                Console.WriteLine( "SOLD XPM RESELL AND NEW ORDERS" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.XPMLTC.Marketid, "Buy", XPMAmount, currentPrice ) )};
                                    if( orderArray[1].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[1].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) ),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.XPMLTC.Marketid, "Buy", XPMAmount, currentPrice ) )};
                                    if( orderArray[2].Result.Success.Equals( "1" ) )
                                    {
                                        currentOrder = orderArray[2].Result;
                                    }
                                    else
                                    {
                                        currentOrder = null;
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine( "SOLD XPM RESELL AND MAX REACHED" );
                                if( ltcCurrent > maxLTC )
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice ) ) };
                                }
                                else
                                {
                                    Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                                }
                                currentOrder = null;
                            }
                        }

                        if( currentOrder != null && currentPrice < double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) )// || ( currentPrice == double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) && double.Parse( XPMltcmarket.Return.Buyorders[0].Quantity ) > XPMAmount ) )
                        {

                            Console.WriteLine( "Found higher price" );
                            cancelOrdersOnMarket( markets.XPMLTC.Marketid );
                            currentOrder = placeOrder( markets.XPMLTC.Marketid, "Buy", XPMAmount, double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( XPMltcmarket.Return.Buyorders[0].Buyprice ) + raiseJump ) * 1.002d * XPMAmount;


                        }
                        if( XPMAvailable - XPMStart > XPMAmount )
                        {
                            Console.WriteLine( "Selling partial bought XPM" );
                            if( ltcCurrent > maxLTC )
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice ) ) };
                            }
                            else
                            {
                                Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                            }
                        }

                    }
                    if( countClean == 20 && false )
                    {
                        Console.WriteLine( "Doing cleanup!" );

                        if( ( currentPrice - double.Parse( XPMltcmarket.Return.Buyorders[1].Buyprice ) ) > raiseJump )
                        {
                            Console.WriteLine( "Found gap between 1 and 2 price" );
                            cancelOrdersOnMarket( markets.XPMLTC.Marketid );
                            currentOrder = placeOrder( markets.XPMLTC.Marketid, "Buy", XPMAmount, double.Parse( XPMltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump );
                            if( !currentOrder.Success.Equals( "1" ) )
                            {
                                currentOrder = null;
                            }
                            currentPrice = double.Parse( XPMltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump;
                            ltcSpend = ( double.Parse( XPMltcmarket.Return.Buyorders[1].Buyprice ) + raiseJump ) * 1.002d * XPMAmount;
                        }
                        countClean = 0;
                    }
                    btcPrice = double.Parse( XPMBTCmarket.Return.Buyorders[0].Buyprice );
                    ltcPrice = double.Parse( ltcbtcmarket.Return.Sellorders[0].Sellprice );
                }
                else if( currentOrder != null )
                {
                    if( countBuyOrders( getOrdersByMarketID( markets.XPMLTC.Marketid ).Return ) == 0 )
                    {
                        Console.WriteLine( "SOLD XPM AND NEW ORDERS BEFORE CANCEL" );
                        if( ltcCurrent > maxLTC )
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew( () => placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice ) ) };
                        }
                        else
                        {
                            Task<OrderResponse>[] orderArray = { Task<OrderResponse>.Factory.StartNew(() =>  placeOrder( markets.XPMBTC.Marketid, "Sell", XPMAmount, btcPrice )),
                                    Task<OrderResponse>.Factory.StartNew(() => placeOrder( markets.LTCBTC.Marketid, "Buy", ltcSpend, ltcPrice ) )};
                        }
                        currentOrder = null;
                        transactionDone++;
                        countClean = 0;
                    }
                    else
                    {
                        Console.WriteLine( "Cancel orders!! Not profitable!" );
                        cancelOrdersOnMarket( markets.XPMLTC.Marketid );
                        currentOrder = null;
                        countClean = 0;
                    }
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
        #endregion

        #region api functions

        private static int countBuyOrders( MyOrdersJsonTypes.Return[] p )
        {
            int ret = 0;
            for( int i = 0; i < p.Length; i++ )
            {
                if( p[i].Ordertype.Equals( "Buy" ) )
                {
                    ret++;
                }
            }
            return ret;
        }

        private static int countSellOrders( MyOrdersJsonTypes.Return[] p )
        {
            int ret = 0;
            for( int i = 0; i < p.Length; i++ )
            {
                if( p[i].Ordertype.Equals( "Sell" ) )
                {
                    ret++;
                }
            }
            return ret;
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

        private static void cancelOrder( string orderid )
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create( "https://www.Cryptsy.com/api" );
            string nonce = DateTime.UtcNow.Subtract( new DateTime( 1988, 7, 21 ) ).TotalSeconds.ToString();
            string postData =
                "method=cancelorder&orderid=" + orderid +
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
                cancelOrdersOnMarket( orderid );
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

        #endregion

        #region variables
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

        public static double MECStart
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

        public static double MECCurrent
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

        public static double XPMStart
        {
            get;
            set;
        }

        public static double XPMCurrent
        {
            get;
            set;
        }

        public static double XPMAvailable
        {
            get;
            set;
        }

        public static double MECAvailable
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

        #endregion

        public static double ZETStart
        {
            get;
            set;
        }

        public static double ZETCurrent
        {
            get;
            set;
        }

        public static double ZETAvailable
        {
            get;
            set;
        }

        public static double SBCStart
        {
            get;
            set;
        }

        public static double SBCCurrent
        {
            get;
            set;
        }

        public static double SBCAvailable
        {
            get;
            set;
        }

        public static double QRKStart
        {
            get;
            set;
        }

        public static double QRKCurrent
        {
            get;
            set;
        }

        public static double QRKAvailable
        {
            get;
            set;
        }

        public static double PPCStart
        {
            get;
            set;
        }

        public static double PPCCurrent
        {
            get;
            set;
        }

        public static double PPCAvailable
        {
            get;
            set;
        }

        public static double NETStart
        {
            get;
            set;
        }

        public static double NETCurrent
        {
            get;
            set;
        }

        public static double NETAvailable
        {
            get;
            set;
        }

        public static double CNCStart
        {
            get;
            set;
        }

        public static double CNCCurrent
        {
            get;
            set;
        }

        public static double CNCAvailable
        {
            get;
            set;
        }

        public static double CGBStart
        {
            get;
            set;
        }

        public static double CGBCurrent
        {
            get;
            set;
        }

        public static double CGBAvailable
        {
            get;
            set;
        }

        public static double ANCStart
        {
            get;
            set;
        }

        public static double ANCCurrent
        {
            get;
            set;
        }

        public static double ANCAvailable
        {
            get;
            set;
        }
    }
}
