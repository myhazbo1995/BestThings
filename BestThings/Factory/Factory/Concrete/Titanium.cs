using Factory.Interfaces;

namespace Factory.Concrete
{
    public class Titanium : ICreditCard
    {
        public string GetCardType()
        {
            return "Titanium Edge";
        }

        public int GetCreditLimit() 
        {
            return 25000;
        }

        public int GetAnnualCharge() 
        {
            return 1500;
        }
    }
}
