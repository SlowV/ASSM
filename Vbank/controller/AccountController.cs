using System;
using System.Collections.Generic;
using System.IO;
using Vbank.entity;
using Vbank.model;
using Vbank.utility;
using Vbank.view;

namespace Vbank.controller
{
    public class AccountController
    {

        private readonly AccountModel _model = new AccountModel();

        public void Register()
        {
            Console.Clear();
            Console.Out.Flush();
            Console.WriteLine("Nhập thông tin tài khoản.");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Tài khoản: ");
            var username = Console.ReadLine();
            Console.WriteLine("Mật khẩu: ");
            var password = Console.ReadLine();
            Console.WriteLine("Nhập lại mật khẩu: ");
            var cpassword = Console.ReadLine();
            Console.WriteLine("Số thẻ căn cước|Chứng minh thư nhân dân: ");
            var identityCard = Console.ReadLine();
            Console.WriteLine("Họ và tên: ");
            var fullName = Console.ReadLine();
            Console.WriteLine("Email: ");
            var email = Console.ReadLine();
            Console.WriteLine("Số điện thoại: ");
            var phone = Console.ReadLine();
            var account = new Account(username, password, cpassword, identityCard, phone, email, fullName);
            var errors = account.CheckValid();
            if (errors.Count == 0 && !_model.CheckExistUserName(username))
            {
                _model.Save(account);
                Console.WriteLine("Đăng ký thành công.");
                Console.WriteLine("--------------------------------");
                Console.WriteLine("Bạn có muốn đăng nhập không? Y/N");
                Console.WriteLine("Vui lòng nhập lựa chọn của bạn:");
                var choice = Console.ReadLine();
                if (choice == "Y")
                {
                    DoLogin();
                    MainView.GenarateMenu();
                }
                else if (choice == "N")
                {
                    MainView.GenarateMenu();
                }
                else if (choice != "Y" && choice != "N")
                {
                    Console.WriteLine("Lựa chọn không hợp lệ, vui lòng thử lại.");
                }

                Console.ReadLine();
            }
            else
            {
                Console.Error.WriteLine("Thông tin tài khoản không hợp lệ, vui lòng thử lại.");
                foreach (var messagErrorsValue in errors.Values)
                {
                    Console.Error.WriteLine(messagErrorsValue);
                }

                Console.ReadLine();
            }
        }

        public Boolean DoLogin()
        {
            Console.Clear();
            Console.Out.Flush();
            // Lấy thông tin từ người dùng nhập vào.
            Console.WriteLine("============= ĐĂNG NHẬP ============");
            Console.WriteLine("TÀI KHOẢN: ");
            var username = Console.ReadLine();
            Console.WriteLine("MẬT KHẨU: ");
            var password = Console.ReadLine();
            var account = new Account(username, password);
            // Bắt đầu kiểm tra Valid user và password length khác null và lớn hơn 0.
            var errors = account.ValidLoginInformation();
            if (errors.Count > 0)
            {
                Console.WriteLine("Vui lòng kiểm tra lại.");
                foreach (var messagErrorsValue in errors.Values)
                {
                    Console.Error.WriteLine(messagErrorsValue);
                }

                Console.ReadLine();
                return false;
            }

            account = _model.GetAccountByUserName(username);
            if (account == null)
            {
                // Sai thông tin username, trả về thông báo lỗi cụ thể.
                Console.WriteLine("Thông tin tài khoản không hợp lệ, vui lòng thử lại.");
                return false;
            }

            // Băm password người dùng nhập vào với muối lấy trên database và so sánh với password trên database.
            if (account.Password != Hash.GenerateSaltedSHA1(password, account.Salt))
            {
                // Sai thông tin password, trả về thông báo lỗi cụ thể.
                Console.WriteLine("Thông tin tài khoản không hợp lệ, vui lòng thử lại.");
                return false;
            }

            // Login thành công, lưu thông tin vào biến static trong lớp Program.
            Program.CurrentLoggedIn = account;
            return true;
        }

