using System.Collections.Generic;
using AbstractFactory.Abstract;
using AbstractFactory.Concrete;
using AbstractFactory.Enums;
using Xunit;

namespace Test
{
    public class TestAbstractFactory
    {
        private readonly List<AnimalTypes> _landAnimals = new List<AnimalTypes>()
            {AnimalTypes.Cat, AnimalTypes.Dog, AnimalTypes.Lion};
        
        private readonly List<AnimalTypes> _seaAnimals = new List<AnimalTypes>()
            {AnimalTypes.Octopus, AnimalTypes.Shark};
        
        [Theory]
        [InlineData(FactoryTypes.Land, AnimalTypes.Cat)]
        [InlineData(FactoryTypes.Land, AnimalTypes.Dog)]
        [InlineData(FactoryTypes.Land, AnimalTypes.Lion)]
        [InlineData(FactoryTypes.Land, AnimalTypes.Octopus)]
        [InlineData(FactoryTypes.Land, AnimalTypes.Shark)]
        [InlineData(FactoryTypes.Sea, AnimalTypes.Cat)]
        [InlineData(FactoryTypes.Sea, AnimalTypes.Dog)]
        [InlineData(FactoryTypes.Sea, AnimalTypes.Lion)]
        [InlineData(FactoryTypes.Sea, AnimalTypes.Octopus)]
        [InlineData(FactoryTypes.Sea, AnimalTypes.Shark)]
        public void Test1(FactoryTypes factoryType, AnimalTypes animalType)
        {
            var animalFactory = AnimalFactory.CreateAnimalFactory(factoryType);
            
            Assert.NotNull(animalFactory);
            
            var animal = animalFactory.GetAnimal(animalType);

            if (factoryType == FactoryTypes.Land)
            {
                if (_landAnimals.Contains(animalType))
                {
                    Assert.NotNull(animal);
                    
                    switch (animal)
                    {
                        case Cat cat:
                            Assert.Contains("Meow", cat.Speak());
                            break;
                        case Dog dog:
                            Assert.Contains("Bark", dog.Speak());
                            break;
                        case Lion lion:
                            Assert.Contains("Roar", lion.Speak());
                            break;
                    }
                }   
                else
                    Assert.Null(animal);
            }
            else
            {
                if (_seaAnimals.Contains(animalType))
                {
                    Assert.NotNull(animal);

                    switch (animal)
                    {
                        case Octopus octopus:
                            Assert.Contains("SQUAWCK", octopus.Speak());
                            break;
                        case Shark shark:
                            Assert.Contains("Cannot", shark.Speak());
                            break;
                    }
                }   
                else
                    Assert.Null(animal);
            }
        }
    }
}