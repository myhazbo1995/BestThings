using FactoryMethod.Abstract;
using FactoryMethod.Interfaces;

namespace FactoryMethod.Concrete
{
    public class PlatinumFactory : CreditCardFactory
    {
        protected override ICreditCard MakeProduct()
        {
            ICreditCard product = new Platinum();
            return product;
        }
    }
}