# Non-Login Honeypot Endpoints

## Objetivo

Este documento lista los endpoints que puedes probar para activar comportamiento honeypot fuera de `/api/Auth/login`.

## Tipos de activación fuera del login

Hay 2 formas principales:

1. Endpoints reales que reciben una amenaza severa y, en `Development`, pueden emitir token honeypot.
2. Endpoints señuelo que no necesariamente emiten token, pero sí registran actividad `HONEYPOT_*`.

## 1. Endpoints reales con amenaza severa

Con la configuración actual de desarrollo, una amenaza severa detectada en cualquier endpoint puede generar:

- `200 OK`
- token honeypot
- `sessionType = "honeypot-threat"`

### `GET /api/Usuario?file=../../../etc/passwd`

Ejemplo:

```http
GET http://localhost:5273/api/Usuario?file=../../../etc/passwd
```

Tipo esperado:

- `EntityName = PATH_TRAVERSAL`

Resultado esperado:

- `ActionType = SECURITY_THREAT`
- posible emisión de token honeypot

### `GET /api/Usuario?filter=' OR 1=1 --`

Ejemplo:

```http
GET http://localhost:5273/api/Usuario?filter=' OR 1=1 --
```

Tipo esperado:

- `EntityName = SQL_INJECTION`

Resultado esperado:

- `ActionType = SECURITY_THREAT`
- posible emisión de token honeypot

### `POST /api/Usuario`

Ejemplo:

```http
POST http://localhost:5273/api/Usuario
Content-Type: application/json
```

```json
{
  "fullName": "test; cat /etc/passwd",
  "email": "trap@demo.com",
  "password": "123456"
}
```

Tipo esperado:

- `EntityName = COMMAND_INJECTION`

Resultado esperado:

- `ActionType = SECURITY_THREAT`
- posible emisión de token honeypot

### Otros endpoints reales recomendados

Puedes repetir el mismo patrón de prueba sobre:

- `POST /api/Contact`
- `POST /api/Categoria`
- `POST /api/Cuenta`
- `POST /api/Transaccion`

Siempre que el payload caiga en un regex de amenaza severa.

## 2. Endpoints señuelo

Estos endpoints están diseñados para atraer atacantes. Normalmente registran eventos `HONEYPOT_*` en `AuditLogs`.

### Administración falsa

- `POST /api/admin/login`
- `GET /api/admin/config`
- `GET /api/admin/backup`
- `GET /api/admin/users`
- `POST /api/admin/execute`
- `POST /api/admin/upload`
- `POST /api/admin/query`

### Archivos sensibles falsos

- `GET /.env`
- `GET /api/.env`
- `GET /app/.env`
- `GET /config.php`
- `GET /wp-config.php`
- `GET /configuration.php`
- `GET /robots.txt`
- `GET /.git/config`
- `GET /.git/HEAD`
- `GET /phpinfo.php`
- `GET /info.php`
- `GET /test.php`
- `GET /api-docs`
- `GET /swagger.json`
- `GET /openapi.json`
- `GET /actuator`
- `GET /actuator/env`
- `GET /actuator/heapdump`

## Ejemplos fáciles de probar

### 1. Archivo `.env` falso

```http
GET http://localhost:5273/.env
```

Resultado esperado:

- respuesta `200`
- contenido falso sensible
- log `HONEYPOT_ENV_FILE_ACCESS`

### 2. Query SQL falsa

```http
POST http://localhost:5273/api/admin/query
Content-Type: application/json
```

```json
{
  "query": "SELECT * FROM users; DROP TABLE users;",
  "database": "production"
}
```

Resultado esperado:

- respuesta `200`
- log `HONEYPOT_SQL_QUERY_ATTEMPT`

### 3. Ejecución de comando falsa

```http
POST http://localhost:5273/api/admin/execute
Content-Type: application/json
```

```json
{
  "command": "whoami",
  "args": "/all"
}
```

Resultado esperado:

- respuesta `200`
- log `HONEYPOT_COMMAND_EXECUTION_ATTEMPT`

### 4. Actuator falso

```http
GET http://localhost:5273/actuator/env
```

Resultado esperado:

- respuesta `200`
- log `HONEYPOT_ACTUATOR_ACCESS`

## Cómo verificar en SQL

### Amenazas detectadas y eventos honeypot

```sql
USE app_tesis;
GO

SELECT TOP 50
    AuditLogId,
    Timestamp,
    ActionType,
    EntityName,
    RequestPath,
    IpAddress,
    ErrorMessage
FROM empresa.AuditLogs
WHERE ActionType = 'SECURITY_THREAT'
   OR ActionType LIKE 'HONEYPOT_%'
ORDER BY AuditLogId DESC;
```

### Sesiones honeypot emitidas

```sql
USE app_honeypot;
GO

SELECT TOP 20
    SessionId,
    AttackerIp,
    InitialThreatType,
    AssignedUserId,
    IsActive,
    StartTime
FROM empresa.HoneypotSessions
ORDER BY SessionId DESC;
```

## Qué probar primero

Te recomiendo esta secuencia:

1. `GET /api/Usuario?file=../../../etc/passwd`
2. `GET /.env`
3. `POST /api/admin/query`
4. `POST /api/Usuario` con payload de command injection

## Resultado esperado por tipo

- Endpoint real con amenaza severa:
  - `SECURITY_THREAT`
  - posible token honeypot
- Endpoint señuelo:
  - `HONEYPOT_*`
  - normalmente sin token

## Nota

Si el endpoint real severo emite token honeypot, luego puedes reutilizarlo como:

```http
Authorization: Bearer {token}
```

Y después revisar en `AuditLogs` los registros `HONEYPOT_ACTIVITY`.
