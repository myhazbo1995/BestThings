using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class Octopus : IAnimal
    {
        public string Speak()
        {
            return "SQUAWCK";
        }
    }
}