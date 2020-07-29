using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Magiro.Api.Bank.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Polly;

namespace Magiro.Api.Bank.Banks
{
    public class Handelsbanken : Bank, IDisposable
    {
        private int _numberOfRetries;
        private int _maxNumberOfRetries = 3;
        private ICookieJar _cookies;
        private IWebDriver _frame;
        private bool _isMobile;

        public WebDriverWait WebDriverWait(int time) => new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(time));

        public Handelsbanken(bool isPrivate, string personalNumber, bool headless = false, bool mobile = false) : base(isPrivate, personalNumber, headless, mobile)
        {
            _cookies = ChromeDriver.Manage().Cookies;
        }

        #region SetPageUrls
        public override void SetPaymentUrl()
        {
            string privateUrl = "https://secure.handelsbanken.se/se/private/sv/#!/payments_and_transfers/payment";
            string corpUrl = "https://secure.handelsbanken.se/se/corporate/sv/#!/payments/outgoing/payments_transfers/payment";
            PaymentUrl = IsPrivate ? privateUrl : corpUrl;
        }

        public override void SetLoginPageUrl()
        {
            LoginPageUrl = "https://secure.handelsbanken.se/logon/se/" + (IsPrivate ? "priv" : "corp") + "/sv/mbidqr/";
        }

        public override void SetLandingPageAfterLoginUrl()
        {
            LandingPageAfterLoginUrl = $"https://secure.handelsbanken.se/se/" + (IsPrivate ? "private" : "corporate") + "/sv/";
        }
        #endregion

        public string GetBankIdUrl()
        {
            Wait45Seconds.Until(x => x.Url.Contains($"bankid.com"));
            var currentURL = ChromeDriver.Url;
            return currentURL;
        }

        private bool IsElementPresent(By by)
        {
            try
            {
                ChromeDriver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public void InitializeIframe()
        {
            SeleniumHelper.WaitForDocumentReady(ChromeDriver);
            Wait10Seconds.Until(x => ExpectedConditions.ElementIsVisible(By.TagName("iframe")));

            Policy.Handle<NoSuchElementException>()
                .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(1))
                .Execute(() =>
                {
                    _frame = ChromeDriver.SwitchTo().Frame(ChromeDriver.FindElement(By.TagName("iframe")));
                });
            SeleniumHelper.WaitForDocumentReady(ChromeDriver);
        }

        public override List<IWebElement> ListFromAccounts()
        {
            var frånKontoXpath = "//*[@id='FranKontoID']/option[3]";
            var frånKontoXpathAll = "//*[@id='FranKontoID']/option";

            Wait30Seconds.Until(x => ExpectedConditions.ElementIsVisible(By.XPath(frånKontoXpath)));

            List<IWebElement> frånKontos = new List<IWebElement>();
            Policy.Handle<Exception>()
                .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(1))
                .Execute(() =>
                {
                    //IWebElement frånKonto = _frame.FindElement(By.XPath(frånKontoXpath));
                    frånKontos = _frame.FindElements(By.XPath(frånKontoXpathAll)).Skip(2).ToList();
                });

            return frånKontos;
            //throw new Exception("Lyckades inte läsa in frånkonton...");
        }

