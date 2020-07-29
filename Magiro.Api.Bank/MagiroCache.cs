using Magiro.Api.Bank.Banks;
using Microsoft.Extensions.Caching.Memory;

namespace Magiro.Api.Bank
{
    public class MagiroCache
    {
        MemoryCache memory = new MemoryCache(new MemoryCacheOptions());

        public Banks.Bank Get(BankName bank, string email = "")
        {
            return (Banks.Bank)memory.Get(email + "_" + bank.ToString());
        }

        public Banks.Bank Set(Banks.Bank bank, BankName bankName, string email = "")
        {
            return memory.Set(email + "_" + bankName, bank);
        }

    }
}
