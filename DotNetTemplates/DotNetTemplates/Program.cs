using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetTemplates
{
    public class SafeTypeTest
    {
        public class Order
        {
            public int Id { get; set; }
            public Guid TransactionId { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
        }
    }

    public class SafeTypeDropTest
    {
        public class Order
        {
            public int Id { get; set; }
            public Guid TransactionId { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
        }

        public class DotLiquidDropProxy : DotLiquid.DropProxy
        {
            public DotLiquidDropProxy(object obj)
                : base(obj, obj.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public).Select(y => y.Name).ToArray())
            {

            }
        }
    }

    public class PropertyTraversingTest
    {
        public class Address
        {
            public string AddressLine { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
            public Address Address { get; set; }
        }

        public class DotLiquidDropProxy : DotLiquid.DropProxy
        {
            public DotLiquidDropProxy(object obj)
                : base(obj, obj.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public).Select(y => y.Name).ToArray())
            {

            }
        }
    }

    class Program
    {
       
        static void DotLiquidSample()
        {
            string template = "Dear {{name}}, Thank you for purchasing {{product}}.";
            var compiledTemplate = DotLiquid.Template.Parse(template);
            string finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { name = "John Wick", product = "Glock 18" }));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");

            var order = new SafeTypeTest.Order() { Id = 42, TransactionId = Guid.NewGuid() };
            template = "Order id {{Id}}, Transaction {{TransactionId}}.";
            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(order));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");

            template = "User {{buyer.Name}} placed order id {{placedOrder.Id}}, transaction {{placedOrder.TransactionId}}.";

            var user = new SafeTypeTest.User() { Name = "John McClane" };
            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject( new { buyer = user, placedOrder = order } ));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");

            // use this registrations:
            DotLiquid.Template.RegisterSafeType(typeof(SafeTypeTest.Order), typeof(SafeTypeTest.Order).GetProperties().Select(x => x.Name).ToArray());
            DotLiquid.Template.RegisterSafeType(typeof(SafeTypeTest.User), typeof(SafeTypeTest.User).GetProperties().Select(x => x.Name).ToArray());

            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { buyer = user, placedOrder = order }));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");

            DotLiquid.Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { buyer = user, placedOrder = order }));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");

            // or this:
            DotLiquid.Template.RegisterSafeType(typeof(SafeTypeDropTest.Order), obj => new SafeTypeDropTest.DotLiquidDropProxy(obj) );
            DotLiquid.Template.RegisterSafeType(typeof(SafeTypeDropTest.User), obj => new SafeTypeDropTest.DotLiquidDropProxy(obj));

            var order2 = new SafeTypeDropTest.Order() { Id = 42, TransactionId = Guid.NewGuid() };
            var user2 = new SafeTypeDropTest.User() { Name = "John McClane" };

            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { buyer = user2, placedOrder = order2 }));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");


            DotLiquid.Template.RegisterSafeType(typeof(PropertyTraversingTest.Address), obj => new PropertyTraversingTest.DotLiquidDropProxy(obj));
            DotLiquid.Template.RegisterSafeType(typeof(PropertyTraversingTest.User), obj => new SafeTypeDropTest.DotLiquidDropProxy(obj));

            var address = new PropertyTraversingTest.Address() { AddressLine = "Astana, Kazakhstan" };
            var user3 = new PropertyTraversingTest.User() { Name = "John McClane", Address = address };

            template = "User {{buyer.Name}} address {{buyer.Address.AddressLine}}.";
            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { buyer = user3 }));

            Console.WriteLine(finalContent);
            Console.WriteLine("----------");

        }
        static void Main(string[] args)
        {
            DotLiquidSample();
            Console.ReadLine();
        }
    }
}
