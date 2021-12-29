using FactoryMethod.Abstract;
using FactoryMethod.Interfaces;

namespace FactoryMethod.Concrete
{
    public class MoneyBackFactory : CreditCardFactory
    {
        protected override ICreditCard MakeProduct()
        {
            ICreditCard product = new MoneyBack();
            return product;
        }
    }
}