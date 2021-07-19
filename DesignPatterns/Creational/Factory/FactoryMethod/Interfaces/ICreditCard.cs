namespace FactoryMethod.Interfaces
{
    public interface ICreditCard
    {
        string GetCardType();

        int GetCreditLimit();

        int GetAnnualCharge();
    }
}