        public override void CreatePayment(int amount, DateTime paymentDate, string bankgiroPostgiro, string ocrMessage, dynamic accounts)
        {
            IJavaScriptExecutor js = ChromeDriver;
            if (accounts != null)
            {
                var frånKontoXpath = $"//*[@id='FranKontoID']/option[{accounts.Index + 3}]";

                IWebElement frånKonto = _frame.FindElement(By.XPath(frånKontoXpath));
                frånKonto.Click();
            }

            var inkorrektOcrFortsättKnapp = IsPrivate ? "//*[@id='ContinueWarnCreateNewPayment']" : "//*[@name='CreateNewGiroPayment']";
            var nyMottagareXpath = "//*[@id='nyMottagare']";
            var bankgiroPlusgiroXpath = "//*[@id='KTONR_BETMOTT']";
            var öppnaMenyXpath = "/html/body/div[2]/header/div[2]/div/div/div[2]/div[3]/a/span/span[1]/span[1]";
            var ocrMeddelandeXpath = IsPrivate ? "//*[@id='fritext0']" : "//*[@name='BET_REF']";
            var betalDatumXpath = IsPrivate ? "//*[@id='dateField']" : "//*[@id='FORFALLODATUM']";
            var läggTillBetalningXpath = IsPrivate ? "//*[@id='CreateNewPayment']" : "//*[@id='CreateNewGiroPayment']";
            var beloppXpath = "//*[@id='TRANSAKTIONSBELOPP']";
            var betalaOchÖverföraXpath = "/html/body/div[2]/header/div[2]/div/div/div[1]/div/nav/div[3]/span/a";
            var betalaXpath = "/html/body/div[2]/header/div[2]/div/div/div[2]/div[3]/div/div/div/div[1]/div[2]/nav/div[1]/div[1]/a";

            if (IsPrivate)
            {
                Wait30Seconds.Until(x => ExpectedConditions.ElementIsVisible(By.XPath(nyMottagareXpath)));
                js.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", ChromeDriver.FindElement(By.XPath(nyMottagareXpath)));
            }

            IWebElement belopp = _frame.FindElement(By.XPath(beloppXpath));
            IWebElement ocr = ChromeDriver.FindElement(By.XPath(ocrMeddelandeXpath));
            IWebElement datum = ChromeDriver.FindElement(By.XPath(betalDatumXpath));
            IWebElement bankgiroPlusgiro = _frame.FindElement(By.XPath(bankgiroPlusgiroXpath));

            ocr.SendKeys(ocrMessage);
            datum.SendKeys(paymentDate.ToString());
            belopp.SendKeys(amount.ToString());
            bankgiroPlusgiro.SendKeys(bankgiroPostgiro);

            js.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", ChromeDriver.FindElement(By.XPath(läggTillBetalningXpath)));

            SeleniumHelper.WaitForDocumentReady(ChromeDriver);
            bool isElementDisplayed = IsElementPresent(By.XPath(inkorrektOcrFortsättKnapp));
            if (isElementDisplayed)
            {
                SeleniumHelper.WaitForDocumentReady(ChromeDriver);
                IWebElement fortsättKnapp = _frame.FindElement(By.XPath(inkorrektOcrFortsättKnapp));
                fortsättKnapp.Click();
            }

            isElementDisplayed = IsElementPresent(By.XPath(inkorrektOcrFortsättKnapp));
            if (isElementDisplayed)
            {
                SeleniumHelper.WaitForDocumentReady(ChromeDriver);
                IWebElement fortsättKnapp = _frame.FindElement(By.XPath(inkorrektOcrFortsättKnapp));
                fortsättKnapp.Click();
            }
        }

        public override bool CheckIfLoggedInByNavigateToLandingPage()
        {
            if (IsLoggedInPreCheck())
                return !ChromeDriver.PageSource.ToLower().Contains("utloggad");

            return false;
        }

        public override bool ConfirmPayment()
        {
            var xpathUncheckAll = IsPrivate ? "//*[@id='multiSelectCheckbox']" : "//*[@id='checkChars']/table[7]/tbody/tr[1]/td[1]/input";
            var xpathLast = IsPrivate ? "/html/body/form/table[8]/tbody/tr[position()=last()-1]/td[1]/input" : "//*[@id='checkChars']/table[7]/tbody/tr[position()=last()-1]/td[1]/input";
            //*[@id="checkChars"]/table[8]/tbody/tr[1]/td[1]/input
            var xpathConfirmPaymentButton = IsPrivate ? "//*[@id='ExecuteNewPayment']" : "//*[@id='ModGiroPayment']";
            
            IWebElement checkAllElement = _frame.FindElement(By.XPath(xpathUncheckAll));
            IWebElement checkLastElement = _frame.FindElement(By.XPath(xpathLast));
            
            checkAllElement.Click();
            checkLastElement.Click();

            IWebElement confirmButton = _frame.FindElement(By.XPath(xpathConfirmPaymentButton));
            confirmButton.Click();

            Policy.Handle<Exception>()
                .WaitAndRetry(30, (int x) => TimeSpan.FromSeconds(1))
                .Execute(() =>
                {
                    var element = ChromeDriver.FindElement(By.XPath("/html/body/h1[text()='Kvittens']"));
                });
            return true;
        }

