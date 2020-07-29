using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Polly;

namespace Magiro.Api.Bank.Banks
{
    public class Seb : Bank
    {
        private string _loginPageUrl = "https://id.seb.se/ibf/mbid";
        private readonly string _landingPageAfterLoginUrl = "https://foretag.ib.seb.se/kgb/0/kgbc0001.aspx";
        private readonly string _paymentUrl = "https://foretag.ib.seb.se/kgb/1000/1100/kgbc1103.aspx";

        public Seb(bool isPrivate, string personalNumber, bool headless = false, bool mobile = false) : base(isPrivate, personalNumber, headless, mobile)
        {
        }

        #region SetPageUrls
        public override void SetLoginPageUrl()
        {
            LoginPageUrl = _loginPageUrl;
        }

        public override void SetLandingPageAfterLoginUrl()
        {
            LandingPageAfterLoginUrl = _landingPageAfterLoginUrl;
        }

        public override void SetPaymentUrl()
        {
            PaymentUrl = _paymentUrl;
        }
        #endregion

        public override void CreatePayment(int amount, DateTime paymentDate, string bankgiroPostgiro, string ocrMessage, dynamic accounts)
        {
            var toAccountXpath = "//*[@id='IKFMaster_MainPlaceHolder_A1']";
            var amountXpath = "//*[@id='IKFMaster_MainPlaceHolder_A3']";
            var dateXpath = "//*[@id='IKFMaster_MainPlaceHolder_A4']";
            var ocrRadioButtonXpath = "//*[@id='IKFMaster_MainPlaceHolder_OCR']";
            var messageXpath = "//*[@id='IKFMaster_MainPlaceHolder_A5']";
            var ownNoteXpath = "//*[@id='IKFMaster_MainPlaceHolder_A6']";
            var addPaymentXpath = "//*[@id='IKFMaster_MainPlaceHolder_BTN_ADB']";

            Wait3Seconds.Until(x => x.FindElement(By.XPath(toAccountXpath)));

            IWebElement tillKonto = ChromeDriver.FindElement(By.XPath(toAccountXpath));
            IWebElement belopp = ChromeDriver.FindElement(By.XPath(amountXpath));
            IWebElement datum = ChromeDriver.FindElement(By.XPath(dateXpath));
            IWebElement meddelande = ChromeDriver.FindElement(By.XPath(messageXpath));
            IWebElement egenNotering = ChromeDriver.FindElement(By.XPath(ownNoteXpath));
            IWebElement addPayment = ChromeDriver.FindElement(By.XPath(addPaymentXpath));

            belopp.SendKeys(amount.ToString());
            datum.SendKeys(paymentDate.ToString());
            tillKonto.SendKeys(bankgiroPostgiro);
            meddelande.SendKeys(ocrMessage);

            addPayment.Click();
        }

        public override bool CheckIfLoggedInByNavigateToLandingPage()
        {
            if (IsLoggedInPreCheck())
                return !ChromeDriver.PageSource.ToLower().Contains("inte inloggad");
            return false;
        }

        public override bool ConfirmPayment()
        {
            var inlagdaFörBetalningXpath = "//*[@id='IKFMaster_MainPlaceHolder_updBundle']/div[1]/div/table[1]/tbody/tr";
            var godkännKnappXpath = "//*[@id='IKFMaster_MainPlaceHolder_BTN_SEND']";
            ReadOnlyCollection<IWebElement> inlagdaFörBetalning = ChromeDriver.FindElements(By.XPath(inlagdaFörBetalningXpath));
            bool kanGodkänna = inlagdaFörBetalning.Count == 1;
            if (kanGodkänna)
            {
                ChromeDriver.FindElementByXPath(godkännKnappXpath).Click();

                Wait3Seconds.Until(x => x.Url == LandingPageAfterLoginUrl);
                var skickaKnappXpath = "//*[@id='IKFMaster_MainPlaceHolder_BTN_Send']";
                var signeraXpath = "//*[@id='IKFMaster_MainPlaceHolder_ucVerify_BTN_OK']";
                IWebElement skickaBetalningsKnapp = ChromeDriver.FindElement(By.XPath(skickaKnappXpath));
                skickaBetalningsKnapp.Click();

                Wait3Seconds.Until(ExpectedConditions.ElementIsVisible(By.XPath(signeraXpath)));
                IWebElement signeraKnapp = ChromeDriver.FindElement(By.XPath(signeraXpath));
                signeraKnapp.Click();

                Wait30Seconds.Until(x => x.Url == "xxx");

                return true;
            }

            return false;
        }

        public override List<IWebElement> ListFromAccounts()
        {
            var frånKontoXpath = "//*[@id='IKFMaster_MainPlaceHolder_A2']/option";

            Wait30Seconds.Until(x => ExpectedConditions.ElementIsVisible(By.XPath(frånKontoXpath)));

            List<IWebElement> frånKontos = new List<IWebElement>();
            Policy.Handle<Exception>()
                .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(1))
                .Execute(() =>
                {
                    frånKontos = ChromeDriver.FindElements(By.XPath(frånKontoXpath)).ToList();
                });

            return frånKontos;
            //throw new Exception("Lyckades inte läsa in frånkonton...");
        }

        public override string Authenticate()
        {
            return Policy.Handle<Exception>()
                .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(1))
                .Execute(() =>
                {
                    NavigateToLoginPageUrl();
                    string loginXpath = "//*[@id='login_button']";
                    string imgXpath = "//*[@id='modalBody']/div/div[2]/qrcode/img";
                    Wait3Seconds.Until(x => ExpectedConditions.ElementIsVisible(By.XPath(loginXpath)));
                    IWebElement frånKonto = ChromeDriver.FindElement(By.XPath(loginXpath));
                    frånKonto.Click();

                    string imgSrc = "";
                    Policy.Handle<Exception>()
                        .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(0.5))
                        .Execute(() =>
                        {
                            IWebElement element = ChromeDriver.FindElementByXPath(imgXpath);
                            imgSrc = element.GetAttribute("src");
                        });

                    return $"<img alt='Scan me!' src='{imgSrc}' style='display: block;'>";
                });
        }
    }
}
