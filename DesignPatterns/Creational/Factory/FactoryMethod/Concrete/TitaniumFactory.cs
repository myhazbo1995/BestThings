using FactoryMethod.Abstract;
using FactoryMethod.Interfaces;

namespace FactoryMethod.Concrete
{
    public class TitaniumFactory : CreditCardFactory
    {
        protected override ICreditCard MakeProduct()
        {
            ICreditCard product = new Titanium();
            return product;
        }
    }
}