using System.Security.Cryptography;
using GoIPGateway.Domain.Entities;
using GoIPGateway.Domain.Enums;
using GoIPGateway.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.Data;

public static class InitialDataSeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Apply migrations or ensure database is created
            await context.Database.EnsureCreatedAsync();

            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Devices') AND name = 'MerchantCode') " +
                    "BEGIN ALTER TABLE Devices ADD MerchantCode nvarchar(max) NULL; END");
            }
            catch { }

            // 1. Roles
            string[] roles = { "SuperAdmin", "TenantAdmin", "TenantOperator", "TenantViewer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole(role) { Description = $"System role {role}" });
                }
            }

            // 2. Super Admin User
            var adminEmail = "admin@goipgateway.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Super Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var createRes = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                }
            }

            // 3. Pricing Plans
            if (!await context.PricingPlans.AnyAsync())
            {
                context.PricingPlans.AddRange(
                    new PricingPlan
                    {
                        PlanName = "Starter Plan",
                        Description = "باقة الشركات الناشئة - حتى جهاز واحد و 10,000 عملية شهرياً",
                        MonthlyFee = 500,
                        MaxDevices = 1,
                        MaxDailyOps = 500,
                        MaxMonthlyOps = 10000,
                        CostPerOperation = 0.15m,
                        IsActive = true
                    },
                    new PricingPlan
                    {
                        PlanName = "Enterprise Pro",
                        Description = "باقة الشركات الكبرى - أجهزة غير محدودة و 100,000 عملية",
                        MonthlyFee = 2500,
                        MaxDevices = 10,
                        MaxDailyOps = 10000,
                        MaxMonthlyOps = 100000,
                        CostPerOperation = 0.08m,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 4. Default Demo Tenant
            var defaultTenant = await context.Tenants.FirstOrDefaultAsync();
            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    CompanyName = "شركة الدفع السريع (FastPay)",
                    CompanyNameEn = "FastPay Ltd.",
                    ContactPerson = "م. أحمد الشريف",
                    ContactEmail = "ahmed@fastpay.com",
                    ContactPhone = "01000000001",
                    Address = "القاهرة، مصر",
                    MaxDevices = 3,
                    MaxDailyOperations = 5000,
                    Balance = 25000m,
                    CreditLimit = 50000m,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                context.Tenants.Add(defaultTenant);
                await context.SaveChangesAsync();

                // Create ApiKey
                var apiKey = new TenantApiKey
                {
                    TenantId = defaultTenant.Id,
                    ApiKey = "gw_live_demo1234567890",
                    SecretKey = "secret_key_demo_fastpay_2026_goip_gateway",
                    KeyName = "Production Live API Key",
                    RateLimit = 300,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                context.TenantApiKeys.Add(apiKey);

                // Create Company Operator User
                var compEmail = "operator@fastpay.com";
                var compUser = await userManager.FindByEmailAsync(compEmail);
                if (compUser == null)
                {
                    compUser = new ApplicationUser
                    {
                        UserName = compEmail,
                        Email = compEmail,
                        FullName = "أحمد المشغل",
                        TenantId = defaultTenant.Id,
                        EmailConfirmed = true,
                        IsActive = true
                    };
                    await userManager.CreateAsync(compUser, "FastPay@123456");
                    await userManager.AddToRoleAsync(compUser, "TenantOperator");
                }

                await context.SaveChangesAsync();
            }

            // 5. Default GoIP Device
            var defaultDevice = await context.Devices.FirstOrDefaultAsync();
            if (defaultDevice == null)
            {
                defaultDevice = new Device
                {
                    DeviceName = "GoIP-128 Gateway Primary",
                    DeviceType = DeviceType.GoIP128,
                    SerialNumber = "GOIP128-EG-2026-001",
                    IpAddress = "192.168.1.100",
                    Port = 80,
                    HttpPort = 80,
                    Username = "admin",
                    Password = "admin",
                    IsOnline = true,
                    TotalPorts = 128,
                    ActivePorts = 4,
                    Location = "Data Center Rack A-12",
                    IsActive = true,
                    LastHeartbeat = DateTime.UtcNow
                };
                context.Devices.Add(defaultDevice);
                await context.SaveChangesAsync();

                // Assign to Tenant
                context.TenantDevices.Add(new TenantDevice
                {
                    TenantId = defaultTenant.Id,
                    DeviceId = defaultDevice.Id,
                    PortRangeStart = 1,
                    PortRangeEnd = 32,
                    IsActive = true
                });

                // Generate Ports & First 4 SIMs
                var ports = new List<DevicePort>();
                for (int i = 1; i <= 128; i++)
                {
                    ports.Add(new DevicePort
                    {
                        DeviceId = defaultDevice.Id,
                        PortNumber = i,
                        PortLabel = $"L{i:D3}",
                        Status = i <= 4 ? PortStatus.Active : PortStatus.Empty,
                        IsBusy = false,
                        IsActive = true
                    });
                }
                context.DevicePorts.AddRange(ports);
                await context.SaveChangesAsync();

                // Add 4 SIM Cards (one for each operator)
                var p1 = ports[0];
                var p2 = ports[1];
                var p3 = ports[2];
                var p4 = ports[3];

                context.SimCards.AddRange(
                    new SimCard
                    {
                        DevicePortId = p1.Id,
                        Operator = OperatorType.Vodafone,
                        PhoneNumber = "01012345678",
                        SimNumber = "01012345678",
                        ICCID = "8920010000000001",
                        CurrentBalance = 15420.50m,
                        WalletPassword = "9999",
                        DailyLimit = 50000,
                        MonthlyLimit = 500000,
                        IsActive = true
                    },
                    new SimCard
                    {
                        DevicePortId = p2.Id,
                        Operator = OperatorType.Orange,
                        PhoneNumber = "01212345678",
                        SimNumber = "01212345678",
                        ICCID = "8920020000000002",
                        CurrentBalance = 8250.00m,
                        WalletPassword = "8888",
                        DailyLimit = 50000,
                        MonthlyLimit = 500000,
                        IsActive = true
                    },
                    new SimCard
                    {
                        DevicePortId = p3.Id,
                        Operator = OperatorType.Etisalat,
                        PhoneNumber = "01112345678",
                        SimNumber = "01112345678",
                        ICCID = "8920030000000003",
                        CurrentBalance = 12400.75m,
                        WalletPassword = "7777",
                        DailyLimit = 50000,
                        MonthlyLimit = 500000,
                        IsActive = true
                    },
                    new SimCard
                    {
                        DevicePortId = p4.Id,
                        Operator = OperatorType.WE,
                        PhoneNumber = "01512345678",
                        SimNumber = "01512345678",
                        ICCID = "8920040000000004",
                        CurrentBalance = 4500.00m,
                        WalletPassword = "6666",
                        DailyLimit = 30000,
                        MonthlyLimit = 300000,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 6. Service Definitions
            if (!await context.ServiceDefinitions.AnyAsync())
            {
                var balInq = new ServiceDefinition
                {
                    ServiceCode = "BAL_INQ",
                    NameAr = "استعلام الرصيد",
                    NameEn = "Balance Inquiry",
                    Description = "استعلام رصيد الشريحة اللحظي عبر USSD",
                    ServiceType = ServiceType.BalanceInquiry,
                    Category = ServiceCategory.Inquiry,
                    BaseCost = 0.10m,
                    IsActive = true,
                    SortOrder = 1,
                    IconClass = "bi-wallet2"
                };

                var numInq = new ServiceDefinition
                {
                    ServiceCode = "NUM_INQ",
                    NameAr = "استعلام رقم الشريحة",
                    NameEn = "SIM Number Inquiry",
                    Description = "استعلام رقم الهاتف الخاص بالشريحة عبر USSD",
                    ServiceType = ServiceType.SimNumberInquiry,
                    Category = ServiceCategory.NumberInquiry,
                    BaseCost = 0.10m,
                    IsActive = true,
                    SortOrder = 2,
                    IconClass = "bi-telephone"
                };

                var qrPay = new ServiceDefinition
                {
                    ServiceCode = "QR_PAY",
                    NameAr = "دفع تاجر QR",
                    NameEn = "QR Merchant Payment",
                    Description = "تنفيذ عملية دفع لتاجر عبر محفظة الهاتف الذكي بخطوات USSD متعددة",
                    ServiceType = ServiceType.QRPayment,
                    Category = ServiceCategory.Payment,
                    BalanceActionType = BalanceActionType.Decrement,
                    BaseCost = 0.50m,
                    IsActive = true,
                    SortOrder = 3,
                    IconClass = "bi-qr-code-scan"
                };

                qrPay.Parameters.Add(new ServiceParameter
                {
                    ParameterName = "MerchantCode",
                    ParameterKey = "merchantCode",
                    DisplayNameAr = "كود التاجر",
                    DisplayNameEn = "Merchant Code",
                    DataType = ParameterDataType.String,
                    IsRequired = true,
                    SortOrder = 1
                });

                qrPay.Parameters.Add(new ServiceParameter
                {
                    ParameterName = "Amount",
                    ParameterKey = "amount",
                    DisplayNameAr = "المبلغ المطلوب",
                    DisplayNameEn = "Payment Amount",
                    DataType = ParameterDataType.Decimal,
                    IsRequired = true,
                    SortOrder = 2
                });

                // USSD Steps for QR Pay
                qrPay.UssdSteps.Add(new ServiceUssdStep
                {
                    StepOrder = 1,
                    StepTitle = "طلب خدمة دفع التاجر",
                    UssdCommand = "*9*2*{MerchantCode}*{Amount}*{Password}#",
                    SuccessPattern = "تمت",
                    FailurePattern = "غير صحيح",
                    TimeoutSeconds = 30
                });

                var transfer = new ServiceDefinition
                {
                    ServiceCode = "TRANSFER",
                    NameAr = "تحويل أموال",
                    NameEn = "Money Transfer",
                    Description = "تحويل رصيد أو أموال إلى رقم محمول آخر",
                    ServiceType = ServiceType.Transfer,
                    Category = ServiceCategory.Payment,
                    BalanceActionType = BalanceActionType.Decrement,
                    BaseCost = 0.50m,
                    IsActive = true,
                    SortOrder = 4,
                    IconClass = "bi-send"
                };

                transfer.Parameters.Add(new ServiceParameter
                {
                    ParameterName = "PhoneNumber",
                    ParameterKey = "phoneNumber",
                    DisplayNameAr = "رقم المستلم",
                    DisplayNameEn = "Recipient Phone",
                    DataType = ParameterDataType.Phone,
                    IsRequired = true,
                    SortOrder = 1
                });

                transfer.Parameters.Add(new ServiceParameter
                {
                    ParameterName = "Amount",
                    ParameterKey = "amount",
                    DisplayNameAr = "المبلغ",
                    DisplayNameEn = "Amount",
                    DataType = ParameterDataType.Decimal,
                    IsRequired = true,
                    SortOrder = 2
                });

                transfer.UssdSteps.Add(new ServiceUssdStep
                {
                    StepOrder = 1,
                    StepTitle = "أمر تحويل الأموال",
                    UssdCommand = "*9*7*{PhoneNumber}*{Amount}*{Password}#",
                    SuccessPattern = "تم",
                    FailurePattern = "فشل",
                    TimeoutSeconds = 30
                });

                context.ServiceDefinitions.AddRange(balInq, numInq, qrPay, transfer);
                await context.SaveChangesAsync();

                // Subscribe default tenant to all services
                if (defaultTenant != null)
                {
                    context.TenantServices.AddRange(
                        new TenantService { TenantId = defaultTenant.Id, ServiceDefinitionId = balInq.Id, CustomPrice = 0.08m },
                        new TenantService { TenantId = defaultTenant.Id, ServiceDefinitionId = numInq.Id, CustomPrice = 0.08m },
                        new TenantService { TenantId = defaultTenant.Id, ServiceDefinitionId = qrPay.Id, CustomPrice = 0.40m },
                        new TenantService { TenantId = defaultTenant.Id, ServiceDefinitionId = transfer.Id, CustomPrice = 0.40m }
                    );
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding initial database data.");
        }
    }
}
