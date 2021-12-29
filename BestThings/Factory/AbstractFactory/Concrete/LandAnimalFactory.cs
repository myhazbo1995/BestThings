using AbstractFactory.Abstract;
using AbstractFactory.Enums;
using AbstractFactory.Interfaces;

namespace AbstractFactory.Concrete
{
    public class LandAnimalFactory : AnimalFactory
    {
        public override IAnimal GetAnimal(AnimalTypes animalType)
        {
            return animalType switch
            {
                AnimalTypes.Cat => new Cat(),
                AnimalTypes.Lion => new Lion(),
                AnimalTypes.Dog => new Dog(),
                _ => null
            };
        }
    }
}