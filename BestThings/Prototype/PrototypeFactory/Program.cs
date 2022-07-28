using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace PrototypeFactory
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Serializable attribute must be present
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(this T self)
        {
            using (var stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, self);
                stream.Seek(0, SeekOrigin.Begin);
                object copy = formatter.Deserialize(stream);
                return (T)copy;
            }
        }

        /// <summary>
        /// Doesn't work with Dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static T DeepCopyXml<T>(this T self)
        {
            using (var ms = new MemoryStream())
            {
                XmlSerializer s = new XmlSerializer(typeof(T));
                s.Serialize(ms, self);
                ms.Position = 0;
                return (T)s.Deserialize(ms);
            }
        }
    }

    [Serializable]
    public class Address
    {
        public string StreetAddress, City;
        public int Suite;

        public Address(string streetAddress, string city, int suite)
        {
            StreetAddress = streetAddress;
            City = city;
            Suite = suite;
        }

        public Address(Address other)
        {
            StreetAddress = other.StreetAddress;
            City = other.City;
            Suite = other.Suite;
        }

        public override string ToString()
        {
            return $"{nameof(StreetAddress)}: {StreetAddress}, {nameof(City)}: {City}, {nameof(Suite)}: {Suite}";
        }
    }

    [Serializable]
    public partial class Employee
    {
        public string Name;
        public Address Address;

        public Employee(string name, Address address)
        {
            Name = name ?? throw new ArgumentNullException(paramName: nameof(name));
            Address = address ?? throw new ArgumentNullException(paramName: nameof(address));
        }

        public Employee(Employee other)
        {
            Name = other.Name;
            Address = new Address(other.Address);
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Address)}: {Address.ToString()}";
        }
    }

    public class EmployeeFactory
    {
        private static Employee main =
          new Employee("Default", new Address("123 East Dr", "London", 0));
        private static Employee aux =
          new Employee("Default", new Address("123B East Dr", "Chicago", 0));

        private static Employee NewEmployee(Employee proto, string name, int suite)
        {
            var copy = proto.DeepCopy();
            copy.Name = name;
            copy.Address.Suite = suite;
            return copy;
        }

        public static Employee NewMainOfficeEmployee(string name, int suite) =>
          NewEmployee(main, name, suite);

        public static Employee NewAuxOfficeEmployee(string name, int suite) =>
          NewEmployee(aux, name, suite);
    }

    public class Program
    {

        static void Main(string[] args)
        {
            var john = EmployeeFactory.NewMainOfficeEmployee("John", 123);
            var jane = EmployeeFactory.NewAuxOfficeEmployee("Jane", 333);

            Console.WriteLine(john);
            Console.WriteLine(jane);

            Console.ReadLine();
        }
    }
}
