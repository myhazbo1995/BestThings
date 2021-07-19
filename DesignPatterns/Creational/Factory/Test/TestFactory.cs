using Factory.Concrete;
using Factory.Interfaces;
using Xunit;

namespace Test
{
    public class TestFactory
    {
        [Theory]
        [InlineData(null)]
        [InlineData("MoneyBack")]
        [InlineData("Titanium")]
        [InlineData("Platinum")]
        public void Test(string cardType)
        {
            ICreditCard cardDetails = cardType switch
            {
                //Based of the CreditCard Type we are creating the
                //appropriate type instance using if else condition
                "MoneyBack" => new MoneyBack(),
                "Titanium" => new Titanium(),
                "Platinum" => new Platinum(),
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