using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using WindowsInput;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    static async Task Main(string[] args)
    {
        var simulator = new InputSimulator();
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe", //Path
            Headless = true,
            Args = new[] {
                "--disable-blink-features=AutomationControlled",
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-infobars", // Disable infobars like "Chrome is being controlled"
                "--disable-extensions", // Disable extensions
                "--disable-notifications", // Disable notifications
                "--disable-background-timer-throttling", // Reduce throttling of background timers
                "--disable-sync", // Disable synchronization
                "--metrics-recording-only", // Prevent browser performance metrics collection
                "--mute-audio", // Mute audio if not needed
            }
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = null
        });

        await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            { "Accept-Language", "en-GB,en-US,en" },
            { "DNT", "1" }, // Do Not Track
            { "Upgrade-Insecure-Requests", "1" }
        });

        var page = await context.NewPageAsync();
        string link = "https://www.sellu.ge/";
        await page.GotoAsync(link);

        Console.WriteLine("Waiting for the page to load...");
        await Task.Delay(1000);
        await page.ClickAsync("//*[@id=\"auction-list\"]/div[1]/div[4]/a");


        Console.WriteLine("Auction Enter button clicked");
        Console.WriteLine("Sleeping for 2 Seconds..");
        await Task.Delay(4000);


        Console.WriteLine("We are starting...");


        while (true)
        {

            var timerElement = await page.QuerySelectorAsync("xpath=//*[@id=\"auctionCountdown\"]");

            string timerText = await timerElement.TextContentAsync();
            if (timerText == null)
            {
                Console.WriteLine("not found");
            }
            string numericTimer = Regex.Replace(timerText, "[^0-9]", "");
            if (int.TryParse(numericTimer, out int timerValue) && timerValue <= 40)
            {
                Console.WriteLine($"Timer value is {timerValue}, waiting for a valid range...");
                await Task.Delay(1000);

                var timerElement2 = await page.QuerySelectorAsync("xpath=//*[@id=\"auctionCountdown\"]");

                string timerText2 = await timerElement2.TextContentAsync();
                string numericTimer2 = Regex.Replace(timerText2, "[^0-9]", "");
                if (int.TryParse(numericTimer2, out int timerValue2) && timerValue2 <= 80)
                {
                    simulator.Mouse.LeftButtonClick();
                    Console.WriteLine($"Button clicked at: {timerText2}");
                    await Task.Delay(100);

                }
            }

        }
    }




}