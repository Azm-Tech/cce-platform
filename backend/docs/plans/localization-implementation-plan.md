# Localization Implementation Plan

## How to Adopt in Another Solution

1. Replace all `[YourAppName]` occurrences with your root namespace.
2. Install the `YamlDotNet` NuGet package.
3. Create a `Localization/Resources.yaml` file in your API project and mark it `CopyToOutputDirectory:Always`.
4. Register `YamlLocalizationStore` as **singleton** and `ILocalizationService` as **scoped** in DI.
5. Ensure your `IUserContext` (or equivalent) exposes a `Locale` property for culture fallback.

---

## Overview

This plan implements a lightweight, file-based bilingual localization system that works without `IStringLocalizer` or `.resx` files. It auto-discovers `Resources.yaml` files from all loaded assemblies and merges them into an in-memory store at startup.

**Packages required:** `YamlDotNet`

---

### 1. Add the NuGet Package

Add to your central package management or `.csproj`:

```xml
<PackageReference Include="YamlDotNet" />
```

---

### 2. Create the YAML Resource File (API Layer)

**File:** `API/Localization/Resources.yaml`

```yaml
INVALID_CREDENTIALS:
  ar: "عذرًا، حدثت مشكلة أثناء تسجيل الدخول"
  en: "Sorry, a problem occurred during login"

INVALID_TOKEN:
  ar: "رمز الوصول غير صالح."
  en: "Invalid access token."

INVALID_REFRESH_TOKEN:
  ar: "رمز التحديث غير صالح أو منتهي الصلاحية."
  en: "Invalid or expired refresh token."

ACCOUNT_DEACTIVATED:
  ar: "عذرًا، حدثت مشكلة أثناء تسجيل الدخول"
  en: "Sorry, a problem occurred during login"

NOT_AUTHENTICATED:
  ar: "المستخدم غير مصادق."
  en: "User not authenticated."

LOGIN_SUCCESS:
  ar: "تم تسجيل الدخول بنجاح"
  en: "Logged in successfully"

REGISTER_SUCCESS:
  ar: "تم إنشاء الحساب بنجاح"
  en: "Account created successfully"

LOGOUT_SUCCESS:
  ar: "تم تسجيل الخروج بنجاح"
  en: "Logged out successfully"

TOKEN_REFRESHED:
  ar: "تم تحديث الرمز بنجاح"
  en: "Token refreshed successfully"

USER_NOT_FOUND:
  ar: "عذرًا، لم يتم العثور على الحساب المرتبط بالبريد الإلكتروني"
  en: "Sorry, no account was found associated with this email address"

EMAIL_EXISTS:
  ar: "عذرًا، حدثت مشكلة أثناء إنشاء الحساب"
  en: "Sorry, a problem occurred while creating the account"

USERNAME_EXISTS:
  ar: "اسم المستخدم مستخدم بالفعل."
  en: "Username already taken."

USER_CREATED:
  ar: "تم إنشاء المستخدم بنجاح!"
  en: "User created successfully!"

USER_UPDATED:
  ar: "تم تحديث المستخدم بنجاح"
  en: "User updated successfully"

USER_DELETED:
  ar: "تم حذف المستخدم بنجاح!"
  en: "User deleted successfully!"

USER_ACTIVATED:
  ar: "تم تفعيل المستخدم بنجاح"
  en: "User activated successfully"

USER_DEACTIVATED:
  ar: "تم تعطيل المستخدم بنجاح"
  en: "User deactivated successfully"

ROLES_ASSIGNED:
  ar: "تم تعيين الأدوار بنجاح"
  en: "Roles assigned successfully"

USER_CREATION_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء إنشاء الحساب"
  en: "Sorry, a problem occurred while creating the account"

USER_UPDATE_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء تحديث المستخدم"
  en: "Sorry, a problem occurred while updating the user"

USER_DELETE_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء حذف المستخدم"
  en: "Sorry, a problem occurred while deleting the user"

ACTIVATE_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء تفعيل المستخدم"
  en: "Sorry, a problem occurred while activating the user"

DEACTIVATE_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء تعطيل المستخدم"
  en: "Sorry, a problem occurred while deactivating the user"

REMOVE_ROLES_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء إزالة الأدوار"
  en: "Sorry, a problem occurred while removing roles"

ADD_ROLES_FAILED:
  ar: "عذرًا، حدثت مشكلة أثناء إضافة الأدوار"
  en: "Sorry, a problem occurred while adding roles"

CONTENT_NOT_FOUND:
  ar: "المحتوى غير موجود."
  en: "Content not found."

CONTENT_EXISTS:
  ar: "المحتوى بهذا العنوان موجود بالفعل."
  en: "Content with this title already exists."

CONTENT_CREATED:
  ar: "تم إنشاء المحتوى بنجاح"
  en: "Content created successfully"

CONTENT_UPDATED:
  ar: "تم تحديث المحتوى بنجاح"
  en: "Content updated successfully"

CONTENT_DELETED:
  ar: "تم حذف المحتوى بنجاح"
  en: "Content deleted successfully"

CONTENT_PUBLISHED:
  ar: "تم نشر المحتوى بنجاح"
  en: "Content published successfully"

CONTENT_ARCHIVED:
  ar: "تم أرشفة المحتوى بنجاح"
  en: "Content archived successfully"

NOTIFICATION_NOT_FOUND:
  ar: "الإشعار غير موجود."
  en: "Notification not found."

ACCESS_DENIED:
  ar: "الوصول مرفوض."
  en: "Access denied."

NOTIFICATION_CREATED:
  ar: "تم إنشاء الإشعار بنجاح"
  en: "Notification created successfully"

NOTIFICATION_MARKED_READ:
  ar: "تم تحديد الإشعار كمقروء"
  en: "Notification marked as read"

NOTIFICATION_DELETED:
  ar: "تم حذف الإشعار بنجاح"
  en: "Notification deleted successfully"

SETTING_NOT_FOUND:
  ar: "الإعداد غير موجود."
  en: "Setting not found."

SETTING_EXISTS:
  ar: "الإعداد بهذا المفتاح موجود بالفعل."
  en: "Setting with this key already exists."

SETTING_CREATED:
  ar: "تم إنشاء الإعداد بنجاح"
  en: "Setting created successfully"

SETTING_UPDATED:
  ar: "تم تحديث الإعداد بنجاح"
  en: "Setting updated successfully"

SETTING_DELETED:
  ar: "تم حذف الإعداد بنجاح"
  en: "Setting deleted successfully"

SETTING_REPROTECT_FAILED:
  ar: "تعذر إعادة معالجة القيمة المحمية الحالية. يرجى تقديم قيمة جديدة عند تغيير وضع الحماية."
  en: "The existing protected value could not be re-processed. Provide a new value when changing protection mode."

VALIDATION_ERROR:
  ar: "عذرًا، البيانات المدخلة غير صحيحة"
  en: "Sorry, the entered data is invalid"

REQUIRED_FIELD:
  ar: "هذا الحقل مطلوب"
  en: "This field is required"

INVALID_EMAIL:
  ar: "البريد الإلكتروني غير صالح"
  en: "Invalid email format"

INVALID_PHONE:
  ar: "رقم الهاتف غير صالح"
  en: "Invalid phone number"

MIN_LENGTH:
  ar: "القيمة قصيرة جدًا"
  en: "Value is too short"

MAX_LENGTH:
  ar: "القيمة طويلة جدًا"
  en: "Value is too long"

INTERNAL_ERROR:
  ar: "حدث خطأ غير متوقع"
  en: "An unexpected error occurred"

UNAUTHORIZED_ACCESS:
  ar: "الوصول غير مصرح به"
  en: "Unauthorized access"

FORBIDDEN_ACCESS:
  ar: "الوصول ممنوع"
  en: "Forbidden access"

BAD_REQUEST:
  ar: "عذرًا، البيانات المدخلة غير صحيحة"
  en: "Sorry, the entered data is invalid"

RESOURCE_NOT_FOUND:
  ar: "المورد غير موجود"
  en: "Resource not found"

EXTERNAL_API_ERROR:
  ar: "عذرًا، حدثت مشكلة أثناء الاتصال بالخدمة الخارجية"
  en: "Sorry, a problem occurred while connecting to the external service"

EXTERNAL_API_NOT_CONFIGURED:
  ar: "الخدمة الخارجية غير مكونة"
  en: "External service is not configured"

SUCCESS_CREATED:
  ar: "تم الإنشاء بنجاح"
  en: "Created successfully"

SUCCESS_UPDATED:
  ar: "تم التحديث بنجاح"
  en: "Updated successfully"

SUCCESS_DELETED:
  ar: "تم الحذف بنجاح"
  en: "Deleted successfully"

SUCCESS_OPERATION:
  ar: "تمت العملية بنجاح"
  en: "Operation completed successfully"

EMAIL_REQUIRED:
  ar: "البريد الإلكتروني مطلوب"
  en: "Email is required"

PASSWORD_REQUIRED:
  ar: "كلمة المرور مطلوبة"
  en: "Password is required"

USERNAME_REQUIRED:
  ar: "اسم المستخدم مطلوب"
  en: "Username is required"

FIRST_NAME_REQUIRED:
  ar: "الاسم الأول مطلوب"
  en: "First name is required"

LAST_NAME_REQUIRED:
  ar: "اسم العائلة مطلوب"
  en: "Last name is required"

TOKEN_REQUIRED:
  ar: "الرمز مطلوب"
  en: "Token is required"

TITLE_REQUIRED:
  ar: "العنوان مطلوب"
  en: "Title is required"

TITLE_MAX_LENGTH:
  ar: "يجب ألا يتجاوز العنوان 500 حرف"
  en: "Title must not exceed 500 characters"

BODY_REQUIRED:
  ar: "المحتوى مطلوب"
  en: "Body is required"

SUMMARY_MAX_LENGTH:
  ar: "يجب ألا يتجاوز الملخص 1000 حرف"
  en: "Summary must not exceed 1000 characters"

CONTENT_TYPE_REQUIRED:
  ar: "نوع المحتوى مطلوب"
  en: "Content type is required"

CONTENT_TYPE_MAX_LENGTH:
  ar: "يجب ألا يتجاوز نوع المحتوى 50 حرف"
  en: "Content type must not exceed 50 characters"

AUTHOR_ID_REQUIRED:
  ar: "معرف المؤلف مطلوب"
  en: "Author ID is required"

STATUS_REQUIRED:
  ar: "الحالة مطلوبة"
  en: "Status is required"

STATUS_INVALID:
  ar: "يجب أن تكون الحالة Draft أو Published أو Archived"
  en: "Status must be Draft, Published, or Archived"

FEATURED_IMAGE_URL_MAX_LENGTH:
  ar: "يجب ألا يتجاوز رابط الصورة 2000 حرف"
  en: "Featured image URL must not exceed 2000 characters"

CATEGORY_MAX_LENGTH:
  ar: "يجب ألا يتجاوز التصنيف 100 حرف"
  en: "Category must not exceed 100 characters"

USER_ID_REQUIRED:
  ar: "معرف المستخدم مطلوب"
  en: "User ID is required"

MESSAGE_REQUIRED:
  ar: "الرسالة مطلوبة"
  en: "Message is required"

MESSAGE_MAX_LENGTH:
  ar: "يجب ألا تتجاوز الرسالة 2000 حرف"
  en: "Message must not exceed 2000 characters"

NOTIFICATION_TYPE_REQUIRED:
  ar: "نوع الإشعار مطلوب"
  en: "Notification type is required"

NOTIFICATION_TYPE_MAX_LENGTH:
  ar: "يجب ألا يتجاوز نوع الإشعار 50 حرف"
  en: "Notification type must not exceed 50 characters"

CHANNEL_REQUIRED:
  ar: "القناة مطلوبة"
  en: "Channel is required"

CHANNEL_INVALID:
  ar: "يجب أن تكون القناة InApp أو Email أو SMS أو Push"
  en: "Channel must be InApp, Email, SMS, or Push"

KEY_REQUIRED:
  ar: "المفتاح مطلوب"
  en: "Key is required"

KEY_MAX_LENGTH:
  ar: "يجب ألا يتجاوز المفتاح 200 حرف"
  en: "Key must not exceed 200 characters"

VALUE_REQUIRED:
  ar: "القيمة مطلوبة"
  en: "Value is required"

VALUE_MAX_LENGTH:
  ar: "يجب ألا تتجاوز القيمة 4000 حرف"
  en: "Value must not exceed 4000 characters"

INVALID_FORMAT:
  ar: "التنسيق غير صالح"
  en: "Invalid format"

PASSWORD_UPPERCASE:
  ar: "يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل"
  en: "Password must contain at least one uppercase letter"

PASSWORD_LOWERCASE:
  ar: "يجب أن تحتوي كلمة المرور على حرف صغير واحد على الأقل"
  en: "Password must contain at least one lowercase letter"

PASSWORD_NUMBER:
  ar: "يجب أن تحتوي كلمة المرور على رقم واحد على الأقل"
  en: "Password must contain at least one number"

EXTERNAL_API_CONFIG_NOT_FOUND:
  ar: "إعداد API الخارجي غير موجود."
  en: "External API configuration not found."

EXTERNAL_API_CONFIG_EXISTS:
  ar: "إعداد API الخارجي بهذا الاسم موجود بالفعل."
  en: "External API configuration with this name already exists."
```

