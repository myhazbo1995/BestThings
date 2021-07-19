using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class Cat : IAnimal
    {
        public string Speak()
        {
            return "Meow Meow Meow";
        }
    }
}