        public override string Authenticate()
        {
            Policy.Handle<Exception>()
                .WaitAndRetry(3, (int x) => TimeSpan.FromSeconds(1))
                .Execute(() =>
                {
                    NavigateToLoginPageUrl();
                    WebDriverWait wait = new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(10));

                    wait.Until(ExpectedConditions.ElementIsVisible((By.ClassName("shb-form-field__input"))));
                    IWebElement textElement = ChromeDriver.FindElement(By.ClassName("shb-form-field__input"));
                    textElement.SendKeys(PersonalNumber);

                    var button = ChromeDriver.FindElement(By.XPath("/html/body/inss-switch-app/div/inss-app/div/main/div[1]/div/div/div[2]/inss-mbidqr/div/div[1]/form/div/div/div/shb-button-primary/div/button"));
                    button.Click();
                    SeleniumHelper.WaitForDocumentReady(ChromeDriver);

                    if (_isMobile)
                    {
                        var xpath = "/html/body/inss-switch-app/div/inss-app/div/main/div[1]/div/div/div[2]/inss-mbidqr/div/div[1]/div[2]/div/div[2]/shb-button-primary/div/button";
                        button = ChromeDriver.FindElement(By.XPath(xpath));
                        button.Click();
                        string url = GetBankIdUrl();
                    }

                    Wait3Seconds.Until(x => x.FindElement(By.ClassName("shb-inss-mbid__qr-container")));
                });


            IWebElement svgDivEl = ChromeDriver.FindElement(By.ClassName("shb-inss-mbid__qr-container"));
            IWebElement element = svgDivEl.FindElement(By.TagName("svg"));
            string contents = (string)((IJavaScriptExecutor)ChromeDriver).ExecuteScript("return arguments[0].innerHTML;", element);
            string svg = $"<svg shape-rendering=\"crispEdges\" height=\"200\" width=\"200\" viewBox=\"0 0 33 33\">{contents}</svg>";

            return svg;
        }

        public List<dynamic> ListPayments()
        {
            var kommandeBetalningarXpath = "/html/body/form/table[8]";
            SeleniumHelper.WaitForDocumentReady(ChromeDriver);
            ChromeDriver.SwitchTo().DefaultContent();
            Wait3Seconds.Until(x => x.FindElement(By.TagName("iframe")));
            var frame = ChromeDriver.SwitchTo().Frame(ChromeDriver.FindElement(By.TagName("iframe")));
            IWebElement kommandeBetalningarTable = frame.FindElement(By.XPath(kommandeBetalningarXpath));
            kommandeBetalningarTable = frame.FindElement(By.XPath(kommandeBetalningarXpath));
            var kommandeBetalningarEfterRows = kommandeBetalningarTable.FindElements(By.TagName("tr")).Where(x => x.FindElements(By.TagName("td")).Count > 0).ToList();
            List<dynamic> tillagdaBetalningar = new List<dynamic>();
            foreach (var item in kommandeBetalningarEfterRows)
            {
                var columns = item.FindElements(By.TagName("td")).Where(x => x.Text != "" && x.Text != " ").ToList();
                if (columns.Count < 5) continue;

                var date = DateTime.Parse(columns[0].Text);
                var reciever = columns[1].Text;
                var account = columns[2].Text;
                var amount = Double.Parse(columns[3].Text);
                var ocrMessage = columns[4].Text;

                tillagdaBetalningar.Add(new
                {
                    Datum = date,
                    Mottagare = reciever,
                    Konto = account,
                    Belopp = amount,
                    Ocr = ocrMessage
                });
            }

            return tillagdaBetalningar;
        }

        public Cookie GetCookie(string name)
        {
            Cookie tokenCookie = _cookies.GetCookieNamed(name);
            if (tokenCookie == null)
            {
                Thread.Sleep(100);
                _numberOfRetries++;
                if (_numberOfRetries < 10)
                    return GetCookie(name);
            }
            return tokenCookie;
        }

        public void Dispose()
        {
            ChromeDriver.Dispose();
        }

        public static SelectElement FindSelectElementWhenPopulated(IWebDriver driver, By by, int delayInSeconds)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(delayInSeconds));
            return wait.Until(drv =>
                {
                    SelectElement element = new SelectElement(drv.FindElement(by));
                    if (element.Options.Count >= 2)
                    {
                        return element;
                    }

                    return null;
                }
            );
        }
    }
}
