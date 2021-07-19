using FactoryMethod.Interfaces;

namespace FactoryMethod.Abstract
{
    public abstract class CreditCardFactory
    {
        protected abstract ICreditCard MakeProduct();

        public ICreditCard CreateProduct()
        {
            return MakeProduct();
        }
    }
}