        public void WithDraw()
        {
            Console.Clear();
            Console.Out.Flush();
            Program.CurrentLoggedIn = _model.GetAccountByUserName(Program.CurrentLoggedIn.Username);
            Console.WriteLine("Rút tiền. \t \t Số dư của bạn: " + Program.CurrentLoggedIn.Balance);
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Vui lòng nhập số tiền bạn muốn rút: ");
            var amount = Utility.GetUnsignDecimalNumber();
            Console.WriteLine("Lời nhắn: ");
            var content = Console.ReadLine();
//            Program.currentLoggedIn = model.GetAccountByUserName(Program.currentLoggedIn.Username);
            var historyTransaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = Transaction.TransactionType.Withdraw,
                Amount = amount,
                Content = content,
                SenderAccountNumber = Program.CurrentLoggedIn.AccountNumber,
                ReceiverAccountNumber = Program.CurrentLoggedIn.AccountNumber,
                Status = Transaction.ActiveStatus.Done
            };
            Console.WriteLine(_model.UpdateBalance(Program.CurrentLoggedIn, historyTransaction)
                ? "Giao dịch thành công!"
                : "Giao dịch thất bại, vui lòng thử lại!");   
            Program.CurrentLoggedIn = _model.GetAccountByUserName(Program.CurrentLoggedIn.Username);
            Console.WriteLine("Số dư hiện tại: " + Program.CurrentLoggedIn.Balance);
            Console.WriteLine("Ấn enter để tiếp tục!");
            Console.ReadLine();
        }

