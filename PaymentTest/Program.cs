﻿using System;
using System.Threading.Tasks;

namespace PaymentTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {

            Process().Wait();

            Console.ReadLine();
        }

        public static async Task Process()
        {
            var processor = new PaymentProcessor("COM6");

            Console.WriteLine("Welcome to Pagador 9000");
            Console.WriteLine("Initializing...");

            await processor.Initialize();

            Console.Write("Amount: ");
            await processor.Pay(Int32.Parse(Console.ReadLine()));

           // Console.WriteLine("Created transaction {0}.", transaction.Id);
        }
    }
}
