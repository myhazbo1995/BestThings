using AbstractFactory.Concrete;
using AbstractFactory.Enums;
using AbstractFactory.Interfaces;

namespace AbstractFactory.Abstract
{
    public abstract class AnimalFactory
    {
        public abstract IAnimal GetAnimal(AnimalTypes animalType);
        
        public static AnimalFactory CreateAnimalFactory(FactoryTypes factoryType)
        {
            if (factoryType == FactoryTypes.Sea)
                return new SeaAnimalFactory();
            else
                return new LandAnimalFactory();
        }
    }
}