        public void Deposit()
        {
            Console.Clear();
            Console.Out.Flush();
            Console.WriteLine("Gửi tiền.");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Vui lòng nhập số tiền bạn muốn gửi: ");
            var amount = Utility.GetUnsignDecimalNumber();
            Console.WriteLine("Lời nhắn: ");
            var content = Console.ReadLine();
            var historyTransaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = Transaction.TransactionType.Deposit,
                Amount = amount,
                Content = content,
                SenderAccountNumber = Program.CurrentLoggedIn.AccountNumber,
                ReceiverAccountNumber = Program.CurrentLoggedIn.AccountNumber,
                Status = Transaction.ActiveStatus.Done
            };
            Console.WriteLine(_model.UpdateBalance(Program.CurrentLoggedIn, historyTransaction)
                ? "Giao dịch thành công!"
                : "Giao dịch thất bại, và thử lại lần nữa!");
            Program.CurrentLoggedIn = _model.GetAccountByUserName(Program.CurrentLoggedIn.Username);
            Console.WriteLine("Số tiền hiện tại: " + Program.CurrentLoggedIn.Balance + " vnđ");
            Console.WriteLine("Ấn enter để tiếp tục.");
            Console.ReadLine();
        }

        public void Transfer()
        {
            Console.Clear();
            Console.Out.Flush();
            Program.CurrentLoggedIn = _model.GetAccountByUserName(Program.CurrentLoggedIn.Username);
            Console.WriteLine("-------------------------");
            Console.WriteLine("Số dư của bạn :" + Program.CurrentLoggedIn.Balance + " vnđ");
            Console.WriteLine("-------------------------");
            Console.WriteLine("Vui lòng nhập số tài khoản người nhận.");
            var stk = Console.ReadLine();
            Program.CurrentReceiverAccountNumber = _model.GetAccountWithAccountNumber(stk);
            Console.WriteLine("Thông tin người nhận.");
            Console.WriteLine("----------------------------------");
            Console.WriteLine("Họ tên: " + Program.CurrentReceiverAccountNumber.FullName);
            Console.WriteLine("Số tài khoản: " + Program.CurrentReceiverAccountNumber.AccountNumber);
            Console.WriteLine("------------------------------------");
            Console.WriteLine("Vui lòng nhập số tiền bạn muốn chuyển: ");
            var amount = Utility.GetUnsignDecimalNumber();
            Console.WriteLine("Lời nhắn: ");
            var content = Console.ReadLine();
            var historyTransaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = Transaction.TransactionType.Transfer,
                Amount = amount,
                Content = content,
                SenderAccountNumber = Program.CurrentLoggedIn.AccountNumber,
                ReceiverAccountNumber = Program.CurrentReceiverAccountNumber.AccountNumber,
                Status = Transaction.ActiveStatus.Done
            };
            Console.WriteLine(_model.UpdateBalanceWhenTransfer(historyTransaction)
                ? "Giao dịch thành công!"
                : "Giao dịch thất bại, và thử lại 1 lần nữa!");
            Program.CurrentLoggedIn = _model.GetAccountByUserName(Program.CurrentLoggedIn.Username);
            Console.WriteLine("Số tiền hiện tại: " + Program.CurrentLoggedIn.Balance + " vnđ");
            Console.WriteLine("Ấn enter để tiếp tục.");
            Console.ReadLine();
        }


        public Boolean HistoryTransactionWith10Day()
        {
            Console.Clear();
            Console.Out.Flush();
            var lt = _model.GetTransactionWith10day(Program.CurrentLoggedIn.AccountNumber);
            Program.CurrentAccountTransaction = _model.GetTransactionWith10day(Program.CurrentLoggedIn.AccountNumber);


            if (lt != null)
            {
                if (lt.Count > 0)
                {
                    int processed = 0;
                    Console.Clear();
                    Console.Out.Flush();
                    Console.WriteLine("Tìm thấy {0} kết quả. ", lt.Count);
                    Console.WriteLine("\t \t \t \t \t \t \t \t  DANH SÁCH LỊCH SỬ GIAO DỊCH 10 NGÀY GẦN ĐÂY  ");
                    Console.WriteLine(" ");
                    Console.WriteLine(
                        $"|{"Stt",3}|{"id",5}|{"Ngày tạo",23}|{"Kiểu GD",10}|{"Số tiền",8}|{"Lời nhắn",17}|{"STK người gửi",37}|{"Tên người gửi",15}|{"STK người nhận",37}|{"Tên người nhận",15}|");
                    Console.WriteLine(
                        $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|");

                    foreach (var item in lt)
                    {
                        Program.CurrentReceiverAccountNumber =
                            _model.GetAccountWithAccountNumber(item.ReceiverAccountNumber);
                        Console.WriteLine(
                            $"|{processed + 1,3}|{item.Id,5}|{item.CreatedAt,23}|{item.Type,10}|{item.Amount,8}|{item.Content,17}|{item.SenderAccountNumber,37}|{Program.CurrentLoggedIn.FullName,15}|{item.ReceiverAccountNumber,37}|{Program.CurrentReceiverAccountNumber.FullName,15}|");
                        Console.WriteLine(
                            $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|");
                        if (++processed == 10) break;
                    }

                    var text = "Số lần giao dịch là: " + lt.Count + Environment.NewLine;
                    var text1 = "\t \t \t \t \t \t \t \t \t \t \t \t \t\t\tDANH SÁCH LỊCH SỬ GIAO DỊCH 10 NGÀY GẦN ĐÂY  " +
                                Environment.NewLine;
                    var text2 = " " + Environment.NewLine;
                    var text3 =
                        $"|{"Stt",3}|{"id",5}|{"Ngày tạo",23}|{"Kiểu GD",10}|{"Số tiền",8}|{"Lời nhắn",17}|{"STK người gửi",37}|{"Tên người gửi",15}|{"STK người nhận",37}|{"Tên người nhận",15}|" +
                        Environment.NewLine;
                    var text4 =
                        $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|" +
                        Environment.NewLine;
                    Console.WriteLine("Chức năng: ");
                    Console.WriteLine("1. In file txt. \t \t 2. Xóa lịch sử giao dịch. \t \t 3. Quay lại.");
                    Console.WriteLine("-------------------------------------------------------");
                    Console.WriteLine("Lựa chọn của bạn là: ");
                    var choice = Utility.GetInt32Number();
                    switch (choice)
                    {
                        case 1:
                            Console.Clear();
                            Console.Out.Flush();
                            int _stt = 0;
                            List<string> ltp = new List<string>();
                            string nameFile = string.Format("HistoryTransaction-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt",
                                DateTime.Now);
                            foreach (var item in lt)
                            {
                                Program.CurrentReceiverAccountNumber =
                                    _model.GetAccountWithAccountNumber(item.ReceiverAccountNumber);
                                var stt = ++_stt;
                                var id = item.Id;
                                var createdAt = item.CreatedAt;
                                var type = item.Type;
                                var amount = item.Amount;
                                var content = item.Content;
                                var senderAccountNumber = item.SenderAccountNumber;
                                var senderAccountName = Program.CurrentLoggedIn.FullName;
                                var receiverAccountNumber = item.ReceiverAccountNumber;
                                var receiverAccountName = Program.CurrentReceiverAccountNumber.FullName;
                                var text5 =
                                    $"|{stt,3}|{id,5}|{createdAt,23}|{type,10}|{amount,8}|{content,17}|{senderAccountNumber,37}|{senderAccountName,15}|{receiverAccountNumber,37}|{receiverAccountName,15}|" +
                                    Environment.NewLine;
                                var text6 =
                                    $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|" +
                                    Environment.NewLine;
                                ltp.Add(text5);
                                ltp.Add(text6);
                            }

                            using (var sw = new StreamWriter(nameFile))
                            {
                                sw.Write(text + text1 + text2 + text3 + text4);
                                foreach (var item in ltp)
                                {
                                    sw.WriteLine(item);
                                }
                            }
                            Console.WriteLine("In file với tên " + nameFile + " thành công.");
                            Console.WriteLine("Ấn enter để quay lại....");
                            Console.ReadLine();
                            MainView.GenarateMenu();
                            Console.Clear();
                            Console.Out.Flush();
                            break;
                        case 2:
                            Console.WriteLine("Vui lòng nhìn bảng bên trên và nhập chính xác ID giao dịch bạn muốn xóa.");
                            var idHt = Console.ReadLine();
                            if (_model.CheckExistId(idHt))
                            {
                                if (_model.UpdateTransaction(idHt))
                                {
                                    Console.WriteLine("Xóa thành công.");
                                    Console.WriteLine("Ấn enter để tiếp tục.");
                                    Console.ReadLine();
                                    Console.Clear();
                                    Console.Out.Flush();
                                }
                                else
                                {
                                    Console.WriteLine("Xóa không thành công, vui lòng thử lại.");
                                    Console.ReadLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Vui lòng nhập đúng ID của giao dịch.");
                                Console.ReadLine();
                            }
                            break;
                        case 3:
                            Console.Clear();
                            Console.Out.Flush();
                            MainView.GenarateMenu();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Không tìm thấy giao dịch nào.");
                }
            }
            else
            {
                return false;
            }

            if (lt.Count > 0)
            {
                return true;
            }

            Console.WriteLine("Ấn enter để tiếp tục.");
            Console.ReadLine();
            return false;
        }

        public Boolean HistoryTransactionWithSearchDay()
        {
            Console.Clear();
            Console.Out.Flush();

            Console.WriteLine("Vui Lòng nhập ngày tháng bắt đầu.");
            Console.WriteLine("Ngày: ");
            int daySrt = int.Parse(Console.ReadLine());
            Console.WriteLine("Tháng: ");
            int monthSrt = int.Parse(Console.ReadLine());
            Console.WriteLine("Năm: ");
            int yearSrt = int.Parse(Console.ReadLine());
            Console.WriteLine("Vui lòng nhập ngày kết thúc.");
            Console.WriteLine("Ngày: ");
            int dayE = int.Parse(Console.ReadLine());
            Console.WriteLine("Tháng: ");
            int monthE = int.Parse(Console.ReadLine());
            Console.WriteLine("Năm: ");
            int yearE = int.Parse(Console.ReadLine());

            var dateStart = new DateTime(yearSrt, monthSrt, daySrt);
            var dateEnd = new DateTime(yearE, monthE, dayE);

            var dayNow = DateTime.Now;
            var dateTime = dayNow.AddDays(+1);

            if (dateStart < dateTime && dateEnd < dateTime && dateStart <= dateEnd)
            {
                var lt = _model.GetTransactionWithSearchDateFromDateTo(Program.CurrentLoggedIn.AccountNumber,
                    dateStart.ToString("yyyy-MM-đ HH:mm:ss"),
                    dateEnd.ToString("yyyy-MM-dd HH:mm:ss"));
                if (lt != null)
                {
                    if (lt.Count > 0)
                    {
                        int processed = 0;
                        Console.Clear();
                        Console.Out.Flush();
                        Console.WriteLine("Tim thấy {0} kết quả. ", lt.Count);
                        Console.WriteLine("\t \t \t \t \t \t \t \t  DANH SÁCH LỊCH SỬ GIAO DỊCH 10 NGÀY GẦN ĐÂY  ");
                        Console.WriteLine(" ");
                        Console.WriteLine(
                            $"|{"Stt",3}|{"id",5}|{"Ngày tạo",23}|{"Kiểu GD",10}|{"Số tiền",8}|{"Lời nhắn",17}|{"STK người gửi",37}|{"Tên người gửi",15}|{"STK người nhận",37}|{"Tên người nhận",15}|");
                        Console.WriteLine(
                            $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|");

                        foreach (var item in lt)
                        {
                            Program.CurrentReceiverAccountNumber =
                                _model.GetAccountWithAccountNumber(item.ReceiverAccountNumber);
                            Console.WriteLine(
                                $"|{processed + 1,3}|{item.Id,5}|{item.CreatedAt,23}|{item.Type,10}|{item.Amount,8}|{item.Content,17}|{item.SenderAccountNumber,37}|{Program.CurrentLoggedIn.FullName,15}|{item.ReceiverAccountNumber,37}|{Program.CurrentReceiverAccountNumber.FullName,15}|");
                            Console.WriteLine(
                                $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|");
                        }

                        var text = "Số lần giao dịch là: " + lt.Count + Environment.NewLine;
                        var text1 = "\t \t \t \t \t \t \t \t  \t\t\t DANH SÁCH LỊCH SỬ GIAO DỊCH 10 NGÀY GẦN ĐÂY  " +
                                    Environment.NewLine;
                        var text2 = " " + Environment.NewLine;
                        var text3 =
                            $"|{"Stt",3}|{"id",5}|{"Ngày tạo",23}|{"Kiểu GD",10}|{"Số tiền",8}|{"Lời nhắn",17}|{"STK người gửi",37}|{"Tên người gửi",15}|{"STK người nhận",37}|{"Tên người nhận",15}|" +
                            Environment.NewLine;
                        var text4 =
                            $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|" +
                            Environment.NewLine;
                        Console.WriteLine("Bạn có muốn in lịch sử giao dịch ra file .txt không?");
                        Console.WriteLine("1. Đồng ý." + "\t\t" + "2. Không và quay lại.");
                        Console.WriteLine("-------------------------------------------------------");
                        Console.WriteLine("Lựa chọn của bạn là: ");
                        var choice = Utility.GetInt32Number();
                        Console.Clear();
                        Console.Out.Flush();
                        switch (choice)
                        {
                            case 1:
                                int _stt = 0;
                                List<string> ltp = new List<string>();
                                string nameFile = string.Format("HistoryTransaction-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt",
                                    DateTime.Now);
                                foreach (var item in lt)
                                {
                                    Program.CurrentReceiverAccountNumber =
                                        _model.GetAccountWithAccountNumber(item.ReceiverAccountNumber);
                                    var stt = ++_stt ;
                                    var id = item.Id;
                                    var createdAt = item.CreatedAt;
                                    var type = item.Type;
                                    var amount = item.Amount;
                                    var content = item.Content;
                                    var senderAccountNumber = item.SenderAccountNumber;
                                    var senderAccountName = Program.CurrentLoggedIn.FullName;
                                    var receiverAccountNumber = item.ReceiverAccountNumber;
                                    var receiverAccountName = Program.CurrentReceiverAccountNumber.FullName;
                                    var text5 =
                                        $"|{stt,3}|{id,5}|{createdAt,23}|{type,10}|{amount,8}|{content,17}|{senderAccountNumber,37}|{senderAccountName,15}|{receiverAccountNumber,37}|{receiverAccountName,15}|" +
                                        Environment.NewLine;
                                    var text6 =
                                        $"|{"---",3}|{"-----",5}|{"-----------------------",23}|{"----------",10}|{"--------",8}|{"-----------------",17}|{"-------------------------------------",37}|{"---------------",15}|{"-------------------------------------",37}|{"---------------",15}|" +
                                        Environment.NewLine;
                                    ltp.Add(text5);
                                    ltp.Add(text6);
                                }

                                using (var sw = new StreamWriter(nameFile))
                                {
                                    sw.Write(text + text1 + text2 + text3 + text4);
                                    foreach (var item in ltp)
                                    {
                                        sw.WriteLine(item);
                                    }
                                }
                                Console.WriteLine("In file với tên " + nameFile + " thành công.");
                                Console.WriteLine("Ấn enter để quay lại....");
                                Console.ReadLine();
                                Console.Clear();
                                Console.Out.Flush();
                                MainView.GenarateMenu();
                                break;
                            case 2:
                                Console.WriteLine("Vui lòng nhìn bảng bên trên và nhập chính xác ID giao dịch bạn muốn xóa.");
                                var idHt = Console.ReadLine();
                                if (_model.CheckExistId(idHt))
                                {
                                    if (_model.UpdateTransaction(idHt))
                                    {
                                        Console.WriteLine("Xóa thành công.");
                                        Console.WriteLine("Ấn enter để tiếp tục.");
                                        Console.ReadLine();
                                        Console.Clear();
                                        Console.Out.Flush();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Xóa không thành công, vui lòng thử lại.");
                                        Console.ReadLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Vui lòng nhập đúng ID của giao dịch.");
                                    Console.ReadLine();
                                }
                                break;
                            case 3:
                                Console.Clear();
                                Console.Out.Flush();
                                MainView.GenarateMenu();
                                break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Vui lòng nhập đúng ngày tháng!");
            }

            return false;
        }

        public void CheckBalance()
        {
            Console.Clear();
            Console.Out.Flush();
            Program.CurrentLoggedIn = _model.GetAccountByUserName(Program.CurrentLoggedIn.Username);
            Console.WriteLine("THÔNG TIN TÀI KHOẢN");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Họ tên: " + Program.CurrentLoggedIn.FullName);
            Console.WriteLine("Số tài khoản: " + Program.CurrentLoggedIn.AccountNumber);
            Console.WriteLine("Số tiền: " + Program.CurrentLoggedIn.Balance);
            Console.WriteLine("Ấn enter để tiếp tục.");
            Console.ReadLine();
        }
    }
}