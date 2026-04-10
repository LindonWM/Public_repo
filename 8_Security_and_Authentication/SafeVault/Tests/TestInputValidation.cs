using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SafeVault.Data;
using SafeVault.Helpers;
using SafeVault.Pages;

[TestFixture]
public class TestInputValidation
{
    private sealed class CapturingUserRepository : IUserRepository
    {
        public string? LastUsername { get; private set; }
        public string? LastPassword { get; private set; }

        public Task<SafeVault.Models.UserRecord?> GetByUsernameAndPasswordAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default
        )
        {
            LastUsername = username;
            LastPassword = password;
            return Task.FromResult<SafeVault.Models.UserRecord?>(null);
        }

        public Task<IReadOnlyList<SafeVault.Models.UserRecord>> SearchByUsernamePrefixAsync(
            string searchTerm,
            int maxResults = 20,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<SafeVault.Models.UserRecord>>(Array.Empty<SafeVault.Models.UserRecord>());
    }

    // ── Username ─────────────────────────────────────────────────────────────

    [Test]
    public void TestForSQLInjection()
    {
        // Classic SQL injection payload; single-quote breaks out of query strings
        var malicious = "admin' OR '1'='1";
        var sanitized = InputSanitizer.SanitizeUsername(malicious);

        // The raw input must fail validation — it contains characters outside the whitelist
        Assert.That(
            InputSanitizer.IsValidUsername(malicious),
            Is.False,
            "Raw SQL injection payload must fail username validation."
        );

        // After sanitization, all dangerous characters must be gone
        Assert.That(sanitized, Does.Not.Contain("'"), "Single-quote must be stripped.");
        Assert.That(sanitized, Does.Not.Contain("="), "Equals sign must be stripped.");

        // The sanitized string is safe to use as a parameterized query value;
        // it will simply not match any real user in the database.
    }

    [TestCase("admin' OR '1'='1")]
    [TestCase("' UNION SELECT UserID,Username,Email,Role,PasswordHash FROM Users --")]
    [TestCase("'; DROP TABLE Users; --")]
    [TestCase("' OR SLEEP(5)#")]
    public void SqlInjectionPayloads_AreNeutralizedByUsernameSanitizer(string payload)
    {
        var sanitized = InputSanitizer.SanitizeUsername(payload);

        Assert.That(InputSanitizer.IsValidUsername(payload), Is.False);
        Assert.That(sanitized, Does.Not.Contain("'"));
        Assert.That(sanitized, Does.Not.Contain("="));
        Assert.That(sanitized, Does.Not.Contain(";"));
        Assert.That(sanitized.Length, Is.LessThanOrEqualTo(50));
    }

    [Test]
    public void TestForXSS()
    {
        // Script tag injection via username field
        var xssPayload = "<script>alert('xss')</script>";
        var sanitized = InputSanitizer.SanitizeUsername(xssPayload);

        // The raw input must fail validation — angle brackets are outside the whitelist
        Assert.That(
            InputSanitizer.IsValidUsername(xssPayload),
            Is.False,
            "Raw XSS payload must fail username validation."
        );

        // After sanitization, all HTML and script characters must be gone
        Assert.That(sanitized, Does.Not.Contain("<script>"), "<script> tag must be stripped.");
        Assert.That(sanitized, Does.Not.Contain("</script>"), "</script> tag must be stripped.");
        Assert.That(sanitized, Does.Not.Contain("("), "Parentheses must be stripped.");
        Assert.That(sanitized, Does.Not.Contain("'"), "Single-quotes must be stripped.");
    }

    [TestCase("<script>alert(1)</script>")]
    [TestCase("<img src=x onerror=alert(1)>")]
    [TestCase("<svg onload=alert(1)>x</svg>")]
    [TestCase("<body onload=alert('xss')>")]
    [TestCase("<a href=javascript:alert(1)>click</a>")]
    public void XssPayloads_AreStrippedFromUsername(string payload)
    {
        var sanitized = InputSanitizer.SanitizeUsername(payload);

        Assert.That(InputSanitizer.IsValidUsername(payload), Is.False);
        Assert.That(sanitized, Does.Not.Contain("<"));
        Assert.That(sanitized, Does.Not.Contain(">"));
        Assert.That(sanitized, Does.Not.Contain("("));
        Assert.That(sanitized, Does.Not.Contain(")"));
        Assert.That(sanitized, Does.Not.Contain("'"));
        Assert.That(sanitized.Length, Is.LessThanOrEqualTo(50));
    }

