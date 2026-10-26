namespace GoIPGateway.Domain.Enums;

public enum DeviceType
{
    GoIP128 = 1,
    GoIP512 = 2,
    GoIP1 = 3,
    GoIP4 = 4,
    GoIP8 = 5,
    GoIP16 = 6,
    GoIP32 = 7
}

public enum PortStatus
{
    Empty = 0,
    Active = 1,
    Busy = 2,
    Error = 3,
    Disabled = 4
}

public enum OperatorType
{
    Vodafone = 1,
    Orange = 2,
    Etisalat = 3,
    WE = 4
}

public enum ServiceType
{
    USSD = 1,
    SMS = 2,
    BalanceInquiry = 3,
    SimNumberInquiry = 4,
    QRPayment = 5,
    Transfer = 6,
    Settlement = 7,
    Custom = 8
}

public enum ServiceCategory
{
    Payment = 1,
    Inquiry = 2,
    BalanceInquiry = 2,
    Management = 3,
    Settlement = 4,
    Transfer = 5,
    NumberInquiry = 6
}

public enum TransactionStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Timeout = 5,
    Reversed = 6
}

public enum BalanceActionType
{
    None = 0,
    Increment = 1,
    Decrement = 2
}

public enum MovementType
{
    Deposit = 1,
    Withdrawal = 2,
    ServiceCharge = 3,
    Refund = 4
}

public enum TenantRole
{
    TenantAdmin = 1,
    TenantOperator = 2,
    TenantViewer = 3
}

public enum RequestSource
{
    API = 0,
    Dashboard = 1,
    System = 2
}

public enum ParameterDataType
{
    String = 1,
    Number = 2,
    Decimal = 3,
    Phone = 4,
    Boolean = 5
}

public enum SmsDirection
{
    Incoming = 1,
    Outgoing = 2
}
