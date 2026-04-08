# Middleware Flow Guide

## Objetivo

Este documento explica de forma sencilla cómo funciona el middleware de seguridad y honeypot en `HoneypotTrack.API`.

## Vista general

La API tiene dos piezas principales:

- `SecurityMiddleware`
- `HoneypotMiddleware`

Ambos trabajan juntos para:

- detectar amenazas
- registrar alertas
- activar sesiones honeypot cuando aplica
- seguir la actividad del atacante

## 1. Qué hace `SecurityMiddleware`

`SecurityMiddleware` es la primera capa importante de análisis.

Su trabajo es:

1. leer la petición
2. revisar si hay patrones maliciosos
3. clasificar la amenaza
4. guardar el evento en auditoría
5. decidir si debe activar el honeypot

## 2. Qué partes de la petición analiza

El middleware revisa:

- la ruta (`Request.Path`)
- el query string
- algunos headers sospechosos
- el body JSON

Ejemplos:

- `?file=../../../etc/passwd`
- `?filter=' OR 1=1 --`
- body con `<script>alert(1)</script>`
- body con `test; cat /etc/passwd`

## 3. Cómo decide que es una amenaza

El middleware usa expresiones regulares para comparar la petición contra patrones conocidos.

Si encuentra coincidencia:

- marca el evento como `ActionType = SECURITY_THREAT`
- asigna un `EntityName` según el tipo detectado

## 4. Qué significa cada campo

### `ActionType`

Describe el tipo general de evento.

Ejemplos:

- `SECURITY_THREAT`
- `HONEYPOT_ACTIVITY`
- `HONEYPOT_ADMIN_LOGIN_ATTEMPT`
- `HONEYPOT_ENV_FILE_ACCESS`

### `EntityName`

Describe qué clase de amenaza se detectó cuando el evento es de seguridad.

Ejemplos:

- `SQL_INJECTION`
- `XSS`
- `COMMAND_INJECTION`
- `PATH_TRAVERSAL`
- `NOSQL_INJECTION`
- `LDAP_INJECTION`
- `XXE`
- `SSRF`

## 5. Cuándo genera una alerta

Cuando detecta una amenaza:

- la guarda en `AuditLogs`
- genera una alerta interna con severidad

La severidad depende del tipo detectado:

- severidad alta o crítica: SQL Injection, Command Injection, etc.
- severidad media o baja: según el patrón

## 6. Cómo sabe qué tipo de alerta es

El flujo es:

1. detecta un patrón malicioso
2. clasifica el tipo
3. asigna severidad
4. registra el evento

Ejemplo:

Si llega esto:

```http
GET /api/Usuario?file=../../../etc/passwd
```

Entonces:

- `ActionType = SECURITY_THREAT`
- `EntityName = PATH_TRAVERSAL`

Otro ejemplo:

```json
{
  "email": "' OR 1=1--",
  "password": "x"
}
```

Entonces:

- `ActionType = SECURITY_THREAT`
- `EntityName = SQL_INJECTION`

## 7. Cuándo entra el honeypot

No toda amenaza entra automáticamente al honeypot.

Primero se detecta y se registra.

Luego el middleware evalúa si debe crear una sesión honeypot.

Eso depende de cosas como:

- severidad de la amenaza
- configuración del entorno
- si es endpoint de autenticación
- si está habilitado emitir sesión honeypot para amenazas severas

Si se activa:

- crea una sesión en `app_honeypot`
- devuelve un token honeypot
- responde con una sesión falsa

## 8. Qué hace `HoneypotMiddleware`

Después de emitirse un token honeypot, entra en juego `HoneypotMiddleware`.

Su función es:

- detectar si el token enviado es honeypot
- marcar la petición como actividad de atacante
- registrar la navegación posterior

Es decir:

- `SecurityMiddleware` detecta y captura
- `HoneypotMiddleware` sigue y registra

## 9. Diferencia entre alerta y sesión honeypot

### Solo alerta

Pasa cuando:

- se detecta algo sospechoso
- se registra como `SECURITY_THREAT`
- pero no se activa una sesión honeypot

### Sesión honeypot

Pasa cuando:

- se detecta una amenaza
- además cumple las condiciones de activación
- la API entrega token honeypot

## 10. Casos comunes

### Caso 1. Login malicioso

- se detecta amenaza
- puede crearse sesión honeypot
- el atacante recibe token falso

### Caso 2. Endpoint señuelo

- no necesariamente pasa por el mismo flujo de sesión
- pero sí registra eventos `HONEYPOT_*`

### Caso 3. Usuario autenticado real haciendo algo malicioso

- se recomienda registrar alerta
- guardar `SECURITY_THREAT`
- no cambiar automáticamente su sesión, a menos que la estrategia del sistema lo decida

## 11. Resumen simple para explicar el sistema

Puedes explicarlo así:

1. La API inspecciona cada petición.
2. Si detecta un patrón peligroso, la marca como amenaza.
3. Luego decide qué tipo de amenaza fue.
4. Registra el evento y genera una alerta.
5. En ciertos casos, en vez de bloquear al atacante, lo desvía a un entorno honeypot.
6. Si el atacante cae en ese entorno, sus siguientes acciones se siguen registrando como actividad honeypot.

## 12. Idea clave

La lógica no empieza preguntando “¿esto es SQL Injection o XSS?”.

Empieza así:

1. ¿La petición parece maliciosa?
2. Si sí, ¿a qué tipo pertenece?
3. ¿solo se registra o también se desvía al honeypot?

Ese es el corazón del funcionamiento del middleware.
