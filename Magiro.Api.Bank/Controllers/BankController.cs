using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Magiro.Api.Bank.Banks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Magiro.Api.Bank.Controllers
{
    public class AuthenticationModel
    {
        public string PersonalNumber { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class BankController : ControllerBase
    {
        private MagiroCache _cache;
        public BankController(MagiroCache cache)
        {
            _cache = cache;
        }

        // GET: api/Bank
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Bank/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Bank
        [HttpPost("Authenticate")]
        public void Authenticate([FromBody] AuthenticationModel model, BankName bankName)
        {
            var bank = Banks.Bank.GetBank(bankName, _cache, model.PersonalNumber);
            bank.Authenticate();
        }

        [HttpGet("BankAccounts")]
        public dynamic BankAccounts()
        {
            int i = 0;
            var bank = _cache.Get(BankName.Handelsbanken);
            if (bank == null || !bank.LoggedInDuringSession) return Unauthorized("Not logged in");
            var accounts = bank.ListFromAccounts().Select(x =>
            {
                dynamic expando = new ExpandoObject();
                expando.Selected = i == 0;
                expando.Text = x.Text;
                expando.Index = i++;
                return expando;
            }).ToList();
            return accounts;
        }
        // PUT: api/Bank/5
        [HttpPut("{id}")]
        public void CreatePayment(int amount, DateTime paymentDate, string bankgiroPostgiro, string ocrMessage, dynamic account)
        {
            var bank = _cache.Get(BankName.Handelsbanken);
            bank.CreatePayment(amount, paymentDate, bankgiroPostgiro, ocrMessage, account);
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
