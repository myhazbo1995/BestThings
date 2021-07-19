using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class Shark : IAnimal
    {
        public string Speak()
        {
            return "Cannot Speak";
        }
    }
}