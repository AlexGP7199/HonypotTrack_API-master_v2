# Honeypot Testing Guide

## Objetivo

Esta guía resume cómo activar y validar el honeypot localmente en `HoneypotTrack.API`, tanto por login como por endpoints señuelo y detección de amenazas.

## Prerrequisitos

- API corriendo en `http://localhost:5273`
- Base principal `app_tesis` operativa
- Base honeypot `app_honeypot` operativa
- Tabla `empresa.HoneypotSessions` creada con `SessionId INT IDENTITY(1,1)`
- Usuarios falsos cargados en `app_honeypot.empresa.Usuarios`
- Entorno `Development`

## Arranque local

```powershell
cd "C:\Users\dgomez\Downloads\HonypotTrack_API-master_V3\HonypotTrack_API-master\HoneypotTrack.API"
dotnet run -p:TreatWarningsAsErrors=false -p:NuGetAudit=false
```

## Qué activa el honeypot

### 1. Login malicioso en endpoints de autenticación

Aplica sobre:

- `POST /api/Auth/login`
- `POST /api/Auth/register`
- `POST /api/Auth/forgot-password` si existiera

Se activa cuando:

- se detecta una amenaza con severidad `>= 7`
- el request va a un endpoint de autenticación
- o se usa el header de prueba manual en desarrollo

### 2. Honeypot conductual por intentos fallidos

En login, también se puede activar al fallar 3 veces con la misma combinación:

- misma IP
- mismo email
- dentro de 10 minutos

### 2.1. Emisión de token honeypot fuera de login

Si `Security:Honeypot:IssueSessionOnAnyHighSeverityThreat` está en `true`, una amenaza severa detectada en cualquier endpoint también puede devolver un token honeypot.

Configuración actual:

- `Development`: `true`
- `Production`: `false`

Esto permite probar localmente escenarios donde un `GET` o `POST` malicioso recibe una sesión señuelo y luego toda su navegación queda trazada como `HONEYPOT_ACTIVITY`.

## Cómo clasifica una amenaza

El flujo es este:

1. `SecurityMiddleware` analiza la petición.
2. Si detecta un patrón malicioso, registra un evento con:
   - `ActionType = SECURITY_THREAT`
   - `EntityName = tipo de amenaza detectada`
3. Si además la amenaza cumple las reglas de activación del honeypot, emite una sesión/token honeypot.

Los campos que se analizan normalmente son:

- `Request.Path`
- `QueryString`
- headers sospechosos
- campos del body JSON

En endpoints de autenticación, algunos campos como `password` y `token` se excluyen para evitar falsos positivos.

## Patrones por `EntityName`

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

### 3. Endpoints señuelo

Estos no crean token honeypot, pero sí registran actividad trampa en auditoría:

- `POST /api/admin/login`
- `GET /api/admin/config`
- `GET /api/admin/backup`
- `GET /api/admin/users`
- `POST /api/admin/execute`
- `POST /api/admin/upload`
- `POST /api/admin/query`
- `GET /.env`
- `GET /config.php`
- `GET /wp-config.php`
- `GET /robots.txt`
- `GET /.git/config`
- `GET /phpinfo.php`
- `GET /api-docs`
- `GET /swagger.json`
- `GET /openapi.json`
- `GET /actuator`
- `GET /actuator/env`
- `GET /actuator/heapdump`

## Pruebas recomendadas

### Prueba 1. Activación manual del honeypot en login

Request:

```http
POST /api/Auth/login
X-Test-Honeypot: true
Content-Type: application/json
```

```json
{
  "email": "test@test.com",
  "password": "123456"
}
```

Resultado esperado:

- `200 OK`
- `message: "Login exitoso"`
- `token` falso
- `refreshToken`
- `user.role = "Admin"`

### Prueba 2. SQL Injection en login

Request:

```http
POST /api/Auth/login
Content-Type: application/json
```

```json
{
  "email": "' OR 1=1--",
  "password": "x"
}
```

Resultado esperado en desarrollo:

- activación del honeypot
- respuesta `200 OK` con token falso

Si falla la sesión honeypot:

- respuesta `500` con detalle del motivo

### Prueba 3. Honeypot conductual por credenciales erróneas

Enviar 3 veces seguidas:

```json
{
  "email": "usuario-inexistente@demo.com",
  "password": "clave-mala"
}
```

