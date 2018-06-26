using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Vbank.entity;
using Vbank.utility;
using Vbank.view;

namespace Vbank
{
    class Program
    {
        public static Account CurrentLoggedIn;
        public static Account CurrentReceiverAccountNumber;

        static void Main(string[] args)
        {
            Console.Clear();
            Console.Out.Flush();
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            MainView.GenarateMenu();
        }
    }
}