using System;
using System.Runtime.InteropServices.ComTypes;
using Vbank.controller;

namespace Vbank.utility
{
    public class Utility
    {
        private static AccountController controller = new AccountController(); 
        delegate void DeledateVoid();
        // Đảm bảo người dùng nhập số.
        public static decimal GetUnsignDecimalNumber()
        {
            decimal choice;
            while (true)
            {
                try
                {
                    var strChoice = Console.ReadLine();
                    choice = Decimal.Parse(strChoice);
                    if (choice <= 0)
                    {
                        throw new FormatException();
                    }
                    else
                    {
                        break;
                    }
                }
                catch (FormatException e)
                {
                    Console.WriteLine("Please enter a unsign number.");
                }
            }

            return choice;
        }

        public static int GetInt32Number()
        {
            var choice = 0;
            while (true)
            {
                try
                {
                    var strChoice = Console.ReadLine();
                    choice = Int32.Parse(strChoice);
                    break;
                }
                catch (FormatException e)
                {
                    Console.WriteLine("Vui lòng nhập 1 số.");
                }
            }

            return choice;
        }

        public static int GetInt32WithDeledate()
        {
            DeledateVoid deledateVoid;
            var choice = 0;
            while (true)
            {
                try
                {
                    var strChoice = Console.ReadLine();
                    choice = Int32.Parse(strChoice);
                    if (choice == 1)
                    {
                        deledateVoid = controller.CheckBalance;
                    }
                    break;
                }
                catch (FormatException e)
                {
                   Console.WriteLine("Vui lòng nhập 1 số.");
                }
            }
            return choice;
        }
    }
}