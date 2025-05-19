using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static public Dictionary<string, Dictionary<int, int[]>> usersBiddingInfo = new Dictionary<string, Dictionary<int, int[]>>();
    static SortedDictionary<int, object[]> modifiedUsersBiddingInfo = new SortedDictionary<int, object[]>();
    static List<string> auctionFullInfo = new List<string>();
    static Dictionary<string, int> forRemindedBids = new Dictionary<string, int>();
    static string auctionName = "City Mall-ის 300 ლარიანი ვაუჩერი (15 May)";

    public static async Task Main(string[] args)
    {
        // Ensure the console supports Unicode output
        Console.OutputEncoding = Encoding.UTF8;

        using var ws = new ClientWebSocket();

        // Connect to WebSocket
        Uri serverUri = new Uri("wss://auction-ws.sellu.ge/auctions/2df0433f-6b9b-448c-a46d-d01edb443ea2");
        await ws.ConnectAsync(serverUri, CancellationToken.None);
        Console.WriteLine("Connected to WebSocket!");

        while (ws.State != WebSocketState.Open)
        {

        }
        // Start receiving messages
        _ = Task.Run(async () =>
        {
            byte[] buffer = new byte[4096];
            int interval = 0;
            int tempInterval = 0;
            long previous = 0;
            long temp = 0;
            while (true)  //ws.State == WebSocketState.Open
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Parse and print message
                ParseAndPrintBidData(message, ref previous, ref temp, ref interval, ref tempInterval);
            }
        });

        // Keep the program running to receive messages
        Console.ReadLine();


        WriteUsersOnConsoleAndInFileWhoBiddedOn3600();
        ModifyUserList();

        Console.ReadLine();

        BiddersTogeather();


        //second

        Console.ReadLine();
        WriteFullAuctionInTxtFile();


    }

    static void WriteUsersOnConsoleAndInFileWhoBiddedOn3600()
    {
        //console
        foreach (var user in usersBiddingInfo)
        {
            var res = user.Value.Where(x => x.Value[1] > 3500).Select(x => new { bids = x.Value[0], interval = x.Value[1], userInterval = x.Value[2] });
            Console.WriteLine("--------------------------");
            Console.WriteLine(user.Key);
            Console.WriteLine("--------------------------");

            foreach (var item in res)
            {
                Console.WriteLine("bidNumber: " + item.bids + " | " + "[" + item.interval + "]" + "[" + item.userInterval + "]");
            }
            Console.WriteLine("------------------------------------------------------------");
        }

        //file
        using (FileStream fs = new FileStream("AuctionLogs11.txt", FileMode.Append))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine("*****************");

                writer.Write(auctionName);
                writer.Write(" *****************");

                writer.WriteLine("---------------------------------------------------------------------------------");
                foreach (var user in usersBiddingInfo)
                {
                    var res = user.Value.Where(x => x.Value[1] > 3500).Select(x => new { bids = x.Value[0], interval = x.Value[1], userInterval = x.Value[2] });
                    writer.WriteLine("--------------------------");
                    writer.WriteLine(user.Key);
                    writer.WriteLine("--------------------------");

                    foreach (var item in res)
                    {
                        writer.WriteLine("bidNumber: " + item.bids + " | " + "[" + item.interval + "]" + "[" + item.userInterval + "]");
                    }
                    writer.WriteLine("------------------------------------------------------------");
                }
                writer.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");

            }
        }
    }

    //Modify And Sort UserDictionary
    static void ModifyUserList()
    {
        foreach (var user in usersBiddingInfo)
        {
            string name = user.Key;

            foreach (var item in user.Value)
            {
                var totalBidCount = item.Key;
                var delay = item.Value[1];
                modifiedUsersBiddingInfo[totalBidCount] = new object[2];
                modifiedUsersBiddingInfo[totalBidCount][0] = name;
                modifiedUsersBiddingInfo[totalBidCount][1] = delay;

            }

        }
    }
    //write all bidders who bidded togeather

    static void BiddersTogeather()
    {
        bool isFound = false;
        int currentBid = 0;
        Dictionary<int, object[]> res = new Dictionary<int, object[]>();
        foreach (var bid in modifiedUsersBiddingInfo)
        {

            string userName = (string)bid.Value[0];
            int userInterval = (int)bid.Value[1];
            int totalBid = bid.Key;
            if (userInterval > 3500)
            {

                res[totalBid] = new object[2] { userName, userInterval };




            }
            else if (res.Count >= 2)
            {
                Console.WriteLine("----------------------------------------------------------------------------");
                foreach (var item in res)
                {
                    Console.WriteLine(item.Key + "|---[" + item.Value[0] + "]---|" + item.Value[1]);


                }
                res.Clear();

            }
            else
            {
                res.Clear();
            }
        }
        currentBid = 0;
        isFound = false;
        using (FileStream fs = new FileStream("AuctionLogs2.txt", FileMode.Append))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine("***************** ");

                writer.Write(auctionName);
                writer.Write(" *****************");

                writer.WriteLine("---------------------------------------------------------------------------------");
                foreach (var bid in modifiedUsersBiddingInfo)
                {
                    string userName = (string)bid.Value[0];
                    int userInterval = (int)bid.Value[1];
                    int totalBid = bid.Key;
                    if (userInterval > 3500)
                    {
                        res[totalBid] = new object[2] { userName, userInterval };
                    }

                    else if (res.Count >= 2)
                    {
                        writer.WriteLine("---------------------------------------------------------------------------------");


                        foreach (var item in res)
                        {
                            writer.WriteLine(item.Key + "|-[" + item.Value[0] + "][" + item.Value[1] + "]");

                        }

                        res.Clear();

                    }
                    else
                    {
                        res.Clear();
                    }
                }
                writer.WriteLine("-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");

            }
        }
    }

    static void ParseAndPrintBidData(string data, ref long previous, ref long temp, ref int interval, ref int tempInterval)
    {
        int curentUserPlacedBid = 1;
        int userlastIntedval = 0;
        // Regex pattern to extract username and remainingBids
        string pattern = @"""username"":""(.*?)"",""bidPlacedAt"":(\d+),""remainingBids"":(\d+),""totalBidsPlaced"":(\d+)";

        Match match = Regex.Match(data, pattern);

        if (match.Success)
        {
            if (tempInterval == 0)
            {
                if (previous == 0)
                {
                    previous = long.Parse(match.Groups[2].Value);
                }
                else
                {
                    temp = long.Parse(match.Groups[2].Value);
                    interval = (int)(temp - previous);
                    previous = temp;
                }
                if (interval > 3500)
                {
                    tempInterval = interval;
                }
            }
            else
            {

                temp = long.Parse(match.Groups[2].Value);
                interval = (int)(temp - previous);
                previous = temp;
                if (interval < 200)
                {
                    interval = tempInterval + interval;
                }
                else
                {
                    tempInterval = 0;
                }
            }
            string username = match.Groups[1].Value;
            int totalBIdsNumber = int.Parse(match.Groups[4].Value);
            int userRemainingBids = int.Parse(match.Groups[3].Value); //user reminded bids
            if (usersBiddingInfo.ContainsKey(username))
            {

                var lastElementAddedInDictioary = usersBiddingInfo[username].Last();
                curentUserPlacedBid = lastElementAddedInDictioary.Value[0] + 1;
                usersBiddingInfo[username][totalBIdsNumber] = new int[3];
                usersBiddingInfo[username][totalBIdsNumber][0] = curentUserPlacedBid;
                usersBiddingInfo[username][totalBIdsNumber][1] = interval;
                usersBiddingInfo[username][totalBIdsNumber][2] = interval - lastElementAddedInDictioary.Value[1];

            }
            else
            {
                int placedBid = int.Parse(match.Groups[4].Value);
                usersBiddingInfo[username] = new Dictionary<int, int[]> { { placedBid, new int[3] { 1, interval, 0 } } };
            }

            //filling dictionary for RemindedBids.
            if (forRemindedBids.ContainsKey(username))
            {
                forRemindedBids[username] = userRemainingBids;

            }
            else
            {
                forRemindedBids.Add(username, userRemainingBids);
            }

            int allRemindedBids = 0;
            int remindedUsers = 0;
            foreach (var item in forRemindedBids)
            {
                allRemindedBids += item.Value;
                if (item.Value != 0)
                {
                    remindedUsers++;
                }
            }
            string parsedInfo = "";
            if (allRemindedBids % 10 == 0)
            {
                parsedInfo = $"[{totalBIdsNumber}].[{username}] ----- [{curentUserPlacedBid}/ {userRemainingBids}]--Interval is [{interval}]----Interval2[{usersBiddingInfo[username][totalBIdsNumber][2]}] ==== Bids Left-[{allRemindedBids}][{totalBIdsNumber + allRemindedBids}][{remindedUsers}])";
            }
            else
            {
                parsedInfo = $"[{totalBIdsNumber}].[{username}] ----- [{curentUserPlacedBid}/ {userRemainingBids}]--Interval is [{interval}]----Interval2[{usersBiddingInfo[username][totalBIdsNumber][2]}] ";
            }
            allRemindedBids = 0;
            Console.WriteLine(parsedInfo);
            auctionFullInfo.Add(parsedInfo);

            interval = 0;
        }
    }
    static void WriteFullAuctionInTxtFile()
    {
        using (FileStream fs = new FileStream("FullAuction.txt", FileMode.Append))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine("***************** ");

                writer.Write(auctionName);
                writer.Write(" *****************");

                writer.WriteLine("---------------------------------------------------------------------------------");
                foreach (var item in auctionFullInfo)
                {
                    writer.WriteLine(item);
                }
                writer.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");

            }
        }
    }
}
