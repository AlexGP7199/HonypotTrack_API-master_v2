# Threat Classification Guide

## Objetivo

Este documento explica cómo `HoneypotTrack.API` clasifica una petición como amenaza, cómo decide el `EntityName` y qué patrones suelen disparar cada tipo.

## Flujo de clasificación

El flujo es este:

1. `SecurityMiddleware` analiza la petición.
2. Si detecta un patrón malicioso, registra un evento con:
   - `ActionType = SECURITY_THREAT`
   - `EntityName = tipo de amenaza detectada`
3. Si además la amenaza cumple las reglas de activación del honeypot, puede emitir una sesión/token honeypot.

## Qué campos analiza

Normalmente se inspeccionan:

- `Request.Path`
- `QueryString`
- headers sospechosos
- campos del body JSON

En endpoints de autenticación, algunos campos como `password`, `refreshToken` y `token` se excluyen para evitar falsos positivos.

## Tipos de amenaza por `EntityName`

### `SQL_INJECTION`

Patrones relevantes:

```regex
(--|#|/\*)
\b(union\s+(all\s+)?select)\b
\b(or|and)\s+[\d\w]+\s*[=<>]+\s*[\d\w]+
'\s*(or|and)\s*'?\d+'\s*[=<>]+\s*'?\d+
'\s*(or|and)\s*'\w+'\s*[=<>]+\s*'\w+
;\s*(select|insert|update|delete|drop|create|alter|exec|execute)\b
'\s*(select|insert|update|delete|drop|truncate|create|alter)\b
\b(waitfor\s+delay|sleep\s*\(|benchmark\s*\(|pg_sleep)\b
\b(extractvalue|updatexml|xmltype|dbms_pipe)\b
'\s*=\s*'
1\s*=\s*1
'1'\s*=\s*'1'
0x[0-9a-fA-F]+
\bchar\s*\(\d+\)
\b(information_schema|sysobjects|syscolumns|sys\.)\b
```

Ejemplos:

- `' OR 1=1--`
- `UNION SELECT * FROM users`
- `'; DROP TABLE users; --`
- `1=1`

### `XSS`

Patrones relevantes:

```regex
<script[^>]*>
</script>
\bon\w+\s*=
javascript\s*:
data\s*:\s*text/html
expression\s*\(
vbscript\s*:
<svg[^>]*onload
<iframe[^>]*>
<(object|embed|applet)[^>]*>
<style[^>]*>.*expression\s*\(
<img[^>]*onerror
<body[^>]*onload
<input[^>]*onfocus
&#\d+;
&#x[0-9a-fA-F]+;
\b(document\.(cookie|location|write)|window\.location)\b
```

Ejemplos:

- `<script>alert(1)</script>`
- `<img src=x onerror=alert(1)>`
- `javascript:alert(1)`

### `COMMAND_INJECTION`

Patrones relevantes:

```regex
[|;&`$]
\$\([^)]+\)
`[^`]+`
\b(cat|ls|dir|pwd|whoami|id|uname|wget|curl|nc|netcat|bash|sh|cmd|powershell)\b
\b(rm|del|copy|move|cp|mv)\s+
\b(ping|nslookup|dig|traceroute|telnet|ftp|ssh)\b
/dev/(tcp|udp)/
\b(nc|ncat)\s+-[elp]
\$\{?[A-Z_]+\}?
\b(type|more|net\s+user|net\s+localgroup)\b
```

Ejemplos:

- `test; cat /etc/passwd`
- `whoami`
- `$(rm -rf /)`

### `PATH_TRAVERSAL`

Patrones relevantes:

```regex
\.\./
\.\.\\
%2e%2e[/\\%]
\.\.%2f
%2e%2e%2f
%00
\x00
^/etc/
^/var/
^/usr/
^[a-zA-Z]:\\
\b(passwd|shadow|hosts|\.htaccess|web\.config|\.env)\b
\\windows\\
\\system32\\
\\boot\.ini
```

Ejemplos:

- `../../../etc/passwd`
- `..\..\windows\system32`
- `%2e%2e%2f`

### `NOSQL_INJECTION`

Patrones relevantes:

```regex
\$where
\$gt
\$lt
\$ne
\$regex
\$or
\$and
\$exists
\{[^}]*\$[a-z]+
function\s*\(
this\.[a-zA-Z]+
\[\$ne\]
\{\s*"\$
'\s*:\s*\{
```

Ejemplos:

- `$where`
- `$ne`
- `{"filter":{"$regex":".*"}}`

### `LDAP_INJECTION`

Patrones relevantes:

```regex
[()\\*\x00]
\(\|
\(&
\(!\(
\b(objectClass|cn|uid|sn|mail)\s*=\s*\*
\*\)\(
```

Ejemplos:

- `*)(uid=*)`
- `(|(cn=*))`

### `XXE`

Patrones relevantes:

```regex
<!DOCTYPE
<!ENTITY
SYSTEM\s+['"]
PUBLIC\s+['"]
file://
php://
expect://
%\w+;
```

Ejemplos:

- `<!DOCTYPE foo>`
- `<!ENTITY xxe SYSTEM "file:///etc/passwd">`

### `SSRF`

Patrones relevantes:

```regex
\b127\.0\.0\.1\b
\blocalhost\b
\b10\.\d{1,3}\.\d{1,3}\.\d{1,3}\b
\b172\.(1[6-9]|2[0-9]|3[0-1])\.\d{1,3}\.\d{1,3}\b
\b192\.168\.\d{1,3}\.\d{1,3}\b
169\.254\.169\.254
metadata\.google\.internal
file:///
gopher://
dict://
```

Ejemplos:

- `http://127.0.0.1`
- `http://localhost`
- `http://169.254.169.254`

## Relación entre `ActionType` y `EntityName`

Ejemplos típicos:

- `ActionType = SECURITY_THREAT`, `EntityName = SQL_INJECTION`
- `ActionType = SECURITY_THREAT`, `EntityName = XSS`
- `ActionType = SECURITY_THREAT`, `EntityName = COMMAND_INJECTION`
- `ActionType = SECURITY_THREAT`, `EntityName = PATH_TRAVERSAL`

Después, si se emite un token honeypot y el atacante lo reutiliza:

- `ActionType = HONEYPOT_ACTIVITY`

Y si entra a endpoints señuelo:

- `ActionType = HONEYPOT_ADMIN_LOGIN_ATTEMPT`
- `ActionType = HONEYPOT_SQL_QUERY_ATTEMPT`
- `ActionType = HONEYPOT_ENV_FILE_ACCESS`
- etc.

## Resumen práctico

- `SECURITY_THREAT` significa que el detector encontró un patrón malicioso.
- `EntityName` identifica qué tipo de patrón fue detectado.
- No toda amenaza genera token honeypot.
- Si además se activa la sesión honeypot, el atacante puede pasar a ser rastreado con `HONEYPOT_ACTIVITY`.
