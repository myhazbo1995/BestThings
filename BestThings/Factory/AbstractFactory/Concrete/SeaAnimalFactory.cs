using AbstractFactory.Abstract;
using AbstractFactory.Enums;
using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class SeaAnimalFactory : AnimalFactory
    {
        public override IAnimal GetAnimal(AnimalTypes animalType)
        {
            return animalType switch
            {
                AnimalTypes.Shark => new Shark(),
                AnimalTypes.Octopus => new Octopus(),
                _ => null
            };
        }
    }
}