    [Test]
    public async Task FormPost_WithSqlInjectionInput_UsesNeutralizedValues()
    {
        var repository = new CapturingUserRepository();
        var page = new IndexModel(repository)
        {
            Username = "admin' OR '1'='1",
            Password = "x' OR 1=1 --",
        };

        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.That(result, Is.TypeOf<PageResult>());
        Assert.That(page.ErrorMessage, Is.EqualTo("Invalid username or password."));
        Assert.That(repository.LastUsername, Is.Not.Null.And.Not.Empty);
        Assert.That(repository.LastPassword, Is.Not.Null.And.Not.Empty);
        Assert.That(repository.LastUsername, Does.Not.Contain("'"));
        Assert.That(repository.LastUsername, Does.Not.Contain("="));
        Assert.That(repository.LastPassword, Is.EqualTo("x' OR 1=1 --"));
    }

    [Test]
    public async Task FormPost_WithXssPayload_DoesNotPassRawHtmlToRepository()
    {
        var repository = new CapturingUserRepository();
        var page = new IndexModel(repository)
        {
            Username = "<script>alert('xss')</script>",
            Password = "<img src=x onerror=alert(1)>",
        };

        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.That(result, Is.TypeOf<PageResult>());
        Assert.That(page.ErrorMessage, Is.Null);
        Assert.That(page.ModelState.IsValid, Is.False);
        Assert.That(repository.LastUsername, Is.Null);
        Assert.That(repository.LastPassword, Is.Null);
    }

    [Test]
    public void UsernameWithImgOnErrorPayload_IsSanitized()
    {
        var payload = "<img src=x onerror=alert(1)>";
        var sanitized = InputSanitizer.SanitizeUsername(payload);

        Assert.That(InputSanitizer.IsValidUsername(payload), Is.False);
        Assert.That(
            sanitized,
            Is.EqualTo(string.Empty),
            "HTML tag payload should be removed fully."
        );
        Assert.That(InputSanitizer.IsValidUsername(sanitized), Is.False);
    }

    [Test]
    public void UsernameWithSvgOnloadPayload_IsSanitized()
    {
        var payload = "<svg onload=alert(1)>x</svg>";
        var sanitized = InputSanitizer.SanitizeUsername(payload);

        Assert.That(InputSanitizer.IsValidUsername(payload), Is.False);
        Assert.That(
            sanitized,
            Is.EqualTo("x"),
            "Tag content may remain, but tags and script chars must be removed."
        );
        Assert.That(sanitized, Does.Not.Contain("<"));
        Assert.That(sanitized, Does.Not.Contain(">"));
    }

    [Test]
    public void ValidUsername_PassesValidation()
    {
        var input = "john_doe.123";
        var sanitized = InputSanitizer.SanitizeUsername(input);

        Assert.That(sanitized, Is.EqualTo(input));
        Assert.That(InputSanitizer.IsValidUsername(sanitized), Is.True);
    }

    [Test]
    public void UsernameExceedingMaxLength_IsTruncated()
    {
        var longInput = new string('a', 100);
        var sanitized = InputSanitizer.SanitizeUsername(longInput);

        Assert.That(sanitized.Length, Is.LessThanOrEqualTo(50));
    }

    // ── Email ─────────────────────────────────────────────────────────────────

    [Test]
    public void ValidEmail_PassesValidation()
    {
        var email = "user@example.com";
        var sanitized = InputSanitizer.SanitizeEmail(email);

        Assert.That(sanitized, Is.EqualTo(email));
        Assert.That(InputSanitizer.IsValidEmail(sanitized), Is.True);
    }

    [Test]
    public void InvalidEmail_ReturnsEmpty()
    {
        var email = "not-an-email";
        var sanitized = InputSanitizer.SanitizeEmail(email);

        Assert.That(sanitized, Is.Empty, "A string without @ should be rejected.");
    }

    [Test]
    public void EmailWithXSSPayload_IsRejected()
    {
        // After tag stripping, local part contains ( ) ' which fail the strict regex
        var email = "<script>alert('xss')</script>@example.com";
        var sanitized = InputSanitizer.SanitizeEmail(email);

        Assert.That(
            sanitized,
            Does.Not.Contain("<script>"),
            "HTML tags must be stripped from email."
        );
        Assert.That(
            sanitized,
            Is.Empty,
            "Residual special characters from the XSS payload must cause email rejection."
        );
    }

    [Test]
    public void EmailWithJavascriptScheme_IsRejected()
    {
        var email = "javascript:alert(1)@example.com";
        var sanitized = InputSanitizer.SanitizeEmail(email);

        Assert.That(sanitized, Is.Empty, "javascript: payload must not be accepted as an email.");
    }

    [Test]
    public void EmailWithSQLInjection_IsRejected()
    {
        var email = "user'--@example.com";
        var sanitized = InputSanitizer.SanitizeEmail(email);

        Assert.That(
            sanitized,
            Is.Empty,
            "Single-quote in email local part should fail strict email validation."
        );
    }
}
