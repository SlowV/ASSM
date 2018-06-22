namespace Vbank.entity
{
    public class Transaction
    {
        public enum ActiveStatus
        {
            Processing = 1,
            Done = 2,
            Reject = 0,
            Deleted = -1,
        }

        public enum TransactionType
        {
            Deposit = 1,
            Withdraw = 2,
            Transfer = 3
        }

        private string _id;
        private string _createdAt;
        private string _updatedAt;
        private TransactionType _type;
        private decimal _amount;
        private string _content;
        private string _senderAccountNumber;
        private string _receiverAccountNumber;
        private ActiveStatus _status;

        public Transaction()
        {
        }

        public Transaction(string id, string createdAt, string updatedAt, TransactionType type, decimal amount, string content, string senderAccountNumber, string receiverAccountNumber, ActiveStatus status)
        {
            _id = id;
            _createdAt = createdAt;
            _updatedAt = updatedAt;
            _type = type;
            _amount = amount;
            _content = content;
            _senderAccountNumber = senderAccountNumber;
            _receiverAccountNumber = receiverAccountNumber;
            _status = status;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        public string UpdatedAt
        {
            get => _updatedAt;
            set => _updatedAt = value;
        }

        public TransactionType Type
        {
            get => _type;
            set => _type = value;
        }

        public decimal Amount
        {
            get => _amount;
            set => _amount = value;
        }

        public string Content
        {
            get => _content;
            set => _content = value;
        }

        public string SenderAccountNumber
        {
            get => _senderAccountNumber;
            set => _senderAccountNumber = value;
        }

        public string ReceiverAccountNumber
        {
            get => _receiverAccountNumber;
            set => _receiverAccountNumber = value;
        }

        public ActiveStatus Status
        {
            get => _status;
            set => _status = value;
        }
    }
}