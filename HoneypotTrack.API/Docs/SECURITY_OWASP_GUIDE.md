# ??? Sistema de Seguridad OWASP Top 10 - HoneyPot API

## Descripción General

HoneypotTrack API es un **sistema HoneyPot completo** diseñado para detectar, registrar y analizar intentos de ataque basados en las vulnerabilidades del **OWASP Top 10 2021**. El sistema incluye endpoints señuelo, detección de amenazas, bloqueo de IPs, alertas en tiempo real y un dashboard de monitoreo.

---

## ?? Tabla de Contenidos

1. [Amenazas Detectadas](#amenazas-detectadas)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Sistema de Bloqueo de IPs](#sistema-de-bloqueo-de-ips)
4. [Whitelist (Lista Blanca)](#whitelist-lista-blanca)
5. [Configuración](#configuración)
6. [Auditoría de Amenazas](#auditoría-de-amenazas)
7. [Ejemplos de Prueba](#ejemplos-de-prueba)
8. [API de Administración](#api-de-administración)
9. [Mejores Prácticas](#mejores-prácticas)

---

## ?? Amenazas Detectadas

El sistema detecta las siguientes categorías de ataques según OWASP Top 10:

| Categoría OWASP | Tipo de Ataque | Severidad | Descripción |
|-----------------|----------------|-----------|-------------|
| **A01:2021** | Path Traversal | 8/10 | Intentos de acceder a archivos fuera del directorio permitido (`../`, `..\\`) |
| **A03:2021** | SQL Injection | 9/10 | Inyección de código SQL malicioso (`' OR 1=1 --`, `UNION SELECT`) |
| **A03:2021** | XSS | 8/10 | Cross-Site Scripting (`<script>`, `onerror=`) |
| **A03:2021** | Command Injection | 10/10 | Inyección de comandos del sistema (`; rm -rf`, `| cat /etc/passwd`) |
| **A03:2021** | NoSQL Injection | 8/10 | Inyección en bases de datos NoSQL (`$where`, `$ne`) |
| **A03:2021** | LDAP Injection | 7/10 | Inyección en consultas LDAP (`)(`, `*)(`) |
| **A03:2021** | XXE | 8/10 | XML External Entity (`<!DOCTYPE`, `<!ENTITY`) |
| **A10:2021** | SSRF | 8/10 | Server-Side Request Forgery (`169.254.169.254`, `localhost`) |

### Patrones Detectados por Tipo

#### SQL Injection
```
-- Comentarios SQL
' OR 1=1 --
UNION SELECT * FROM users
'; DROP TABLE users; --
WAITFOR DELAY '0:0:5'
```

#### XSS (Cross-Site Scripting)
```html
<script>alert('XSS')</script>
<img src=x onerror=alert(1)>
javascript:alert(1)
<svg onload=alert(1)>
```

#### Command Injection
```bash
; cat /etc/passwd
| whoami
`id`
$(rm -rf /)
```

#### Path Traversal
```
../../../etc/passwd
..\..\windows\system32
%2e%2e%2f
```

---

## ??? Arquitectura del Sistema

```
???????????????????????????????????????????????????????????????
?                    HTTP Request                              ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?              SecurityMiddleware                              ?
?  ???????????????????????????????????????????????????????    ?
?  ?  1. Verificar si IP está bloqueada                   ?    ?
?  ?  2. Verificar si IP está en whitelist                ?    ?
?  ?  3. Analizar URL, Headers, Body                      ?    ?
?  ?  4. Detectar patrones maliciosos                     ?    ?
?  ?  5. Registrar amenazas en AuditLog                   ?    ?
?  ?  6. Bloquear IP si es necesario                      ?    ?
?  ???????????????????????????????????????????????????????    ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?              AuditMiddleware                                 ?
?         (Registro completo de la petición)                   ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?                    Controllers                               ?
???????????????????????????????????????????????????????????????
```

### Archivos del Sistema

| Archivo | Descripción |
|---------|-------------|
| `Security/SecurityThreatDetector.cs` | Detector de patrones maliciosos |
| `Security/SecurityMiddleware.cs` | Middleware de seguridad |
| `Middlewares/AuditMiddleware.cs` | Middleware de auditoría |

---

## ?? Sistema de Bloqueo de IPs

### Niveles de Bloqueo

El sistema implementa un bloqueo **progresivo** basado en el comportamiento:

| Nivel | Condición | Acción | Duración |
|-------|-----------|--------|----------|
| **Crítico** | Amenaza con severidad ? 9 | Bloqueo inmediato | 1 hora |
| **Advertencia** | 3 amenazas detectadas | Bloqueo temporal | 5 minutos |
| **Bloqueo** | 5+ amenazas detectadas | Bloqueo extendido | 30 minutos |

### Flujo de Bloqueo

```
Amenaza Detectada
       ?
       ?
????????????????????
? ¿Severidad ? 9?  ?
????????????????????
         ?
    ???????????
    ? Sí      ? No
    ?         ?
???????????  ????????????????????
? Bloqueo ?  ? Incrementar      ?
? 1 hora  ?  ? contador         ?
???????????  ????????????????????
                      ?
              ?????????????????
              ? ¿Contador ? 5??
              ?????????????????
                      ?
                 ???????????
                 ? Sí      ? No
                 ?         ?
           ???????????  ????????????????????
           ? Bloqueo ?  ? ¿Contador ? 3?   ?
           ? 30 min  ?  ????????????????????
           ???????????           ?
                            ???????????
                            ? Sí      ? No
                            ?         ?
                      ???????????  ???????????
                      ? Bloqueo ?  ? Solo    ?
                      ? 5 min   ?  ? Log     ?
                      ???????????  ???????????
```

### Respuesta de IP Bloqueada

Cuando una IP bloqueada intenta acceder:

```http
HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "error": "Acceso denegado",
  "message": "Tu IP ha sido bloqueada temporalmente por actividad sospechosa",
  "code": "IP_BLOCKED"
}
```

---

## ? Whitelist (Lista Blanca)

### IPs Automáticamente en Whitelist

En **modo Desarrollo** (`ASPNETCORE_ENVIRONMENT=Development`), las siguientes IPs **nunca serán bloqueadas**:

| IP/Rango | Descripción |
|----------|-------------|
| `127.0.0.1` | Localhost IPv4 |
| `::1` | Localhost IPv6 |
| `192.168.x.x` | Red local clase C |
| `10.x.x.x` | Red local clase A |

### Comportamiento con Whitelist

| Situación | Amenaza Detectada | Bloqueo | Registro en Auditoría |
|-----------|-------------------|---------|----------------------|
| IP en whitelist | ? Sí | ? NO | ? SÍ |
| IP normal (producción) | ? Sí | ? SÍ | ? SÍ |

> **Importante**: Las amenazas **siempre se registran** en la auditoría, incluso para IPs en whitelist. Solo se omite el bloqueo.

---

## ?? Configuración

### appsettings.Development.json

```json
{
  "Security": {
    "WhitelistedIps": [
      "127.0.0.1",
      "::1",
      "192.168.1.100"
    ],
    "EnableBlocking": false
  }
}
```

### appsettings.json (Producción)

```json
{
  "Security": {
    "WhitelistedIps": [
      "10.0.0.1"
    ],
    "EnableBlocking": true
  }
}
```

### Variables de Entorno

```bash
# Deshabilitar bloqueo en desarrollo
ASPNETCORE_ENVIRONMENT=Development
```

---

## ?? Auditoría de Amenazas

### Registro en Base de Datos

Todas las amenazas detectadas se registran en la tabla `AuditLogs` con:

| Campo | Valor |
|-------|-------|
| `ActionType` | `SECURITY_THREAT` |
| `EntityName` | Tipo de amenaza (ej: `SQL_INJECTION`) |
| `ErrorMessage` | Descripción de la amenaza |
| `ExceptionDetails` | Patrón detectado y severidad |

### Consulta SQL para Ver Amenazas

```sql
-- Ver todas las amenazas detectadas
SELECT 
    Timestamp,
    IpAddress,
    EntityName AS ThreatType,
    ErrorMessage AS Description,
    ExceptionDetails AS Details,
    RequestUrl,
    UserAgent
FROM empresa.AuditLogs 
WHERE ActionType = 'SECURITY_THREAT'
ORDER BY Timestamp DESC;

-- Resumen de amenazas por tipo
SELECT 
    EntityName AS ThreatType,
    COUNT(*) AS TotalAttempts,
    COUNT(DISTINCT IpAddress) AS UniqueIPs
FROM empresa.AuditLogs 
WHERE ActionType = 'SECURITY_THREAT'
GROUP BY EntityName
ORDER BY TotalAttempts DESC;

-- IPs con más intentos de ataque
SELECT 
    IpAddress,
    COUNT(*) AS TotalThreats,
    STRING_AGG(DISTINCT EntityName, ', ') AS ThreatTypes
FROM empresa.AuditLogs 
WHERE ActionType = 'SECURITY_THREAT'
GROUP BY IpAddress
HAVING COUNT(*) > 3
ORDER BY TotalThreats DESC;
```

---

## ?? Ejemplos de Prueba

### SQL Injection

```bash
# Intento de SQL Injection en query string
curl "http://localhost:5273/api/Usuario?filter=' OR 1=1 --"

# Intento de SQL Injection en body
curl -X POST "http://localhost:5273/api/Usuario" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "'; DROP TABLE Users; --"}'
```

### XSS (Cross-Site Scripting)

```bash
# Intento de XSS en body
curl -X POST "http://localhost:5273/api/Usuario" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "<script>alert(document.cookie)</script>"}'

# Intento de XSS con evento
curl -X POST "http://localhost:5273/api/Usuario" \
  -H "Content-Type: application/json" \
  -d '{"email": "test@test.com\" onmouseover=\"alert(1)"}'
```

### Path Traversal

```bash
# Intento de Path Traversal
curl "http://localhost:5273/api/Usuario?file=../../../etc/passwd"
curl "http://localhost:5273/api/Usuario?path=..\..\windows\system32"
```

### Command Injection

```bash
# Intento de Command Injection
curl -X POST "http://localhost:5273/api/Usuario" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "test; cat /etc/passwd"}'
```

### Respuesta Esperada (Amenaza Crítica)

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json
X-Security-Warning: Suspicious activity detected

{
  "error": "Solicitud rechazada",
  "message": "Se ha detectado contenido malicioso en tu solicitud",
  "code": "MALICIOUS_REQUEST",
  "threatType": "SQL_INJECTION"
}
```

---

## ?? API de Administración

### Métodos Disponibles (Programáticos)

```csharp
// Desbloquear una IP manualmente
SecurityMiddleware.UnblockIp("192.168.1.100");

// Ver todas las IPs bloqueadas
var blockedIps = SecurityMiddleware.GetBlockedIps();
foreach (var ip in blockedIps)
{
    Console.WriteLine($"IP: {ip.Key}");
    Console.WriteLine($"  Bloqueada hasta: {ip.Value.BlockedUntil}");
    Console.WriteLine($"  Razón: {ip.Value.Reason}");
    Console.WriteLine($"  Intentos: {ip.Value.ThreatCount}");
}

// Agregar IP a whitelist en runtime
SecurityMiddleware.AddToWhitelist("192.168.1.100");

// Remover IP de whitelist
SecurityMiddleware.RemoveFromWhitelist("192.168.1.100");
```

### Endpoint de Administración (Opcional)

Si deseas crear un endpoint de administración:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Proteger con autorización
public class SecurityController : ControllerBase
{
    [HttpGet("blocked-ips")]
    public IActionResult GetBlockedIps()
    {
        var blocked = SecurityMiddleware.GetBlockedIps();
        return Ok(blocked.Select(x => new
        {
            Ip = x.Key,
            BlockedUntil = x.Value.BlockedUntil,
            Reason = x.Value.Reason,
            ThreatCount = x.Value.ThreatCount
        }));
    }

    [HttpDelete("blocked-ips/{ip}")]
    public IActionResult UnblockIp(string ip)
    {
        var result = SecurityMiddleware.UnblockIp(ip);
        return result ? Ok() : NotFound();
    }
}
```

---

## ?? Mejores Prácticas

### Desarrollo

1. **Usa `ASPNETCORE_ENVIRONMENT=Development`** para evitar bloqueos durante pruebas
2. **Configura tu IP en whitelist** si trabajas desde una máquina remota
3. **Revisa los logs de auditoría** para entender qué patrones se detectan
4. **Reinicia la app** para limpiar el cache de IPs bloqueadas (almacenado en memoria)

### Producción

1. **Habilita el bloqueo** (`EnableBlocking: true`)
2. **Configura alertas** para amenazas detectadas
3. **Implementa persistencia** de IPs bloqueadas en Redis/DB para clusters
4. **Monitorea regularmente** los logs de seguridad
5. **Configura rate limiting** adicional para endpoints sensibles

### Falsos Positivos

Algunos patrones pueden generar falsos positivos. Si necesitas permitir ciertos patrones legítimos:

1. Revisa el patrón en `SecurityThreatDetector.cs`
2. Ajusta la expresión regular si es necesario
3. O agrega una excepción específica para el endpoint

---

## ?? Endpoints HoneyPot (Señuelos)

El sistema incluye endpoints señuelo que parecen vulnerables para atraer atacantes:

### Endpoints de Administración Falsos

| Endpoint | Descripción | Comportamiento |
|----------|-------------|----------------|
| `POST /api/admin/login` | Login de admin falso | Captura credenciales intentadas |
| `GET /api/admin/config` | Configuración falsa | Devuelve API keys falsas |
| `GET /api/admin/users` | Lista de usuarios falsa | Devuelve usuarios ficticios |
| `GET /api/admin/backup` | Backup falso | Simula iniciar backup |
| `POST /api/admin/execute` | Ejecución de comandos | Captura comandos maliciosos |
| `POST /api/admin/query` | Query SQL directo | Captura intentos de SQL injection |

### Archivos Sensibles Falsos

| Endpoint | Descripción |
|----------|-------------|
| `GET /.env` | Variables de entorno falsas |
| `GET /config.php` | Configuración PHP falsa |
| `GET /wp-config.php` | WordPress config falso |
| `GET /.git/config` | Configuración Git falsa |
| `GET /robots.txt` | Robots.txt con rutas "secretas" |
| `GET /phpinfo.php` | Información PHP falsa |
| `GET /actuator/env` | Spring Boot actuator falso |

> **Nota**: Estos endpoints están ocultos de Swagger pero son descubiertos por escaneos automatizados.

---

## ?? Dashboard de Seguridad

### Endpoints Disponibles

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/SecurityDashboard/stats` | GET | Estadísticas generales |
| `/api/SecurityDashboard/attacks` | GET | Lista de ataques recientes |
| `/api/SecurityDashboard/attacker/{ip}` | GET | Perfil del atacante |
| `/api/SecurityDashboard/blocked-ips` | GET | IPs bloqueadas |
| `/api/SecurityDashboard/blocked-ips/{ip}` | DELETE | Desbloquear IP |
| `/api/SecurityDashboard/whitelist/{ip}` | POST | Agregar a whitelist |
| `/api/SecurityDashboard/realtime` | GET | Métricas en tiempo real |
| `/api/SecurityDashboard/alerts` | GET | Alertas recientes |

### Ejemplo de Respuesta - Estadísticas

```json
{
  "period": "Last 7 days",
  "totalThreats": 45,
  "totalHoneypotTriggers": 12,
  "uniqueAttackerIPs": 8,
  "threatsByType": {
    "SQL_INJECTION": 20,
    "XSS": 15,
    "PATH_TRAVERSAL": 10
  },
  "topAttackerIPs": [
    {
      "ipAddress": "192.168.1.100",
      "totalAttempts": 25,
      "riskScore": 85
    }
  ]
}
```

### Ejemplo de Respuesta - Perfil del Atacante

```json
{
  "ipAddress": "192.168.1.100",
  "totalAttempts": 25,
  "firstSeen": "2026-03-01T10:00:00Z",
  "lastSeen": "2026-03-09T15:30:00Z",
  "isCurrentlyBlocked": true,
  "riskScore": 85,
  "threatTypes": {
    "SQL_INJECTION": 15,
    "XSS": 8,
    "HONEYPOT_ADMIN_LOGIN_ATTEMPT": 2
  },
  "targetedEndpoints": {
    "/api/Usuario": 10,
    "/api/admin/login": 5
  }
}
```

---

## ?? Sistema de Alertas

### Configuración de Alertas

En `appsettings.json`:

```json
{
  "Alerts": {
    "Email": {
      "Enabled": true,
      "SmtpHost": "smtp.example.com",
      "SmtpPort": 587,
      "FromEmail": "alerts@honeypottrack.com",
      "ToEmail": "admin@honeypottrack.com"
    },
    "Webhook": {
      "Url": "https://discord.com/api/webhooks/..."
    }
  }
}
```

### Niveles de Severidad

| Nivel | Valor | Cuando se usa |
|-------|-------|---------------|
| `Low` | 1 | Amenazas menores |
| `Medium` | 2 | Amenazas moderadas |
| `High` | 3 | Amenazas significativas |
| `Critical` | 4 | Amenazas críticas (notificación inmediata) |

### Webhook para Discord/Slack

Las alertas se envían en formato compatible con Discord y Slack:

```json
{
  "content": "?? **Security Alert**",
  "embeds": [{
    "title": "SQL Injection Attempt",
    "description": "Posible intento de SQL Injection detectado",
    "color": 15158332,
    "fields": [
      {"name": "Severity", "value": "Critical"},
      {"name": "IP", "value": "192.168.1.100"}
    ]
  }]
}
```

---

## ?? Soporte

Si tienes dudas sobre el sistema de seguridad:

1. Revisa los logs en la tabla `AuditLogs`
2. Verifica la configuración en `appsettings.json`
3. Consulta este documento

---

## ?? Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2026-03-09 | Implementación inicial del sistema de seguridad OWASP Top 10 |
| 1.1.0 | 2026-03-09 | Añadidos endpoints HoneyPot, Dashboard de seguridad, Sistema de alertas, Perfil del atacante |

