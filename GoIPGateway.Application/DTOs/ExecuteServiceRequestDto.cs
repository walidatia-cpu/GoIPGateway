using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.DTOs;

public class ExecuteServiceRequestDto
{
    public string ServiceCode { get; set; } = string.Empty;
    public string? ExternalRef { get; set; }
    public int? SimCardId { get; set; }
    public OperatorType? Operator { get; set; }

    /// <summary>
    /// المبلغ المطلوب تنفيذه (متاح كحقل مباشر أو داخل Parameters)
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// رقم هاتف المستلم (متاح كحقل مباشر أو داخل Parameters)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// كود التاجر (متاح كحقل مباشر أو داخل Parameters)
    /// </summary>
    public string? MerchantCode { get; set; }

    /// <summary>
    /// الرقم السري للمحفظة / الشريحة (اختياري)
    /// </summary>
    public string? Pin { get; set; }

    /// <summary>
    /// قاموس الحقول والمتغيرات الديناميكية
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? CallbackUrl { get; set; }

    /// <summary>
    /// دمج الحقول المباشرة مع قاموس Parameters وإرجاع قاموس غير حساس لحالة الأحرف
    /// </summary>
    public Dictionary<string, string> GetNormalizedParameters()
    {
        var dict = new Dictionary<string, string>(Parameters ?? new(), StringComparer.OrdinalIgnoreCase);

        if (Amount.HasValue && !dict.ContainsKey("amount"))
            dict["amount"] = Amount.Value.ToString("0.##");

        if (!string.IsNullOrWhiteSpace(PhoneNumber) && !dict.ContainsKey("phoneNumber"))
            dict["phoneNumber"] = PhoneNumber.Trim();

        if (!string.IsNullOrWhiteSpace(MerchantCode) && !dict.ContainsKey("merchantCode"))
            dict["merchantCode"] = MerchantCode.Trim();

        if (!string.IsNullOrWhiteSpace(Pin) && !dict.ContainsKey("pin"))
            dict["pin"] = Pin.Trim();

        return dict;
    }
}
