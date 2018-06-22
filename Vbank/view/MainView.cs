using System;
using System.IO;
using System.Net;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Vbank.controller;
using Vbank.utility;

namespace Vbank.view
{
    public class MainView
    {
        private static AccountController controller = new AccountController();

        delegate void DelegateVoid();

        public static void GenarateMenu()
        {
            DelegateVoid myDelegate1 = GenarateMenuInfo;
            DelegateVoid myDelegate2 = GenarateCustomerMenu;
            while (true)
            {
                if (Program.CurrentLoggedIn != null)
                {
                    myDelegate2();
                }
                else
                {
                    myDelegate1();
                }
            }
        }


        // Menu chưa login
        private static void GenarateMenuInfo()
        {
            Console.Clear();
            Console.Out.Flush();
            while (true)
            {
                Console.WriteLine("====****** CHÀO MỪNG ĐẾN VỚI VBANK ******====");
                Console.WriteLine("------------------ * -----------------");
                Console.WriteLine("1. Đăng ký.");
                Console.WriteLine("2. Đăng nhập.");
                Console.WriteLine("3. Thoát.");
                Console.WriteLine("------------------ * -----------------");
                Console.WriteLine("Vui lòng nhập lựa chọn của bạn (1|2|3).");
                var choice = Utility.GetInt32Number();

                switch (choice)
                {
                    case 1:
                        controller.Register();
                        break;
                    case 2:
                        if (controller.DoLogin())
                        {
                            Console.WriteLine("Đăng nhập thành công.");
                            GenarateCustomerMenu();
                        }

                        break;
                    case 3:
                        Console.WriteLine("Tạm biệt!");
                        break;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ.");
                        break;
                }

                if (choice == 3)
                {
                    Environment.Exit(1);
                }
            }
        }


        // menu đã login
        private static void GenarateCustomerMenu()
        {
            Console.Clear();
            Console.Out.Flush();

            while (true)
            {
                Console.WriteLine("------------------ * ------------------");
                Console.WriteLine("\t \t VBANK \t \t");
                Console.WriteLine("Xin chào:" + " " + Program.CurrentLoggedIn?.FullName);
                Console.WriteLine("1. Kiểm tra số dư.");
                Console.WriteLine("2. Rút tiền.");
                Console.WriteLine("3. Gửi tiền.");
                Console.WriteLine("4. Chuyển khoản.");
                Console.WriteLine("5. Lịch sử giao dịch.");
                Console.WriteLine("6. Đăng xuất.");
                Console.WriteLine("Vui lòng nhập lựa chọn của bạn (1|2|3|4|5|6).");
                Console.WriteLine("------------------ * ------------------");
                var choice = Utility.GetInt32Number();
                switch (choice)
                {
                    case 1:
                        controller.CheckBalance();
                        break;
                    case 2:
                        controller.WithDraw();
                        break;
                    case 3:
                        controller.Deposit();
                        break;
                    case 4:
                        controller.Transfer();
                        break;
                    case 5:
                        GenerateTransaction();
                        break;
                    case 6:
                        Program.CurrentLoggedIn = null;
                        GenarateMenuInfo();
                        break;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ.");
                        break;
                }
            }
        }


        // menu tracsaction
        private static void GenerateTransaction()
        {
            Console.Clear();
            Console.Out.Flush();
            while (true)
            {
                Console.WriteLine("------------------ * ------------------");
                Console.WriteLine("\t \t VBANK \t \t");
                Console.WriteLine("------------------ * ------------------");
                Console.WriteLine("1. Lịch sử giao dịch 10 ngày gần nhất.");
                Console.WriteLine("2. Tìm kiếm lịch sử giao dịch theo ngày tháng.");
                Console.WriteLine("3. Quay lại");
                Console.WriteLine("Vui lòng nhập lựa chọn của bạn (1|2|3).");
                Console.WriteLine("------------------ * ------------------");
                var choice = Utility.GetInt32Number();
                switch (choice)
                {
                    case 1:
                        controller.HistoryTransactionWith10Day();
                        if (controller.HistoryTransactionWith10Day())
                        {
                            PrintTransaction();
                        }
                        break;
                    case 2:
                        controller.HistoryTransactionWithSearchDay();
                        if (controller.HistoryTransactionWithSearchDay())
                        {
                            PrintTransaction();
                        }
                        break;
                    case 3:
                        GenarateCustomerMenu();
                        break;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ!");
                        break;
                }
            }
        }

        public static void PrintTransaction()
        {
            while (true)
            {
                Console.WriteLine("Bạn có muốn in lịch sử giao dịch ra file .txt không?");
                Console.WriteLine("1. Đồng ý.");
                Console.WriteLine("2. Không và quay lại.");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine("Lựa chọn của bạn là: ");
                var choice = Utility.GetInt32Number();
                switch (choice)
                {
                    case 1:
                        Console.WriteLine("Chức năng đang phát triển.");
                        break;
                    case 2:
                        Console.Clear();
                        Console.Out.Flush();
                        GenerateTransaction();
                        break;
                }
            }
        }
    }
}