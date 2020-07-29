using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Magiro.Api.Bank.Banks
{
    public enum BankName { Handelsbanken, Seb }

    public abstract class Bank
    {
        public static Bank GetBank(BankName bankName, MagiroCache cache, string personalNumber)
        {
            Bank bank = null;
            switch (bankName)
            {
                case BankName.Handelsbanken:
                    bank = (Handelsbanken)cache.Get(BankName.Handelsbanken) ?? new Handelsbanken(true, personalNumber, true, false);
                    break;
                case BankName.Seb:
                    bank = (Seb)cache.Get(BankName.Seb) ?? new Seb(true, personalNumber, true, false);
                    break;
            }
            return bank;
        }
        public BankName SelectedBankName { get; set; } = BankName.Handelsbanken;
        public ChromeOptions Options { get; } = new ChromeOptions();
        public ChromeDriver ChromeDriver { get; }
        public bool IsPrivate { get; }
        public bool IsMobile { get; }
        public bool LoggedInDuringSession { get; set; }
        public DateTime? LoggedInDuringSessionDate { get; set; }

        public WebDriverWait Wait30Seconds => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(30));
        public WebDriverWait Wait60Seconds => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(60));
        public WebDriverWait Wait45Seconds => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(60));
        public WebDriverWait Wait10Seconds => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(10));
        public WebDriverWait Wait3Seconds => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(3));
        public WebDriverWait Wait1Seconds => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(1));

        public string PersonalNumber { get; set; }

        public string LoginPageUrl { get; set; }
        public string LandingPageAfterLoginUrl { get; set; }
        public string PaymentUrl { get; set; }

        public Bank(bool isPrivate, string personalNumber, bool headless = false, bool mobile = false)
        {
            IsPrivate = isPrivate;
            IsMobile = mobile;
            //string startUrl = "https://secure.handelsbanken.se/logon/se/" + (_isPrivate ? "priv" : "corp") + "/sv/mbidqr/";
            Options.AddArgument("--ignore-certificate-errors");
            Options.AddArguments("--no-sandbox");
            Options.AddArguments("--disable-dev-shm-usage");
            if (headless) Options.AddArgument("--headless");
            if (mobile) Options.AddArgument("--user-agent=Mozilla/5.0 (iPad; CPU OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5355d Safari/8536.25");
            PersonalNumber = personalNumber;

            ChromeDriver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"/Chrome", Options);
            SetLoginPageUrl();
            SetLandingPageAfterLoginUrl();
            SetPaymentUrl();
        }

        public void NavigateToUrl(string url)
        {
            ChromeDriver.Navigate().GoToUrl(url);
        }

        public void NavigateToLoginPageUrl()
        {
            ChromeDriver.Navigate().GoToUrl(LoginPageUrl);
        }

        public void NavigateToPaymentPageUrl()
        {
            ChromeDriver.Navigate().GoToUrl(PaymentUrl);
        }

        public void NavigateToLandingPageAfterLoginUrl()
        {
            ChromeDriver.Navigate().GoToUrl(LandingPageAfterLoginUrl);
        }

        public void ValidateLogin()
        {
            Wait30Seconds.Until(x => x.Url == LandingPageAfterLoginUrl);
            LoggedInDuringSession = true;
            LoggedInDuringSessionDate = DateTime.Now;
        }


        public bool IsLoggedInPreCheck()
        {
            if (!LoggedInDuringSession) return false;
            NavigateToLandingPageAfterLoginUrl();
            Wait3Seconds.Until(x => x.Url == LandingPageAfterLoginUrl);
            return true;
        }


        public abstract void SetLoginPageUrl();
        public abstract void SetLandingPageAfterLoginUrl();
        public abstract void SetPaymentUrl();
        public abstract bool CheckIfLoggedInByNavigateToLandingPage();
        public abstract bool ConfirmPayment();
        public abstract List<IWebElement> ListFromAccounts();
        public abstract void CreatePayment(int amount, DateTime paymentDate, string bankgiroPostgiro, string ocrMessage, dynamic accounts);
        public abstract string Authenticate();
    }
}