Resultado esperado:

- intento 1: `401`
- intento 2: `401`
- intento 3: `200 OK` con login honeypot falso

Importante:

- debe ser el mismo `email`
- debe ser la misma IP
- no cambies el email entre intentos

### Prueba 4. Endpoint admin falso

Request:

```http
POST /api/admin/login
Content-Type: application/json
```

```json
{
  "username": "admin",
  "password": "admin123"
}
```

Resultado esperado:

- `401 Unauthorized`
- se registra `HONEYPOT_ADMIN_LOGIN_ATTEMPT` en `AuditLogs`

### Prueba 4.1. Amenaza severa fuera de login con token honeypot

Request:

```http
GET /api/Usuario?file=../../../etc/passwd
```

Resultado esperado en desarrollo:

- `200 OK`
- respuesta JSON con `token`
- `sessionType = "honeypot-threat"`

Otro ejemplo:

```http
POST /api/Usuario
Content-Type: application/json
```

```json
{
  "fullName": "test; cat /etc/passwd",
  "email": "trap@demo.com",
  "password": "123456"
}
```

Resultado esperado en desarrollo:

- `200 OK`
- respuesta JSON con token honeypot

### Prueba 5. Query SQL falsa

Request:

```http
POST /api/admin/query
Content-Type: application/json
```

```json
{
  "query": "SELECT * FROM users; DROP TABLE users;",
  "database": "production"
}
```

Resultado esperado:

- `200 OK`
- se registra `HONEYPOT_SQL_QUERY_ATTEMPT`

### Prueba 6. Ejecución de comando falsa

Request:

```http
POST /api/admin/execute
Content-Type: application/json
```

```json
{
  "command": "whoami",
  "args": "/all"
}
```

Resultado esperado:

- `200 OK`
- se registra `HONEYPOT_COMMAND_EXECUTION_ATTEMPT`

### Prueba 7. Archivo sensible falso

Request:

```http
GET /.env
```

Resultado esperado:

- `200 OK`
- contenido falso de variables sensibles
- se registra `HONEYPOT_ENV_FILE_ACCESS`

### Prueba 8. Actuator falso

Request:

```http
GET /actuator/env
```

Resultado esperado:

- `200 OK`
- payload falso estilo Spring Boot
- se registra `HONEYPOT_ACTUATOR_ACCESS`

## Qué validar en base de datos

### Sesiones honeypot creadas

```sql
USE app_honeypot;
GO

SELECT TOP 20 *
FROM empresa.HoneypotSessions
ORDER BY SessionId DESC;
```

### Eventos de seguridad y honeypot

```sql
USE app_tesis;
GO

SELECT TOP 50
    AuditLogId,
    Timestamp,
    ActionType,
    EntityName,
    IpAddress,
    RequestPath,
    ErrorMessage
FROM empresa.AuditLogs
WHERE ActionType = 'SECURITY_THREAT'
   OR ActionType LIKE 'HONEYPOT_%'
ORDER BY AuditLogId DESC;
```

### Actividad con token honeypot

Después de obtener un token falso desde `/api/Auth/login`, úsalo en cualquier endpoint:

```http
Authorization: Bearer {token_honeypot}
```

Luego revisa:

```sql
USE app_tesis;
GO

SELECT TOP 50
    AuditLogId,
    Timestamp,
    ActionType,
    RequestPath,
    SessionId,
    IpAddress
FROM empresa.AuditLogs
WHERE ActionType = 'HONEYPOT_ACTIVITY'
ORDER BY AuditLogId DESC;
```

## Señales de que algo está mal

- `401` en SQL injection de login cuando esperabas `200`
- `500` con error de conexión o escritura en honeypot
- `0` filas en `app_honeypot.empresa.Usuarios`
- `0` filas nuevas en `empresa.HoneypotSessions`
- no aparecen eventos `HONEYPOT_%` en `app_tesis.empresa.AuditLogs`

## Notas útiles

- Los endpoints ocultos no salen en Swagger.
- En `Development` puedes usar `X-Test-Honeypot: true` para forzar la activación.
- En `Development` también puedes simular una amenaza con:

```http
X-Simulate-Threat: SQL_INJECTION
```

- El honeypot de login usa la base `app_honeypot`.
- La auditoría del honeypot y de amenazas se guarda en `app_tesis`.
