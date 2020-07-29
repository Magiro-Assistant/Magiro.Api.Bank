using System;
using OpenQA.Selenium;
using Polly;

namespace Magiro.Api.Bank.Helpers
{
    public class SeleniumHelper
    {
        public static void WaitForDocumentReady(IWebDriver driver)
        {
            Policy.Handle<Exception>()
                .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(5))
                .Execute(() =>
                {
                    Console.WriteLine("Waiting for five instances of document.readyState returning 'complete' at 100ms intervals.");
                    IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                    int i = 0; // Count of (document.readyState === complete) && (ae.isProcessing === false)
                    int j = 0; // Count of iterations in the while() loop.
                    int k = 0; // Count of times i was reset to 0.
                    bool readyState = false;
                    while (i < 5)
                    {
                        System.Threading.Thread.Sleep(100);
                        readyState = (bool)jse.ExecuteScript("return ((document.readyState === 'complete'))");
                        if (readyState) { i++; }
                        else
                        {
                            i = 0;
                            k++;
                        }
                        j++;
                        if (j > 300) { throw new TimeoutException("Timeout waiting for document.readyState to be complete."); }
                    }
                    j *= 100;
                    Console.WriteLine("Waited " + j.ToString() + " milliseconds. There were " + k + " resets.");
                });
        }
    }
}