> **Note:** Trim the file to only the keys your application actually uses. Keep keys identical to `ApplicationErrors` constants for automatic lookup.

---

### 3. Mark YAML File as Copy-to-Output (API `.csproj`)

```xml
<ItemGroup>
  <None Include="Localization\*.yaml">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

### 4. Create `YamlLocalizationStore` (Infrastructure Layer)

**File:** `Infrastructure/Localization/YamlLocalizationStore.cs`

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace [YourAppName].Infrastructure.Localization;

public class YamlLocalizationStore
{
    private readonly Dictionary<string, Dictionary<string, string>> _store = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public YamlLocalizationStore()
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var location = asm.Location;
                if (string.IsNullOrEmpty(location)) continue;
                var dir = Path.GetDirectoryName(location);
                if (string.IsNullOrEmpty(dir)) continue;

                var resourcesPath = Path.Combine(dir, "Localization", "Resources.yaml");
                if (File.Exists(resourcesPath))
                {
                    var resourcesYaml = File.ReadAllText(resourcesPath);
                    var resourcesParsed = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(resourcesYaml);
                    Merge(resourcesParsed);
                }
            }
            catch
            {
                // Continue loading other assemblies on malformed files
            }
        }
    }

    private void Merge(Dictionary<string, Dictionary<string, string>>? parsed)
    {
        if (parsed == null) return;
        lock (_lock)
        {
            foreach (var kv in parsed)
            {
                var key = kv.Key.Trim();
                if (!_store.TryGetValue(key, out var langs))
                {
                    langs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _store[key] = langs;
                }

                foreach (var lp in kv.Value)
                {
                    var lang = lp.Key.Trim();
                    var text = lp.Value ?? string.Empty;
                    langs[lang] = text;
                }
            }
        }
    }

    public bool TryGet(string key, out Dictionary<string, string>? langs)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            langs = null;
            return false;
        }
        return _store.TryGetValue(key, out langs!);
    }
}
```

