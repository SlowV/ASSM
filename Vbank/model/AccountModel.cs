using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Cms;
using Vbank.entity;
using Vbank.error;
using Vbank.utility;

namespace Vbank.model
{
    public class AccountModel
    {
        public Boolean Save(Account account)
        {
            DbConnection.Instance().OpenConnection(); // đảm bảo rằng đã kết nối đến db thành công.
            var salt = Hash.RandomString(7); // sinh ra chuỗi muối random.
            account.Salt = salt; // đưa muối vào thuộc tính của account để lưu vào database.
            // mã hoá password của người dùng kèm theo muối, set thuộc tính password mới.
            account.Password = Hash.GenerateSaltedSHA1(account.Password, account.Salt);
            var sqlQuery = "insert into `accounts` " +
                           "(`username`, `password`, `accountNumber`, `identityCard`, `balance`, `phone`, `email`, `fullName`, `salt`, `status`) values" +
                           "(@username, @password, @accountNumber, @identityCard, @balance, @phone, @email, @fullName, @salt, @status)";
            var cmd = new MySqlCommand(sqlQuery, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@username", account.Username);
            cmd.Parameters.AddWithValue("@password", account.Password);
            cmd.Parameters.AddWithValue("@accountNumber", account.AccountNumber);
            cmd.Parameters.AddWithValue("@identityCard", account.IdentityCard);
            cmd.Parameters.AddWithValue("@balance", account.Balance);
            cmd.Parameters.AddWithValue("@phone", account.Phone);
            cmd.Parameters.AddWithValue("@email", account.Email);
            cmd.Parameters.AddWithValue("@fullName", account.FullName);
            cmd.Parameters.AddWithValue("@salt", account.Salt);
            cmd.Parameters.AddWithValue("@status", Account.ActiveStatus.Active);
            var result = cmd.ExecuteNonQuery();
            DbConnection.Instance().CloseConnection();
            return result == 1;
        }

        public bool UpdateBalance(Account account, Transaction historyTransaction)
        {
            DbConnection.Instance().OpenConnection(); // đảm bảo rằng đã kết nối đến db thành công.
            var transaction = DbConnection.Instance().Connection.BeginTransaction(); // Khởi tạo transaction.

            try
            {
                /**
                 * 1. Lấy thông tin số dư mới nhất của tài khoản.
                 * 2. Kiểm tra kiểu transaction. Chỉ chấp nhận deposit và withdraw.
                 *     2.1. Kiểm tra số tiền rút nếu kiểu transaction là withdraw.                 
                 * 3. Update số dư vào tài khoản.
                 *     3.1. Tính toán lại số tiền trong tài khoản.
                 *     3.2. Update số tiền vào database.
                 * 4. Lưu thông tin transaction vào bảng transaction.
                 */

                // 1. Lấy thông tin số dư mới nhất của tài khoản.
                var queryBalance = "select balance from `accounts` where username = @username and status = @status";
                MySqlCommand queryBalanceCommand = new MySqlCommand(queryBalance, DbConnection.Instance().Connection);
                queryBalanceCommand.Parameters.AddWithValue("@username", account.Username);
                queryBalanceCommand.Parameters.AddWithValue("@status", account.Status);
                var balanceReader = queryBalanceCommand.ExecuteReader();
                // Không tìm thấy tài khoản tương ứng, throw lỗi.
                if (!balanceReader.Read())
                {
                    // Không tồn tại bản ghi tương ứng, lập tức rollback transaction, trả về false.
                    // Hàm dừng tại đây.
                    throw new VbankError("Username không hợp lệ.");
                }

                // Đảm bảo sẽ có bản ghi.
                var currentBalance = balanceReader.GetDecimal("balance");
                balanceReader.Close();

                // 2. Kiểm tra kiểu transaction. Chỉ chấp nhận deposit và withdraw. 
                if (historyTransaction.Type != Transaction.TransactionType.Deposit
                    && historyTransaction.Type != Transaction.TransactionType.Withdraw)
                {
                    throw new VbankError("Kiểu giao dịch không hợp lệ.");
                }

                // 2.1. Kiểm tra số tiền rút nếu kiểu transaction là withdraw.
                if (historyTransaction.Type == Transaction.TransactionType.Withdraw &&
                    historyTransaction.Amount > currentBalance)
                {
                    throw new VbankError("Số tiền bạn rút lớn hơn số tiền còn trong tài khoản, vui lòng thử lại!");
                }

                // 3. Update số dư vào tài khoản.
                // 3.1. Tính toán lại số tiền trong tài khoản.
                if (historyTransaction.Type == Transaction.TransactionType.Deposit)
                {
                    currentBalance += historyTransaction.Amount;
                }
                else
                {
                    currentBalance -= historyTransaction.Amount;
                }

                // 3.2. Update số dư vào database.
                var queryUpdateAccountBalance =
                    "update `accounts` set balance = @balance where username = @username and status = 1";
                var cmdUpdateAccountBalance =
                    new MySqlCommand(queryUpdateAccountBalance, DbConnection.Instance().Connection);
                cmdUpdateAccountBalance.Parameters.AddWithValue("@username", account.Username);
                cmdUpdateAccountBalance.Parameters.AddWithValue("@balance", currentBalance);
                var updateAccountResult = cmdUpdateAccountBalance.ExecuteNonQuery();

                // 4. Lưu thông tin transaction vào bảng transaction.
                var queryInsertTransaction = "insert into `transactions` " +
                                             "(id, type, amount, content, senderAccountNumber, receiverAccountNumber, status) " +
                                             "values (@id, @type, @amount, @content, @senderAccountNumber, @receiverAccountNumber, @status)";
                var cmdInsertTransaction =
                    new MySqlCommand(queryInsertTransaction, DbConnection.Instance().Connection);
                cmdInsertTransaction.Parameters.AddWithValue("@id", historyTransaction.Id);
                cmdInsertTransaction.Parameters.AddWithValue("@type", historyTransaction.Type);
                cmdInsertTransaction.Parameters.AddWithValue("@amount", historyTransaction.Amount);
                cmdInsertTransaction.Parameters.AddWithValue("@content", historyTransaction.Content);
                cmdInsertTransaction.Parameters.AddWithValue("@senderAccountNumber",
                    historyTransaction.SenderAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@receiverAccountNumber",
                    historyTransaction.ReceiverAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@status", historyTransaction.Status);
                var insertTransactionResult = cmdInsertTransaction.ExecuteNonQuery();

                if (updateAccountResult == 1 && insertTransactionResult == 1)
                {
                    transaction.Commit();
                    return true;
                }
            }
            catch (VbankError)
            {
                transaction.Rollback();
                return false;
            }

            DbConnection.Instance().CloseConnection();
            return false;
        }

        public Boolean CheckExistUserName(string username)
        {
            DbConnection.Instance().OpenConnection();
            var queryUser = "select * from `accounts` where username = @username and status = 1";
            var cmd = new MySqlCommand(queryUser, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@username", username);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                DbConnection.Instance().CloseConnection();
                return true;
            }

            DbConnection.Instance().CloseConnection();
            return false;
        }

        public Account GetAccountByUserName(string username)
        {
            DbConnection.Instance().OpenConnection();
            var queryString = "select * from  `accounts` where username = @username and status = 1";
            var cmd = new MySqlCommand(queryString, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@username", username);
            var reader = cmd.ExecuteReader();
            Account account = null;
            if (reader.Read())
            {
                var username2 = reader.GetString("username");
                var password = reader.GetString("password");
                var salt = reader.GetString("salt");
                var accountNumber = reader.GetString("accountNumber");
                var identityCard = reader.GetString("identityCard");
                var balance = reader.GetDecimal("balance");
                var phone = reader.GetString("phone");
                var email = reader.GetString("email");
                var fullName = reader.GetString("fullName");
                var createdAt = reader.GetString("createAt");
                var updatedAt = reader.GetString("updateAt");
                var status = reader.GetInt32("status");
                account = new Account(username2, password, salt, accountNumber, identityCard, balance, phone, email,
                    fullName, createdAt, updatedAt, (Account.ActiveStatus) status);
            }

            DbConnection.Instance().CloseConnection();
            return account;
        }

        public Account GetAccountWithAccountNumber(string accountNumber)
        {
            DbConnection.Instance().OpenConnection();
            var queryString = "select * from `accounts` where accountNumber = @accountNumber and status = 1";
            var cmd = new MySqlCommand(queryString, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
            var reader = cmd.ExecuteReader();
            Account account = null;
            if (reader.Read())
            {
                var username = reader.GetString("username");
                var password = reader.GetString("password");
                var salt = reader.GetString("salt");
                var accountNumber2 = reader.GetString("accountNumber");
                var identityCard = reader.GetString("identityCard");
                var balance = reader.GetDecimal("balance");
                var phone = reader.GetString("phone");
                var email = reader.GetString("email");
                var fullName = reader.GetString("fullName");
                var createdAt = reader.GetString("createAt");
                var updatedAt = reader.GetString("updateAt");
                var status = reader.GetInt32("status");
                account = new Account(username, password, salt, accountNumber2, identityCard, balance, phone, email,
                    fullName, createdAt, updatedAt, (Account.ActiveStatus) status);
            }

            reader.Close();
            DbConnection.Instance().CloseConnection();
            return account;
        }

        public bool UpdateBalanceWhenTransfer(Transaction historyTransaction)
        {
            /*
             * 1. Kiểm tra số dư mới nhất.
             *     1.1 Người gửi.
             *     1.2 Người nhận.
             * 2. Kiểm tra kiểu transaction type chỉ nhận type là transfer.
             * 3. Tính toán lại số tiền của người gửi và nhận.
             *     3.1 Update số dư người nhận người gửi lên database.
             *         3.1.1 Người gửi.
             *         3.1.2 Người nhận.
             * 4. Lưu transaction vào bảng transaction trên database
             */
            DbConnection.Instance().OpenConnection(); // Đảm bảo rằng đã kết nối đến db thành công.
            var transaction = DbConnection.Instance().Connection.BeginTransaction(); // Khởi tạo transaction.

            // 1. Kiểm tra số dư mới nhất.
            try
            {
                // 1.1 Người gửi.
                var queryBalanceAccountFrom =
                    "select balance from `accounts` where username = @usernameFrom and status = @statusFrom";
                var queryBalanceCommand =
                    new MySqlCommand(queryBalanceAccountFrom, DbConnection.Instance().Connection);
                queryBalanceCommand.Parameters.AddWithValue("@usernameFrom", Program.CurrentLoggedIn.Username);
                queryBalanceCommand.Parameters.AddWithValue("@statusFrom", Program.CurrentLoggedIn.Status);
                var balanceAccountFrom = queryBalanceCommand.ExecuteReader();
                if (!balanceAccountFrom.Read())
                {
                    // Không tồn tại bản ghi tương ứng, lập tức rollback transaction, trả về false.
                    // Hàm dừng tại đây.
                    throw new VbankError("Username không hợp lệ.");
                }

                // Lấy balance hiện tại của ng gửi.
                var currentBalanceAccountFrom = balanceAccountFrom.GetDecimal("balance");
                balanceAccountFrom.Close();

                // 1.2 Người nhận.
                var queryBalanceAccountTo =
                    "select balance from `accounts` where username = @usernameTo and status = @statusTo";
                var cmd = new MySqlCommand(queryBalanceAccountTo, DbConnection.Instance().Connection);
                cmd.Parameters.AddWithValue("@usernameTo", Program.CurrentReceiverAccountNumber.Username);
                cmd.Parameters.AddWithValue("@statusTo", Program.CurrentReceiverAccountNumber.Status);
                var balanceAccountTo = cmd.ExecuteReader();
                if (!balanceAccountTo.Read())
                {
                    // Không tồn tại bản ghi tương ứng, lập tức rollback transaction, trả về false.
                    // Hàm dừng tại đây.
                    throw new VbankError("Username không hợp lệ.");
                }

                // Lấy balance của người nhận.
                var currentBalanceAccountTo = balanceAccountTo.GetDecimal("balance");
                balanceAccountTo.Close();

                // 2. Kiểm tra kiểu transaction type chỉ nhận type là transfer.
                if (historyTransaction.Type != Transaction.TransactionType.Transfer)
                {
                    throw new VbankError("Kiểu giao dịch không hợp lệ!");
                }

                if (historyTransaction.Type == Transaction.TransactionType.Transfer &&
                    historyTransaction.Amount > currentBalanceAccountFrom)
                {
                    throw new VbankError(
                        "Số tiền bạn muốn chuyển lớn hơn số tiền còn trong tài khoản của bạn, vui lòng thử lại.");
                }

                // 3. Tính toán lại số tiền của người gửi và nhận.
                if (historyTransaction.Type == Transaction.TransactionType.Transfer)
                {
                    currentBalanceAccountFrom -= historyTransaction.Amount;
                    currentBalanceAccountTo += historyTransaction.Amount;
                }

                // 3.1 Update số dư người nhận người gửi lên database.
                // 3.1.1 Người gửi.
                var queryUpdateBalanceAccountFrom =
                    "update `accounts` set balance = @balanceFrom where username = @usernameFrom and status = 1";
                var cmdUpdateAccountBalanceFrom =
                    new MySqlCommand(queryUpdateBalanceAccountFrom, DbConnection.Instance().Connection);
                cmdUpdateAccountBalanceFrom.Parameters.AddWithValue("@usernameFrom", Program.CurrentLoggedIn.Username);
                cmdUpdateAccountBalanceFrom.Parameters.AddWithValue("@balanceFrom", currentBalanceAccountFrom);
                var updateAccountFromResult = cmdUpdateAccountBalanceFrom.ExecuteNonQuery();

                // 3.1.2 Người nhận.
                var queryUpdateBalanceAccountTo =
                    "update `accounts` set balance = @balanceTo where username = @usernameTo and status = 1";
                var cmdUpdateBalanceAccountTo =
                    new MySqlCommand(queryUpdateBalanceAccountTo, DbConnection.Instance().Connection);
                cmdUpdateBalanceAccountTo.Parameters.AddWithValue("@usernameTo",
                    Program.CurrentReceiverAccountNumber.Username);
                cmdUpdateBalanceAccountTo.Parameters.AddWithValue("@balanceTo", currentBalanceAccountTo);
                var updateAccountToResult = cmdUpdateBalanceAccountTo.ExecuteNonQuery();

                // 4. Lưu thông tin transaction vào bảng transaction.
                var queryInsertTransaction = "insert into `transactions` " +
                                             "(id, type, amount, content, senderAccountNumber, receiverAccountNumber, status) " +
                                             "values (@id, @type, @amount, @content, @senderAccountNumber, @receiverAccountNumber, @status)";
                var cmdInsertTransaction =
                    new MySqlCommand(queryInsertTransaction, DbConnection.Instance().Connection);
                cmdInsertTransaction.Parameters.AddWithValue("@id", historyTransaction.Id);
                cmdInsertTransaction.Parameters.AddWithValue("@type", historyTransaction.Type);
                cmdInsertTransaction.Parameters.AddWithValue("@amount", historyTransaction.Amount);
                cmdInsertTransaction.Parameters.AddWithValue("@content", historyTransaction.Content);
                cmdInsertTransaction.Parameters.AddWithValue("@senderAccountNumber",
                    historyTransaction.SenderAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@receiverAccountNumber",
                    historyTransaction.ReceiverAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@status", historyTransaction.Status);
                var insertTransactionResult = cmdInsertTransaction.ExecuteNonQuery();

                if (updateAccountFromResult == 1 && updateAccountToResult == 1 && insertTransactionResult == 1)
                {
                    transaction.Commit();
                    return true;
                }

                DbConnection.Instance().CloseConnection();
                return false;
            }
            catch (VbankError)
            {
                transaction.Rollback();
                return false;
            }
        }

        public List<Transaction> GetTransactionWith10day(string senderAccountNumber)
        {
            DbConnection.Instance().OpenConnection();
            var date = DateTime.Now;
            date = date.AddDays(-10);
            var lt = new List<Transaction>();
            Transaction transaction;
            var queryString =
                "select * from `transactions` where senderAccountNumber = @senderAccountNumber and status = 2 and createAt > @createAt";
            var cmd = new MySqlCommand(queryString, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@senderAccountNumber", senderAccountNumber);
            cmd.Parameters.AddWithValue("@createAt", date);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetString("id");
                var createdAt = reader.GetString("createAt");
                var updatedAt = reader.GetString("updateAt");
                var type = reader.GetInt32("type");
                var amount = reader.GetDecimal("amount");
                var content = reader.GetString("content");
                var senderAccountNumber2 = reader.GetString("senderAccountNumber");
                var receiverAccountNumber = reader.GetString("receiverAccountNumber");
                var status = reader.GetInt32("status");
                transaction = new Transaction(id, createdAt, updatedAt, (Transaction.TransactionType) type, amount,
                    content, senderAccountNumber2, receiverAccountNumber, (Transaction.ActiveStatus) status);
                lt.Add(transaction);
            }

            reader.Close();
            if (lt.Count > 0)
            {
                return lt;
            }

            DbConnection.Instance().CloseConnection();
            return null;
        }

        public List<Transaction> GetTransactionWithSearchDateFromDateTo(string senderAccountNumber, string dateStart,
            string dateEnd)
        {
            DbConnection.Instance().OpenConnection();
            var lt = new List<Transaction>();

            var queryString =
                "select * from `transactions` where senderAccountNumber = @senderAccountNumber and status = 2 and createAt between @dateStart and @dateEnd";
            var cmd = new MySqlCommand(queryString, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@senderAccountNumber", senderAccountNumber);
            cmd.Parameters.AddWithValue("@dateStart", dateStart);
            cmd.Parameters.AddWithValue("@dateEnd", dateEnd);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var id = reader.GetString("id");
                var createdAt = reader.GetString("createAt");
                var updatedAt = reader.GetString("updateAt");
                var type = reader.GetInt32("type");
                var amount = reader.GetDecimal("amount");
                var content = reader.GetString("content");
                var senderAccountNumber2 = reader.GetString("senderAccountNumber");
                var receiverAccountNumber = reader.GetString("receiverAccountNumber");
                var status = reader.GetInt32("status");
                var transaction = new Transaction(id, createdAt, updatedAt, (Transaction.TransactionType) type, amount,
                    content, senderAccountNumber2, receiverAccountNumber, (Transaction.ActiveStatus) status);
                lt.Add(transaction);
            }

            reader.Close();
            if (lt.Count > 0)
            {
                return lt;
            }

            DbConnection.Instance().CloseConnection();
            return null;
        }

        public bool UpdateTransaction(string id)
        {
            DbConnection.Instance().OpenConnection();
            var transaction = DbConnection.Instance().Connection.BeginTransaction();
            try
            {
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var queryUpdateTransaction =
                    "update `transactions` set status = 3, updateAt = @updateAt where `transactions`.`id` = @id and status = 2";
                var cmd = new MySqlCommand(queryUpdateTransaction, DbConnection.Instance().Connection);
                cmd.Parameters.AddWithValue("@updateAt", date);
                cmd.Parameters.AddWithValue("@id", id);
                var updateTrannsaction = cmd.ExecuteNonQuery();
                if (updateTrannsaction == 1)
                {
                    transaction.Commit();
                    return true;
                }
                DbConnection.Instance().CloseConnection();
                return false;
            }
            catch (VbankError)
            {
               transaction.Rollback();
                return false;
            }
        }

        public bool CheckExistId(string id)
        {
            DbConnection.Instance().OpenConnection();
            var queryUser = "select * from `transactions` where id = @id and status = 2";
            var cmd = new MySqlCommand(queryUser, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@id", id);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                DbConnection.Instance().CloseConnection();
                return true;
            }
            
            reader.Close();
            DbConnection.Instance().CloseConnection();
            return false;
        }
    }
}