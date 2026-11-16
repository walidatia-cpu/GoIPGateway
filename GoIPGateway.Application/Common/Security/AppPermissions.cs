namespace GoIPGateway.Application.Common.Security;

public static class AppPermissions
{
    public const string ClaimType = "Permission";

    // 1. Devices (أجهزة GoIP)
    public static class Devices
    {
        public const string View = "Permissions.Devices.View";
        public const string Create = "Permissions.Devices.Create";
        public const string Edit = "Permissions.Devices.Edit";
        public const string Delete = "Permissions.Devices.Delete";
        public const string Reboot = "Permissions.Devices.Reboot";
    }

    // 2. SimCards (الشرائح والمحافظ)
    public static class SimCards
    {
        public const string View = "Permissions.SimCards.View";
        public const string Recharge = "Permissions.SimCards.Recharge";
        public const string CheckBalance = "Permissions.SimCards.CheckBalance";
        public const string Transfer = "Permissions.SimCards.Transfer";
        public const string Configure = "Permissions.SimCards.Configure";
    }

    // 3. Transactions (العمليات والـ USSD)
    public static class Transactions
    {
        public const string View = "Permissions.Transactions.View";
        public const string Execute = "Permissions.Transactions.Execute";
        public const string Export = "Permissions.Transactions.Export";
    }

    // 4. Routing (توجيه العمليات والشرائح)
    public static class Routing
    {
        public const string View = "Permissions.Routing.View";
        public const string Manage = "Permissions.Routing.Manage";
    }

    // 5. ApiSettings (إعدادات الـ API والمفاتيح)
    public static class ApiSettings
    {
        public const string View = "Permissions.ApiSettings.View";
        public const string Manage = "Permissions.ApiSettings.Manage";
    }

    // 6. Reports (التقارير والإحصائيات)
    public static class Reports
    {
        public const string ViewSummary = "Permissions.Reports.ViewSummary";
        public const string ViewDevices = "Permissions.Reports.ViewDevices";
        public const string ViewTenants = "Permissions.Reports.ViewTenants";
        public const string Export = "Permissions.Reports.Export";
    }