---

### 5. Create `ILocalizationService` and `LocalizedMessage` (Application Layer)

**File:** `Application/Localization/ILocalizationService.cs`

```csharp
using System.Globalization;

namespace [YourAppName].Application.Localization;

public interface ILocalizationService
{
    string GetString(string key, CultureInfo? culture = null);
    string GetStringOrDefault(string key, string defaultMessage, CultureInfo? culture = null);
    LocalizedMessage GetLocalizedMessage(string key);
}
```

**File:** `Application/Localization/LocalizedMessage.cs`

```csharp
namespace [YourAppName].Application.Localization;

public class LocalizedMessage
{
    public string Ar { get; set; } = string.Empty;
    public string En { get; set; } = string.Empty;
}
```

---

### 6. Create `LocalizationService` (Infrastructure Layer)

**File:** `Infrastructure/Localization/LocalizationService.cs`

```csharp
using System.Globalization;
using [YourAppName].Application.Interfaces;
using [YourAppName].Application.Localization;

namespace [YourAppName].Infrastructure.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly YamlLocalizationStore _store;
    private readonly IUserContext _userContext;

    public LocalizationService(YamlLocalizationStore store, IUserContext userContext)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _userContext = userContext;
    }

    public string GetString(string key, CultureInfo? culture = null)
    {
        culture = GetCultureInfo(culture);
        var lang = culture.TwoLetterISOLanguageName;

        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        if (_store.TryGet(key, out var language) && language != null)
        {
            if (language.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
            if (language.TryGetValue("ar", out var ar) && !string.IsNullOrEmpty(ar)) return ar;
            return language.Values.FirstOrDefault() ?? key;
        }

        return key;
    }

    public string GetStringOrDefault(string key, string defaultMessage, CultureInfo? culture = null)
    {
        var v = GetString(key, culture);
        return string.IsNullOrEmpty(v) || v == key ? defaultMessage : v;
    }

    public LocalizedMessage GetLocalizedMessage(string key)
    {
        var enCulture = new CultureInfo("en");
        var arCulture = new CultureInfo("ar");

        var enMessage = GetString(key, enCulture);
        var arMessage = GetString(key, arCulture);

        if (string.IsNullOrEmpty(enMessage) || enMessage == key) enMessage = key;
        if (string.IsNullOrEmpty(arMessage) || arMessage == key) arMessage = key;

        return new LocalizedMessage { En = enMessage, Ar = arMessage };
    }

    private CultureInfo GetCultureInfo(CultureInfo? culture)
    {
        if (culture != null) return culture;
        return _userContext?.Locale ?? new CultureInfo("ar-SA");
    }
}
```

