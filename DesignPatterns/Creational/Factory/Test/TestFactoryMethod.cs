using FactoryMethod.Concrete;
using FactoryMethod.Interfaces;
using Xunit;

namespace Test
{
    public class TestFactoryMethod
    {
        [Theory]
        [InlineData(null)]
        [InlineData("MoneyBack")]
        [InlineData("Titanium")]
        [InlineData("Platinum")]
        public void Test1(string cardType)
        {
            ICreditCard cardDetails = cardType switch
            {
                //Based of the CreditCard Type we are creating the
                //appropriate type instance using if else condition
                "MoneyBack" => new MoneyBackFactory().CreateProduct(),
                "Titanium" => new TitaniumFactory().CreateProduct(),
                "Platinum" => new PlatinumFactory().CreateProduct(),
                _ => null
            };
            
            if (string.IsNullOrEmpty(cardType))
                Assert.True(cardDetails is null);
            else
            {
                Assert.False(cardDetails is null);
                Assert.Contains(cardType.ToLower(), cardDetails.GetCardType().ToLower());
            }   
        }
    }
}