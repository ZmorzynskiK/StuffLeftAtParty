using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetTemplates
{
    class Program
    {
        public class Order
        {
            public int Id { get; set; }
            public Guid TransactionId { get; set; }
        }

        static void DotLiquidSample()
        {
            string template = "Dear {{name}}, Thank you for purchasing {{product}}.";
            var compiledTemplate = DotLiquid.Template.Parse(template);
            string finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(new { name = "John Wick", product = "Glock 18" }));

            Console.WriteLine(finalContent);

            var order = new Order() { Id = 42, TransactionId = Guid.NewGuid() };
            template = "Order id {{Id}}, Transaction {{TransactionId}}.";
            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject(order));

            Console.WriteLine(finalContent);

            template = "Order id {{placedOrder.Id}}, Transaction {{placedOrder.TransactionId}}.";
            compiledTemplate = DotLiquid.Template.Parse(template);
            finalContent = compiledTemplate.Render(DotLiquid.Hash.FromAnonymousObject( new { placedOrder = order } ));

            Console.WriteLine(finalContent);
        }
        static void Main(string[] args)
        {
            DotLiquidSample();
            Console.ReadLine();
        }
    }
}