> **Prerequisite:** `IUserContext` must expose a `Locale` property (type `CultureInfo`). If you do not have this abstraction, remove the `_userContext` dependency and default to `ar-SA` or read from `Thread.CurrentThread.CurrentCulture`.

---

### 7. Register Services in DI (API Layer)

**File:** `API/Extensions/WebApiServiceExtensions.cs` (or your own DI registration class)

```csharp
using [YourAppName].Application.Localization;
using [YourAppName].Infrastructure.Localization;

namespace [YourAppName].API.Extensions;

public static class WebApiServiceExtensions
{
    public static IServiceCollection AddPlatformWebApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddYamlLocalization();
        // ... other registrations
        return services;
    }

    private static IServiceCollection AddYamlLocalization(this IServiceCollection services)
    {
        services.AddSingleton<YamlLocalizationStore>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        return services;
    }
}
```

---

### 8. Integration with OpenAPI (API Layer)

Add the `Accept-Language` header parameter to all operations so consumers know they can request localization.

Inside your OpenAPI document transformer (see Scalar & Swagger plan):

```csharp
options.AddOperationTransformer((operation, _, _) =>
{
    var parameters = operation.Parameters?.ToList() ?? new List<IOpenApiParameter>();
    parameters.Add(new OpenApiParameter
    {
        Name = "Accept-Language",
        In = ParameterLocation.Header,
        Description = "Language preference (ar, en). Default: ar",
        Required = false,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String }
    });
    operation.Parameters = parameters;
    return Task.CompletedTask;
});
```

---

## YAML Schema Reference

```yaml
ERROR_KEY:
  ar: "Arabic text"
  en: "English text"
```

- Keys are case-insensitive at runtime.
- Language codes are lowercase two-letter ISO names (`ar`, `en`).
- If a requested language is missing, the system falls back to `ar`, then the first available language, then returns the key itself.

---

## Integration Checklist

| Step | Location | Lifetime |
|------|----------|----------|
| `YamlLocalizationStore` | Infrastructure | Singleton |
| `ILocalizationService` | Application (interface) / Infrastructure (impl) | Scoped |
| `Resources.yaml` | API / any assembly output | Content file |
| OpenAPI `Accept-Language` | API OpenAPI transformer | N/A |
