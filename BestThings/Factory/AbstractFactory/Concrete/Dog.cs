using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class Dog : IAnimal
    {
        public string Speak()
        {
            return "Bark bark";
        }
    }
}