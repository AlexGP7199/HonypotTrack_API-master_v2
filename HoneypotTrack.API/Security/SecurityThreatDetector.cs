using System.Text.RegularExpressions;

namespace HoneypotTrack.API.Security;

/// <summary>
/// Detector de amenazas de seguridad basado en OWASP Top 10
/// </summary>
public static partial class SecurityThreatDetector
{
    /// <summary>
    /// Resultado del análisis de seguridad
    /// </summary>
    public record ThreatAnalysisResult
    {
        public bool IsThreatDetected { get; init; }
        public string? ThreatType { get; init; }
        public string? ThreatCategory { get; init; }
        public string? Description { get; init; }
        public string? MatchedPattern { get; init; }
        public int Severity { get; init; } // 1-10
    }

    /// <summary>
    /// Analiza un texto en busca de amenazas de seguridad
    /// </summary>
    public static ThreatAnalysisResult AnalyzeForThreats(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ThreatAnalysisResult { IsThreatDetected = false };

        // A01:2021 - Broken Access Control (Path Traversal)
        var pathTraversalResult = DetectPathTraversal(input);
        if (pathTraversalResult.IsThreatDetected) return pathTraversalResult;

        // A03:2021 - Injection (SQL Injection)
        var sqlInjectionResult = DetectSqlInjection(input);
        if (sqlInjectionResult.IsThreatDetected) return sqlInjectionResult;

        // A03:2021 - Injection (NoSQL Injection)
        var noSqlResult = DetectNoSqlInjection(input);
        if (noSqlResult.IsThreatDetected) return noSqlResult;

        // A03:2021 - Injection (Command Injection)
        var commandResult = DetectCommandInjection(input);
        if (commandResult.IsThreatDetected) return commandResult;

        // A03:2021 - Injection (LDAP Injection)
        var ldapResult = DetectLdapInjection(input);
        if (ldapResult.IsThreatDetected) return ldapResult;

        // A03:2021 - Injection (XSS - Cross-Site Scripting)
        var xssResult = DetectXss(input);
        if (xssResult.IsThreatDetected) return xssResult;

        // A03:2021 - Injection (XXE - XML External Entity)
        var xxeResult = DetectXxe(input);
        if (xxeResult.IsThreatDetected) return xxeResult;

        // A07:2021 - Cross-Site Request Forgery indicators
        var ssrfResult = DetectSsrf(input);
        if (ssrfResult.IsThreatDetected) return ssrfResult;

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    /// <summary>
    /// Analiza múltiples campos y retorna todas las amenazas encontradas
    /// </summary>
    public static IEnumerable<ThreatAnalysisResult> AnalyzeMultipleFields(Dictionary<string, string?> fields)
    {
        var threats = new List<ThreatAnalysisResult>();

        foreach (var field in fields)
        {
            var result = AnalyzeForThreats(field.Value);
            if (result.IsThreatDetected)
            {
                threats.Add(result with { Description = $"[{field.Key}] {result.Description}" });
            }
        }

        return threats;
    }

    #region SQL Injection Detection

    private static ThreatAnalysisResult DetectSqlInjection(string input)
    {
        // Patrones de SQL Injection
        var sqlPatterns = new[]
        {
            // Comentarios SQL
            @"(--|#|/\*)",
            // Union-based injection
            @"\b(union\s+(all\s+)?select)\b",
            // Boolean-based injection
            @"\b(or|and)\s+[\d\w]+\s*[=<>]+\s*[\d\w]+",
            @"'\s*(or|and)\s*'?\d+'\s*[=<>]+\s*'?\d+",
            @"'\s*(or|and)\s*'\w+'\s*[=<>]+\s*'\w+",
            // Stacked queries
            @";\s*(select|insert|update|delete|drop|create|alter|exec|execute)\b",
            // Common SQL keywords in suspicious context
            @"'\s*(select|insert|update|delete|drop|truncate|create|alter)\b",
            // Time-based blind injection
            @"\b(waitfor\s+delay|sleep\s*\(|benchmark\s*\(|pg_sleep)\b",
            // Error-based injection
            @"\b(extractvalue|updatexml|xmltype|dbms_pipe)\b",
            // Always true conditions
            @"'\s*=\s*'",
            @"1\s*=\s*1",
            @"'1'\s*=\s*'1'",
            // Hex encoding attempts
            @"0x[0-9a-fA-F]+",
            // CHAR() function abuse
            @"\bchar\s*\(\d+\)",
            // Information schema access
            @"\b(information_schema|sysobjects|syscolumns|sys\.)\b"
        };

        foreach (var pattern in sqlPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "SQL_INJECTION",
                    ThreatCategory = "A03:2021 - Injection",
                    Description = "Posible intento de SQL Injection detectado",
                    MatchedPattern = pattern,
                    Severity = 9
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region XSS Detection

    private static ThreatAnalysisResult DetectXss(string input)
    {
        var xssPatterns = new[]
        {
            // Script tags
            @"<script[^>]*>",
            @"</script>",
            // Event handlers
            @"\bon\w+\s*=",
            // Javascript protocol
            @"javascript\s*:",
            // Data protocol with base64
            @"data\s*:\s*text/html",
            // Expression() in CSS
            @"expression\s*\(",
            // VBScript
            @"vbscript\s*:",
            // SVG onload
            @"<svg[^>]*onload",
            // Iframe injection
            @"<iframe[^>]*>",
            // Object/Embed tags
            @"<(object|embed|applet)[^>]*>",
            // Style with expression
            @"<style[^>]*>.*expression\s*\(",
            // Image onerror
            @"<img[^>]*onerror",
            // Body onload
            @"<body[^>]*onload",
            // Input onfocus
            @"<input[^>]*onfocus",
            // Encoded script
            @"&#\d+;",
            @"&#x[0-9a-fA-F]+;",
            // Document manipulation
            @"\b(document\.(cookie|location|write)|window\.location)\b"
        };

        foreach (var pattern in xssPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "XSS",
                    ThreatCategory = "A03:2021 - Injection",
                    Description = "Posible intento de Cross-Site Scripting (XSS) detectado",
                    MatchedPattern = pattern,
                    Severity = 8
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region Command Injection Detection

    private static ThreatAnalysisResult DetectCommandInjection(string input)
    {
        var commandPatterns = new[]
        {
            // Shell operators
            @"[|;&`$]",
            // Command substitution
            @"\$\([^)]+\)",
            @"`[^`]+`",
            // Common dangerous commands
            @"\b(cat|ls|dir|pwd|whoami|id|uname|wget|curl|nc|netcat|bash|sh|cmd|powershell)\b",
            // File operations
            @"\b(rm|del|copy|move|cp|mv)\s+",
            // Network commands
            @"\b(ping|nslookup|dig|traceroute|telnet|ftp|ssh)\b",
            // Reverse shell patterns
            @"/dev/(tcp|udp)/",
            @"\b(nc|ncat)\s+-[elp]",
            // Environment variables
            @"\$\{?[A-Z_]+\}?",
            // Windows specific
            @"\b(type|more|net\s+user|net\s+localgroup)\b"
        };

        foreach (var pattern in commandPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "COMMAND_INJECTION",
                    ThreatCategory = "A03:2021 - Injection",
                    Description = "Posible intento de Command Injection detectado",
                    MatchedPattern = pattern,
                    Severity = 10
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region Path Traversal Detection

    private static ThreatAnalysisResult DetectPathTraversal(string input)
    {
        var pathPatterns = new[]
        {
            // Directory traversal
            @"\.\./",
            @"\.\.\\",
            @"%2e%2e[/\\%]",
            @"\.\.%2f",
            @"%2e%2e%2f",
            // Null byte injection
            @"%00",
            @"\x00",
            // Absolute paths
            @"^/etc/",
            @"^/var/",
            @"^/usr/",
            @"^[a-zA-Z]:\\",
            // Sensitive files
            @"\b(passwd|shadow|hosts|\.htaccess|web\.config|\.env)\b",
            // Windows specific paths
            @"\\windows\\",
            @"\\system32\\",
            @"\\boot\.ini"
        };

        foreach (var pattern in pathPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "PATH_TRAVERSAL",
                    ThreatCategory = "A01:2021 - Broken Access Control",
                    Description = "Posible intento de Path Traversal detectado",
                    MatchedPattern = pattern,
                    Severity = 8
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region NoSQL Injection Detection

    private static ThreatAnalysisResult DetectNoSqlInjection(string input)
    {
        var noSqlPatterns = new[]
        {
            // MongoDB operators
            @"\$where",
            @"\$gt",
            @"\$lt",
            @"\$ne",
            @"\$regex",
            @"\$or",
            @"\$and",
            @"\$exists",
            // JSON injection
            @"\{[^}]*\$[a-z]+",
            // JavaScript in queries
            @"function\s*\(",
            @"this\.[a-zA-Z]+",
            // Common NoSQL payloads
            @"\[\$ne\]",
            @"\{\s*""\$",
            @"'\s*:\s*\{",
        };

        foreach (var pattern in noSqlPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "NOSQL_INJECTION",
                    ThreatCategory = "A03:2021 - Injection",
                    Description = "Posible intento de NoSQL Injection detectado",
                    MatchedPattern = pattern,
                    Severity = 8
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region LDAP Injection Detection

    private static ThreatAnalysisResult DetectLdapInjection(string input)
    {
        var ldapPatterns = new[]
        {
            // LDAP special characters
            @"[()\\*\x00]",
            // LDAP filter injection
            @"\(\|",
            @"\(&",
            @"\(!\(",
            // Common LDAP attributes
            @"\b(objectClass|cn|uid|sn|mail)\s*=\s*\*",
            // Wildcard abuse
            @"\*\)\(",
        };

        foreach (var pattern in ldapPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "LDAP_INJECTION",
                    ThreatCategory = "A03:2021 - Injection",
                    Description = "Posible intento de LDAP Injection detectado",
                    MatchedPattern = pattern,
                    Severity = 7
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region XXE Detection

    private static ThreatAnalysisResult DetectXxe(string input)
    {
        var xxePatterns = new[]
        {
            // DOCTYPE declarations
            @"<!DOCTYPE",
            @"<!ENTITY",
            // External entity references
            @"SYSTEM\s+['""]",
            @"PUBLIC\s+['""]",
            // File protocol
            @"file://",
            // PHP wrappers
            @"php://",
            @"expect://",
            // Parameter entities
            @"%\w+;",
        };

        foreach (var pattern in xxePatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "XXE",
                    ThreatCategory = "A03:2021 - Injection",
                    Description = "Posible intento de XML External Entity (XXE) detectado",
                    MatchedPattern = pattern,
                    Severity = 8
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion

    #region SSRF Detection

    private static ThreatAnalysisResult DetectSsrf(string input)
    {
        var ssrfPatterns = new[]
        {
            // Internal IP addresses
            @"\b127\.0\.0\.1\b",
            @"\blocalhost\b",
            @"\b10\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
            @"\b172\.(1[6-9]|2[0-9]|3[0-1])\.\d{1,3}\.\d{1,3}\b",
            @"\b192\.168\.\d{1,3}\.\d{1,3}\b",
            // Cloud metadata endpoints
            @"169\.254\.169\.254",
            @"metadata\.google\.internal",
            // File protocol
            @"file:///",
            // Gopher protocol (used in SSRF)
            @"gopher://",
            // Dict protocol
            @"dict://",
        };

        foreach (var pattern in ssrfPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return new ThreatAnalysisResult
                {
                    IsThreatDetected = true,
                    ThreatType = "SSRF",
                    ThreatCategory = "A10:2021 - Server-Side Request Forgery",
                    Description = "Posible intento de Server-Side Request Forgery (SSRF) detectado",
                    MatchedPattern = pattern,
                    Severity = 8
                };
            }
        }

        return new ThreatAnalysisResult { IsThreatDetected = false };
    }

    #endregion
}