    // 7. Users (المستخدمين والموظفين)
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Edit = "Permissions.Users.Edit";
        public const string Delete = "Permissions.Users.Delete";
        public const string ResetPassword = "Permissions.Users.ResetPassword";
        public const string ManagePermissions = "Permissions.Users.ManagePermissions";
    }

    // 8. Roles & Groups (المجموعات والأدوار)
    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Edit = "Permissions.Roles.Edit";
        public const string Delete = "Permissions.Roles.Delete";
    }

    // 9. Tenants (إدارة الشركات والتجار - حصري للأدمن العام)
    public static class Tenants
    {
        public const string View = "Permissions.Tenants.View";
        public const string Create = "Permissions.Tenants.Create";
        public const string Edit = "Permissions.Tenants.Edit";
        public const string Delete = "Permissions.Tenants.Delete";
        public const string ManageServices = "Permissions.Tenants.ManageServices";
        public const string ManageTokens = "Permissions.Tenants.ManageTokens";
    }

    // 10. ServiceCatalog (كتالوج خدمات USSD - حصري للأدمن العام)
    public static class ServiceCatalog
    {
        public const string View = "Permissions.ServiceCatalog.View";
        public const string Manage = "Permissions.ServiceCatalog.Manage";
    }

    public record PermissionDefinition(
        string Code, 
        string NameAr, 
        string DescriptionAr, 
        string ModuleAr, 
        string ModuleKey,
        bool IsTenantAllowed = true);

    public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
    {
        // Devices
        new(Devices.View, "عرض أجهزة GoIP", "عرض قائمة الأجهزة وحالة البورتات والاتصال", "أجهزة GoIP", "Devices", true),
        new(Devices.Create, "إضافة جهاز GoIP جديد", "تسجيل جهاز جديد في النظام", "أجهزة GoIP", "Devices", true),
        new(Devices.Edit, "تعديل بيانات الجهاز", "تعديل إعدادات الربط وعنوان الجهاز والبورتات", "أجهزة GoIP", "Devices", true),
        new(Devices.Delete, "حذف الجهاز", "حذف الجهاز من النظام أو فك ارتباطه", "أجهزة GoIP", "Devices", true),
        new(Devices.Reboot, "إعادة تشغيل الجهاز والبورتات", "إرسال أوامر إعادة التشغيل عن بعد", "أجهزة GoIP", "Devices", true),

        // SimCards
        new(SimCards.View, "عرض الشرائح والمحافظ", "استعراض الخطوط والرصيد وحالة الخط", "الشرائح والمحافظ", "SimCards", true),
        new(SimCards.Recharge, "شحن رصيد الشريحة", "تنفيذ عمليات شحن الرصيد للخطوط", "الشرائح والمحافظ", "SimCards", true),
        new(SimCards.CheckBalance, "استعلام رصيد الشريحة", "إرسال استعلام فوري لرصيد الخط والمحفظة", "الشرائح والمحافظ", "SimCards", true),
        new(SimCards.Transfer, "تحويل رصيد", "إجراء تحويل رصيد من الشريحة", "الشرائح والمحافظ", "SimCards", true),
        new(SimCards.Configure, "إعدادات الشريحة والحدود", "تعديل حدود الاستخدام اليومي والشهري للشريحة", "الشرائح والمحافظ", "SimCards", true),

        // Transactions
        new(Transactions.View, "عرض سجل العمليات", "الاطلاع على الحركات والعمليات المنفذة وسجلاتها", "سجل العمليات", "Transactions", true),
        new(Transactions.Execute, "تنفيذ عمليات فورية", "تنفيذ أوامر USSD ودفع فوري يدوياً", "سجل العمليات", "Transactions", true),
        new(Transactions.Export, "تصدير سجل العمليات", "تصدير سجل المعاملات إلى ملفات Excel/CSV", "سجل العمليات", "Transactions", true),

        // Routing
        new(Routing.View, "عرض قواعد التوجيه", "الاطلاع على مسارات توجيه الخدمات للشرائح", "توجيه الخدمات", "Routing", true),
        new(Routing.Manage, "إدارة وتعديل قواعد التوجيه", "تغيير وتعيين الشرائح المخصصة للخدمات", "توجيه الخدمات", "Routing", true),

        // ApiSettings
        new(ApiSettings.View, "عرض إعدادات الـ API", "الاطلاع على مفاتيح وتوكنات الربط", "الربط البرمجي API", "ApiSettings", true),
        new(ApiSettings.Manage, "إدارة مفاتيح الـ API", "توليد أو إعادة تعيين مفاتيح الربط البرمجي", "الربط البرمجي API", "ApiSettings", true),

        // Reports
        new(Reports.ViewSummary, "عرض التقارير والتسويات", "الاطلاع على تقرير الإجماليات والحركات المالية", "التقارير والتسويات", "Reports", true),
        new(Reports.ViewDevices, "عرض تقارير الأجهزة والبوابات", "الاطلاع على أداء ومعدل نجاح الأجهزة", "التقارير والتسويات", "Reports", true),
        new(Reports.ViewTenants, "عرض تقارير أداء الشركات", "مقارنة أداء وتوزيع الشركات (خاص بالإدارة)", "التقارير والتسويات", "Reports", false),
        new(Reports.Export, "تصدير التقارير", "تنزيل وطباعة التقارير المالية والتشغيلية", "التقارير والتسويات", "Reports", true),

        // Users
        new(Users.View, "عرض المستخدمين والموظفين", "استعراض قائمة المستخدمين والحسابات", "إدارة المستخدمين والموظفين", "Users", true),
        new(Users.Create, "إضافة مستخدم جديد", "إنشاء حساب مستخدم أو موظف جديد", "إدارة المستخدمين والموظفين", "Users", true),
        new(Users.Edit, "تعديل بيانات المستخدم", "تحديث بيانات الحساب وتعيين الأدوار والمجموعات", "إدارة المستخدمين والموظفين", "Users", true),
        new(Users.Delete, "حذف أو تعطيل المستخدم", "إيقاف أو حذف حساب المستخدم من النظام", "إدارة المستخدمين والموظفين", "Users", true),
        new(Users.ResetPassword, "إعادة تعيين كلمة المرور المؤقتة", "توليد كلمة مرور مؤقتة وإلزام التغيير عند أول لوجن", "إدارة المستخدمين والموظفين", "Users", true),
        new(Users.ManagePermissions, "تخصيص الصلاحيات المباشرة", "منح أو سحب صلاحيات فردية خاصة بالمستخدم", "إدارة المستخدمين والموظفين", "Users", true),

        // Roles
        new(Roles.View, "عرض مجموعات الصلاحيات", "استعراض الأدوار والمجموعات الوظيفية", "المجموعات والأدوار", "Roles", true),
        new(Roles.Create, "إنشاء مجموعة صلاحيات جديدة", "تكوين دور جديد وتحديد صلاحياته", "المجموعات والأدوار", "Roles", true),
        new(Roles.Edit, "تعديل مجموعة الصلاحيات", "تعديل الصلاحيات الممنوحة للمجموعة", "المجموعات والأدوار", "Roles", true),
        new(Roles.Delete, "حذف مجموعة الصلاحيات", "إلغاء وحذف المجموعة المخصصة", "المجموعات والأدوار", "Roles", true),

        // Tenants (System Only)
        new(Tenants.View, "عرض الشركات والتجار", "استعراض قائمة الشركات المشتركة في المنصة", "إدارة الشركات والتجار", "Tenants", false),
        new(Tenants.Create, "إضافة شركة جديدة", "تسجيل شركة جديدة في النظام وإنشاء حسابها", "إدارة الشركات والتجار", "Tenants", false),
        new(Tenants.Edit, "تعديل بيانات الشركة", "تحديث حدود العمليات والأجهزة ورصيد الشركة", "إدارة الشركات والتجار", "Tenants", false),
        new(Tenants.Delete, "حذف أو حظر الشركة", "إيقاف خدمات الشركة أو حذفها مؤقتاً", "إدارة الشركات والتجار", "Tenants", false),
        new(Tenants.ManageServices, "إدارة خدمات الشركة", "تحديد خدمات USSD المصرح للشركة باستخدامها", "إدارة الشركات والتجار", "Tenants", false),
        new(Tenants.ManageTokens, "إدارة توكنات ومفاتيح الشركة", "إصدار وإلغاء توكنات الربط الخاصة بالشركة", "إدارة الشركات والتجار", "Tenants", false),

        // ServiceCatalog (System Only)
        new(ServiceCatalog.View, "عرض كتالوج خدمات USSD", "استعراض الخدمات وخطوات USSD المسجلة بالنظام", "كتالوج الخدمات", "ServiceCatalog", false),
        new(ServiceCatalog.Manage, "إدارة كتالوج الخدمات", "إضافة وتعديل الخدمات وخطوات الـ USSD والمتغيرات", "كتالوج الخدمات", "ServiceCatalog", false),
    };

    public static IReadOnlyList<PermissionDefinition> GetForTenant() =>
        All.Where(p => p.IsTenantAllowed).ToList();

    public static IReadOnlyList<PermissionDefinition> GetForSystem() =>
        All.ToList();
}

