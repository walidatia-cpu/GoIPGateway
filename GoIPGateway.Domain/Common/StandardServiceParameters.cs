using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Domain.Common;

/// <summary>
/// التعريف القياسي للبارامتر المعياري للخدمة
/// </summary>
public sealed class StandardParameterDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayNameAr { get; init; } = string.Empty;
    public string DisplayNameEn { get; init; } = string.Empty;
    public ParameterDataType DataType { get; init; }
    public bool DefaultRequired { get; init; }
    public string DescriptionAr { get; init; } = string.Empty;
    public string PlaceholderTag => $"{{{Name}}}";
    public string Icon { get; init; } = "bi-input-cursor-text";
}

/// <summary>
/// السجل المركزي الموحد للبارامترات الثابتة المعيارية وحقول النظام المحمية
/// </summary>
public static class StandardServiceParameters
{
    // 1. مفاتيح البارامترات الثابتة المعيارية المعتمدة لمدخلات العملاء / الـ API
    public const string Amount = "amount";
    public const string PhoneNumber = "phoneNumber";
    public const string NationalId = "nationalId";
    public const string AccountNumber = "accountNumber";
    public const string InvoiceNumber = "invoiceNumber";
    public const string OtpCode = "otpCode";

    public static readonly IReadOnlyList<StandardParameterDefinition> All = new List<StandardParameterDefinition>
    {
        new()
        {
            Key = Amount,
            Name = "Amount",
            DisplayNameAr = "المبلغ المطلوب (ج.م)",
            DisplayNameEn = "Amount",
            DataType = ParameterDataType.Decimal,
            DefaultRequired = true,
            DescriptionAr = "المبلغ المالي المراد خصمه أو تحويله أو شحنه",
            Icon = "bi-cash-coin"
        },
        new()
        {
            Key = PhoneNumber,
            Name = "PhoneNumber",
            DisplayNameAr = "رقم هاتف العميل / المحفظة",
            DisplayNameEn = "Phone Number",
            DataType = ParameterDataType.Phone,
            DefaultRequired = true,
            DescriptionAr = "رقم محفظة أو هاتف العميل المستلم (11 رقم مصري)",
            Icon = "bi-phone"
        },
        new()
        {
            Key = NationalId,
            Name = "NationalId",
            DisplayNameAr = "الرقم القومي للعميل",
            DisplayNameEn = "National ID",
            DataType = ParameterDataType.String,
            DefaultRequired = false,
            DescriptionAr = "الرقم القومي لصاحب العملية (14 رقماً)",
            Icon = "bi-person-vcard"
        },
        new()
        {
            Key = AccountNumber,
            Name = "AccountNumber",
            DisplayNameAr = "رقم الحساب / العداد / العقد",
            DisplayNameEn = "Account Number",
            DataType = ParameterDataType.String,
            DefaultRequired = false,
            DescriptionAr = "رقم الحساب أو كود المشترك أو رقم العداد أو العقد",
            Icon = "bi-file-earmark-text"
        },
        new()
        {
            Key = InvoiceNumber,
            Name = "InvoiceNumber",
            DisplayNameAr = "رقم الفاتورة / المرجع",
            DisplayNameEn = "Invoice Number",
            DataType = ParameterDataType.String,
            DefaultRequired = false,
            DescriptionAr = "الرقم المرجعي أو كود سداد الفاتورة",
            Icon = "bi-receipt"
        },
        new()
        {
            Key = OtpCode,
            Name = "OtpCode",
            DisplayNameAr = "كود التحقق (OTP)",
            DisplayNameEn = "OTP Code",
            DataType = ParameterDataType.String,
            DefaultRequired = false,
            DescriptionAr = "رمز التحقق السري المؤقت المرسل في رسالة SMS لتأكيد المعاملة",
            Icon = "bi-shield-lock"
        }
    };

    public static readonly IReadOnlySet<string> AllowedKeys =
        All.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    // 2. حقول النظام الداخلية المحمية (تُستبدل تلقائياً من إعدادات الشريحة أو جهاز GoIP ولا تُطلب من العميل)
    public static readonly IReadOnlySet<string> ReservedInternalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pin",
        "walletpassword",
        "ussdpassword",
        "merchantcode",
        "merchant",
        "merchantid",
        "vendorcode",
        "simnumber"
    };

    /// <summary>
    /// التحقق هل المفتاح من الحقول المعيارية المعتمدة
    /// </summary>
    public static bool IsAllowedStandardParameter(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var clean = CleanKey(key);
        return AllowedKeys.Contains(clean);
    }

    /// <summary>
    /// التحقق هل المفتاح من حقول النظام الداخلية المحمية
    /// </summary>
    public static bool IsReservedInternalKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var clean = CleanKey(key);
        return ReservedInternalKeys.Contains(clean);
    }

    /// <summary>
    /// البحث عن تعريف البارامتر بواسطة مفتاحه
    /// </summary>
    public static StandardParameterDefinition? FindByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var clean = CleanKey(key);
        return All.FirstOrDefault(p => string.Equals(p.Key, clean, StringComparison.OrdinalIgnoreCase));
    }

    public static string CleanKey(string key)
    {
        return key.Trim().Trim('{', '}').Trim();
    }
}

