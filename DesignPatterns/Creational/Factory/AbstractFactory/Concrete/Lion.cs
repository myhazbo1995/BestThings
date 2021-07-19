using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class Lion : IAnimal
    {
        public string Speak()
        {
            return "Roar";
        }
    }
}