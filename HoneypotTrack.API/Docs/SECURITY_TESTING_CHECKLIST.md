# Security Testing Checklist

## Objetivo

Esta guía resume pruebas simples para validar la detección de amenazas, el honeypot y los logs de auditoría.

Se divide en 2 partes:

- pruebas directas contra la API usando Postman
- pruebas desde el frontend web

---

## Antes de empezar

## Requisitos

- API levantada en `http://localhost:5273`
- frontend levantado en `http://localhost:4200`
- acceso a la base de datos para revisar `empresa.AuditLogs`
- un token JWT válido para endpoints protegidos

## Cómo obtener token

Haz un login normal:

`POST http://localhost:5273/api/Auth/login`

```json
{
  "email": "vp@empresa.com",
  "password": "12345"
}
```

Luego copia `data.token` y úsalo como:

```http
Authorization: Bearer TU_TOKEN
```

## Consulta SQL útil

```sql
SELECT TOP 50
    AuditLogId,
    Timestamp,
    ActionType,
    EntityName,
    RequestPath,
    StatusCode,
    ErrorMessage
FROM empresa.AuditLogs
ORDER BY AuditLogId DESC;
```

---

## Parte 1: Pruebas por API con Postman

## Cómo usar Postman

- selecciona el método HTTP correcto
- pega solo la URL, sin escribir `GET` o `POST` dentro del campo URL
- si el endpoint requiere auth, usa la pestaña `Authorization`
- en `Type` selecciona `Bearer Token`
- pega el JWT en `Token`

---

## 1. Login con SQL Injection

**Endpoint**

`POST http://localhost:5273/api/Auth/login`

**Body**

```json
{
  "email": "' OR 1=1--",
  "password": "x"
}
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = SQL_INJECTION`
- si aplica honeypot, puede devolver login falso

---

## 2. Categoria con XSS

**Endpoint**

`PUT http://localhost:5273/api/Categoria`

**Body**

```json
{
  "categoryId": 1,
  "name": "<script>alert('xss')</script>",
  "operationType": "Egreso"
}
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = XSS`
- el log debería apuntar a `Body:name`

---

## 3. Categoria con SQL Injection

**Endpoint**

`PUT http://localhost:5273/api/Categoria`

**Body**

```json
{
  "categoryId": 1,
  "name": "' OR 1=1--",
  "operationType": "Egreso"
}
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = SQL_INJECTION`

---

## 4. Categoria con Path Traversal

**Endpoint**

`PUT http://localhost:5273/api/Categoria`

**Body**

```json
{
  "categoryId": 1,
  "name": "../../../etc/passwd",
  "operationType": "Egreso"
}
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = PATH_TRAVERSAL`

---

## 5. Categoria con Command Injection

**Endpoint**

`PUT http://localhost:5273/api/Categoria`

**Body**

```json
{
  "categoryId": 1,
  "name": "test; cat /etc/passwd",
  "operationType": "Egreso"
}
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = COMMAND_INJECTION`

---

## 6. Usuario con query maliciosa

**Endpoint**

`GET http://localhost:5273/api/Usuario?file=../../../etc/passwd`

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = PATH_TRAVERSAL`

---

## 7. Usuario con SQL Injection en query

**Endpoint**

`GET http://localhost:5273/api/Usuario?filter=' OR 1=1--`

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = SQL_INJECTION`

---

## 8. Usuario con SSRF

**Endpoint**

`GET http://localhost:5273/api/Usuario?url=http://169.254.169.254/latest/meta-data`

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = SSRF`

---

## 9. Endpoint honeypot de archivo sensible

**Endpoint**

`GET http://localhost:5273/.env`

**Esperado**

- `ActionType = HONEYPOT_ENV_FILE_ACCESS`
- respuesta con contenido falso sensible

---

## 10. Endpoint honeypot de admin falso

**Endpoint**

`POST http://localhost:5273/api/admin/query`

**Body**

```json
{
  "query": "SELECT * FROM users; DROP TABLE users;",
  "database": "production"
}
```

**Esperado**

- `ActionType = HONEYPOT_SQL_QUERY_ATTEMPT`

---

## Parte 2: Pruebas desde el frontend

## Objetivo

Estas pruebas sirven para validar:

- el flujo normal de la app
- detección de amenazas enviadas desde formularios
- detección contextual desde URLs del frontend

---

## 1. Navegación normal

Prueba estas rutas:

- `http://localhost:4200/categorias`
- `http://localhost:4200/usuarios`
- `http://localhost:4200/cuentas`

**Esperado**

- la UI debe cargar normal
- en BD deben aparecer logs normales como `READ`
- no deberían aparecer amenazas por navegación limpia

---

## 2. Editar categoría con XSS

En:

`http://localhost:4200/categorias/2/editar`

Pon en `Nombre`:

```text
<script>alert('xss')</script>
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = XSS`
- el log debería indicar `Body:name`

---

## 3. Editar categoría con SQL Injection

En `Nombre`:

```text
' OR 1=1--
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = SQL_INJECTION`

---

## 4. Editar categoría con Path Traversal

En `Nombre`:

```text
../../../etc/passwd
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = PATH_TRAVERSAL`

---

## 5. Editar categoría con Command Injection

En `Nombre`:

```text
test; cat /etc/passwd
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = COMMAND_INJECTION`

---

## 6. Login malicioso desde el frontend

En la pantalla de login usa:

**Email**

```text
' OR 1=1--
```

**Password**

```text
x
```

**Esperado**

- `ActionType = SECURITY_THREAT`
- `EntityName = SQL_INJECTION`
- según configuración, puede activar honeypot auth

---

## 7. Probar desde la URL del frontend

Ejemplos:

- `http://localhost:4200/categorias?file=../../../etc/passwd`
- `http://localhost:4200/categorias?filter=' OR 1=1--`
- `http://localhost:4200/categorias?url=http://169.254.169.254/latest/meta-data`

**Importante**

Estas pruebas no siempre llegan al endpoint real de la API como query params.

Pueden servir para:

- detección contextual por `Referer`
- validar si el frontend conserva o propaga esos parámetros

Si Angular limpia u omite esos query params, la forma más confiable sigue siendo usar formularios o Postman.

---

## Cómo interpretar resultados

## Si ves `READ`, `UPDATE`, `CREATE`

El flujo funcional de la app se ejecutó normalmente.

## Si ves `SECURITY_THREAT`

Se detectó una amenaza en:

- `Body:campo`
- `Query:param`
- `RefererQuery:param`
- otros campos analizados

## Si ves `HONEYPOT_*`

Entraste en un endpoint señuelo o activaste un flujo honeypot.

## Si ves `HONEYPOT_ACTIVITY`

La navegación se hizo usando un token honeypot.

---

## Recomendación de batería mínima

Si quieres una validación rápida y clara, prueba solo esto:

1. `POST /api/Auth/login` con SQL injection
2. `PUT /api/Categoria` con `<script>alert('xss')</script>`
3. `PUT /api/Categoria` con `../../../etc/passwd`
4. `GET /.env`
5. editar categoría desde frontend con `test; cat /etc/passwd`

Con eso validas:

- detección de `SQL_INJECTION`
- detección de `XSS`
- detección de `PATH_TRAVERSAL`
- honeypot de archivo sensible
- detección desde formulario web real
