using Factory.Concrete;
using Factory.Interfaces;

namespace Factory.Static
{
    public static class CreditCardFactory
    {
        public static ICreditCard GetCreditCard(string cardType)
        {
            ICreditCard cardDetails = cardType switch
            {
                "MoneyBack" => new MoneyBack(),
                "Titanium" => new Titanium(),
                "Platinum" => new Platinum(),
                _ => null
            };

            return cardDetails;
        }
    }
}