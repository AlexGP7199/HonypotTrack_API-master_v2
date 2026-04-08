# HoneypotTrack API - Documentaci�n de Endpoints

## Informaci�n General

- **Base URL:** `http://localhost:5273/api`
- **Swagger UI:** `http://localhost:5273/` (solo en desarrollo)
- **Swagger JSON:** `http://localhost:5273/swagger/v1/swagger.json`
- **Formato:** JSON
- **Autenticaci�n:** JWT Bearer Token

---

## Autenticaci�n

La API utiliza JWT (JSON Web Tokens) para autenticaci�n. Para acceder a endpoints protegidos:

1. Obt�n un token usando `/api/Auth/login` o `/api/Auth/register`
2. Incluye el token en el header `Authorization: Bearer {token}`
3. Cuando el token expire, usa `/api/Auth/refresh-token` para obtener uno nuevo

---

## Estructura de Respuestas

### BaseResponse&lt;T&gt;
Todas las respuestas de la API siguen esta estructura:

```json
{
  "isSuccess": true,
  "data": { ... },
  "message": "Operaci�n exitosa",
  "errors": null
}
```

| Campo | Tipo | Descripci�n |
|-------|------|-------------|
| `isSuccess` | boolean | Indica si la operaci�n fue exitosa |
| `data` | T | Datos de respuesta (puede ser objeto, array, o paginado) |
| `message` | string | Mensaje descriptivo |
| `errors` | string[] | Lista de errores (solo cuando `isSuccess = false`) |

### PagedResponse&lt;T&gt;
Para endpoints con paginaci�n:

```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 100,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## Par�metros de Paginaci�n (Query String)

| Par�metro | Tipo | Default | Descripci�n |
|-----------|------|---------|-------------|
| `PageNumber` | int | 1 | N�mero de p�gina |
| `PageSize` | int | 10 | Cantidad de registros por p�gina |
| `IsDescending` | bool | false | Ordenar descendente |

---

## 0. AUTENTICACI�N (`/api/Auth`)

### Modelos

#### LoginRequestDto (Request)
```json
{
  "email": "usuario@email.com",
  "password": "contrase�a123"
}
```

| Campo | Tipo | Requerido | Validaci�n |
|-------|------|-----------|------------|
| `email` | string | ? | Email v�lido |
| `password` | string | ? | - |

#### RegisterRequestDto (Request)
```json
{
  "fullName": "Juan P�rez",
  "email": "usuario@email.com",
  "password": "contrase�a123",
  "confirmPassword": "contrase�a123"
}
```

| Campo | Tipo | Requerido | Validaci�n |
|-------|------|-----------|------------|
| `fullName` | string | ? | Max 100 caracteres |
| `email` | string | ? | Email v�lido, max 100 caracteres |
| `password` | string | ? | Entre 6 y 100 caracteres |
| `confirmPassword` | string | ? | Debe coincidir con password |

#### LoginResponseDto (Response)
```json
{
  "userId": 1,
  "fullName": "Juan P�rez",
  "email": "usuario@email.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenExpiration": "2024-01-15T11:30:00Z",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

### Endpoints

#### POST /api/Auth/login ??
Inicia sesi�n y obtiene un token JWT.

**Request Body:** `LoginRequestDto`

**Response:** `BaseResponse<LoginResponseDto>`

```bash
curl -X POST "http://localhost:5273/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "usuario@email.com", "password": "contrase�a123"}'
```

---

#### POST /api/Auth/register ??
Registra un nuevo usuario y obtiene un token JWT.

**Request Body:** `RegisterRequestDto`

**Response:** `BaseResponse<LoginResponseDto>` (201 Created)

```bash
curl -X POST "http://localhost:5273/api/Auth/register" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "Nuevo Usuario", "email": "nuevo@email.com", "password": "pass123", "confirmPassword": "pass123"}'
```

---

#### POST /api/Auth/refresh-token ??
Renueva el token de acceso usando el refresh token.

**Request Body:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response:** `BaseResponse<LoginResponseDto>`

```bash
curl -X POST "http://localhost:5273/api/Auth/refresh-token" \
  -H "Content-Type: application/json" \
  -d '{"token": "...", "refreshToken": "..."}'
```

---

#### POST /api/Auth/logout ??
Cierra la sesi�n del usuario (invalida el refresh token).

**Requiere:** `Authorization: Bearer {token}`

**Response:** `BaseResponse<bool>`

```bash
curl -X POST "http://localhost:5273/api/Auth/logout" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

#### POST /api/Auth/change-password ??
Cambia la contrase�a del usuario autenticado.

**Requiere:** `Authorization: Bearer {token}`

**Request Body:**
```json
{
  "currentPassword": "contrase�aActual",
  "newPassword": "nuevaContrase�a",
  "confirmNewPassword": "nuevaContrase�a"
}
```

**Response:** `BaseResponse<bool>`

```bash
curl -X POST "http://localhost:5273/api/Auth/change-password" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"currentPassword": "oldPass", "newPassword": "newPass123", "confirmNewPassword": "newPass123"}'
```

---

#### GET /api/Auth/me ??
Obtiene la informaci�n del usuario autenticado.

**Requiere:** `Authorization: Bearer {token}`

**Response:**
```json
{
  "userId": "1",
  "email": "usuario@email.com",
  "fullName": "Juan P�rez"
}
```

```bash
curl -X GET "http://localhost:5273/api/Auth/me" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## 1. CATEGOR�AS (`/api/Categoria`)

### Modelos

#### CategoriaDto (Response)
```json
{
  "categoryId": 1,
  "name": "Alimentaci�n",
  "operationType": "Egreso"
}
```

#### CategoriaCreateDto (Request)
```json
{
  "name": "Alimentaci�n",
  "operationType": "Egreso"
}
```

| Campo | Tipo | Requerido | Validaci�n |
|-------|------|-----------|------------|
| `name` | string | ? | Max 50 caracteres |
| `operationType` | string | ? | "Ingreso" o "Egreso", max 10 caracteres |

#### CategoriaUpdateDto (Request)
```json
{
  "categoryId": 1,
  "name": "Alimentaci�n Actualizada",
  "operationType": "Egreso"
}
```

### Endpoints

#### GET /api/Categoria
Obtiene todas las categor�as con paginaci�n.

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `IsDescending` (bool, default: false)

**Response:** `BaseResponse<PagedResponse<CategoriaDto>>`

```bash
curl -X GET "http://localhost:5273/api/Categoria?PageNumber=1&PageSize=10"
```

---

#### GET /api/Categoria/all
Obtiene todas las categor�as sin paginaci�n.

**Response:** `BaseResponse<IEnumerable<CategoriaDto>>`

```bash
curl -X GET "http://localhost:5273/api/Categoria/all"
```

---

#### GET /api/Categoria/{id}
Obtiene una categor�a por ID.

**Path Parameters:**
- `id` (int) - ID de la categor�a

**Response:** `BaseResponse<CategoriaDto>`

```bash
curl -X GET "http://localhost:5273/api/Categoria/1"
```

---

#### GET /api/Categoria/tipo/{operationType}
Obtiene categor�as por tipo de operaci�n.

**Path Parameters:**
- `operationType` (string) - "Ingreso" o "Egreso"

**Response:** `BaseResponse<IEnumerable<CategoriaDto>>`

```bash
curl -X GET "http://localhost:5273/api/Categoria/tipo/Ingreso"
```

---

#### POST /api/Categoria
Crea una nueva categor�a.

**Request Body:** `CategoriaCreateDto`

**Response:** `BaseResponse<CategoriaDto>` (201 Created)

```bash
curl -X POST "http://localhost:5273/api/Categoria" \
  -H "Content-Type: application/json" \
  -d '{"name": "Nueva Categor�a", "operationType": "Ingreso"}'
```

---

#### PUT /api/Categoria
Actualiza una categor�a existente.

**Request Body:** `CategoriaUpdateDto`

**Response:** `BaseResponse<CategoriaDto>`

```bash
curl -X PUT "http://localhost:5273/api/Categoria" \
  -H "Content-Type: application/json" \
  -d '{"categoryId": 1, "name": "Categor�a Actualizada", "operationType": "Egreso"}'
```

---

#### DELETE /api/Categoria/{id}
Elimina una categor�a.

**Path Parameters:**
- `id` (int) - ID de la categor�a

**Response:** `BaseResponse<bool>`

```bash
curl -X DELETE "http://localhost:5273/api/Categoria/1"
```

---

## 2. USUARIOS (`/api/Usuario`)

### Modelos

#### UsuarioDto (Response)
```json
{
  "userId": 1,
  "fullName": "Juan P�rez",
  "email": "juan@email.com"
}
```

#### UsuarioCreateDto (Request)
```json
{
  "fullName": "Juan P�rez",
  "email": "juan@email.com",
  "password": "secreto123"
}
```

#### UsuarioUpdateDto (Request)
```json
{
  "userId": 1,
  "fullName": "Juan P�rez Actualizado",
  "email": "juan.nuevo@email.com"
}
```

### Endpoints

#### GET /api/Usuario
Obtiene todos los usuarios con paginaci�n.

**Response:** `BaseResponse<PagedResponse<UsuarioDto>>`

```bash
curl -X GET "http://localhost:5273/api/Usuario?PageNumber=1&PageSize=10"
```

---

#### GET /api/Usuario/all
Obtiene todos los usuarios sin paginaci�n.

**Response:** `BaseResponse<IEnumerable<UsuarioDto>>`

```bash
curl -X GET "http://localhost:5273/api/Usuario/all"
```

---

#### GET /api/Usuario/{id}
Obtiene un usuario por ID.

**Response:** `BaseResponse<UsuarioDto>`

```bash
curl -X GET "http://localhost:5273/api/Usuario/1"
```

---

#### GET /api/Usuario/exists-email/{email}
Verifica si existe un usuario con el email especificado.

**Response:** `BaseResponse<bool>`

```bash
curl -X GET "http://localhost:5273/api/Usuario/exists-email/juan@email.com"
```

---

#### POST /api/Usuario
Crea un nuevo usuario.

**Request Body:** `UsuarioCreateDto`

**Response:** `BaseResponse<UsuarioDto>` (201 Created)

```bash
curl -X POST "http://localhost:5273/api/Usuario" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "Nuevo Usuario", "email": "nuevo@email.com", "password": "pass123"}'
```

---

#### PUT /api/Usuario
Actualiza un usuario existente.

**Request Body:** `UsuarioUpdateDto`

**Response:** `BaseResponse<UsuarioDto>`

```bash
curl -X PUT "http://localhost:5273/api/Usuario" \
  -H "Content-Type: application/json" \
  -d '{"userId": 1, "fullName": "Usuario Actualizado", "email": "actualizado@email.com"}'
```

---

#### DELETE /api/Usuario/{id}
Elimina un usuario.

**Response:** `BaseResponse<bool>`

```bash
curl -X DELETE "http://localhost:5273/api/Usuario/1"
```

---

## 3. CUENTAS (`/api/Cuenta`)

### Modelos

#### CuentaDto (Response)
```json
{
  "accountId": 1,
  "userId": 1,
  "accountName": "Cuenta Principal",
  "currency": "USD",
  "usuarioNombre": "Juan P�rez"
}
```

#### CuentaCreateDto (Request)
```json
{
  "userId": 1,
  "accountName": "Cuenta Principal",
  "currency": "USD"
}
```

#### CuentaUpdateDto (Request)
```json
{
  "accountId": 1,
  "userId": 1,
  "accountName": "Cuenta Actualizada",
  "currency": "EUR"
}
```

### Endpoints

#### GET /api/Cuenta
Obtiene todas las cuentas con paginaci�n.

**Response:** `BaseResponse<PagedResponse<CuentaDto>>`

```bash
curl -X GET "http://localhost:5273/api/Cuenta?PageNumber=1&PageSize=10"
```

---

#### GET /api/Cuenta/{id}
Obtiene una cuenta por ID.

**Response:** `BaseResponse<CuentaDto>`

```bash
curl -X GET "http://localhost:5273/api/Cuenta/1"
```

---

#### GET /api/Cuenta/usuario/{userId}
Obtiene todas las cuentas de un usuario.

**Response:** `BaseResponse<IEnumerable<CuentaDto>>`

```bash
curl -X GET "http://localhost:5273/api/Cuenta/usuario/1"
```

---

#### POST /api/Cuenta
Crea una nueva cuenta.

**Request Body:** `CuentaCreateDto`

**Response:** `BaseResponse<CuentaDto>` (201 Created)

```bash
curl -X POST "http://localhost:5273/api/Cuenta" \
  -H "Content-Type: application/json" \
  -d '{"userId": 1, "accountName": "Nueva Cuenta", "currency": "USD"}'
```

---

#### PUT /api/Cuenta
Actualiza una cuenta existente.

**Request Body:** `CuentaUpdateDto`

**Response:** `BaseResponse<CuentaDto>`

```bash
curl -X PUT "http://localhost:5273/api/Cuenta" \
  -H "Content-Type: application/json" \
  -d '{"accountId": 1, "userId": 1, "accountName": "Cuenta Actualizada", "currency": "EUR"}'
```

---

#### DELETE /api/Cuenta/{id}
Elimina una cuenta.

**Response:** `BaseResponse<bool>`

```bash
curl -X DELETE "http://localhost:5273/api/Cuenta/1"
```

---

## 4. TRANSACCIONES (`/api/Transaccion`)

### Modelos

#### TransaccionDto (Response)
```json
{
  "transaccionId": 1,
  "accountId": 1,
  "categoryId": 1,
  "contactsId": null,
  "monto": 100.50,
  "moneda": "USD",
  "descripcion": "Compra de supermercado",
  "fecha": "2024-01-15T10:30:00",
  "cuentaNombre": "Cuenta Principal",
  "categoriaNombre": "Alimentaci�n",
  "contactoNombre": null,
  "tipoOperacion": "Egreso"
}
```

#### TransaccionCreateDto (Request)
```json
{
  "accountId": 1,
  "categoryId": 1,
  "contactsId": null,
  "monto": 100.50,
  "moneda": "USD",
  "descripcion": "Compra de supermercado",
  "fecha": "2024-01-15T10:30:00"
}
```

#### TransaccionUpdateDto (Request)
```json
{
  "transaccionId": 1,
  "accountId": 1,
  "categoryId": 1,
  "contactsId": null,
  "monto": 150.00,
  "moneda": "USD",
  "descripcion": "Compra actualizada",
  "fecha": "2024-01-15T10:30:00"
}
```

### Endpoints

#### GET /api/Transaccion
Obtiene todas las transacciones con paginaci�n.

**Response:** `BaseResponse<PagedResponse<TransaccionDto>>`

```bash
curl -X GET "http://localhost:5273/api/Transaccion?PageNumber=1&PageSize=10"
```

---

#### GET /api/Transaccion/{id}
Obtiene una transacci�n por ID.

**Response:** `BaseResponse<TransaccionDto>`

```bash
curl -X GET "http://localhost:5273/api/Transaccion/1"
```

---

#### GET /api/Transaccion/cuenta/{accountId}
Obtiene todas las transacciones de una cuenta.

**Response:** `BaseResponse<IEnumerable<TransaccionDto>>`

```bash
curl -X GET "http://localhost:5273/api/Transaccion/cuenta/1"
```

---

#### GET /api/Transaccion/categoria/{categoryId}
Obtiene todas las transacciones de una categor�a.

**Response:** `BaseResponse<IEnumerable<TransaccionDto>>`

```bash
curl -X GET "http://localhost:5273/api/Transaccion/categoria/1"
```

---

#### GET /api/Transaccion/cuenta/{accountId}/total/{operationType}
Obtiene el total de transacciones por tipo de operaci�n.

**Path Parameters:**
- `accountId` (int) - ID de la cuenta
- `operationType` (string) - "Ingreso" o "Egreso"

**Response:** `BaseResponse<decimal>`

```bash
curl -X GET "http://localhost:5273/api/Transaccion/cuenta/1/total/Ingreso"
```

---

#### GET /api/Transaccion/cuenta/{accountId}/balance
Obtiene el balance de una cuenta (Ingresos - Egresos).

**Response:** `BaseResponse<decimal>`

```bash
curl -X GET "http://localhost:5273/api/Transaccion/cuenta/1/balance"
```

---

#### POST /api/Transaccion
Crea una nueva transacci�n.

**Request Body:** `TransaccionCreateDto`

**Response:** `BaseResponse<TransaccionDto>` (201 Created)

```bash
curl -X POST "http://localhost:5273/api/Transaccion" \
  -H "Content-Type: application/json" \
  -d '{"accountId": 1, "categoryId": 1, "monto": 100.50, "moneda": "USD", "descripcion": "Nueva transacci�n"}'
```

---

#### PUT /api/Transaccion
Actualiza una transacci�n existente.

**Request Body:** `TransaccionUpdateDto`

**Response:** `BaseResponse<TransaccionDto>`

```bash
curl -X PUT "http://localhost:5273/api/Transaccion" \
  -H "Content-Type: application/json" \
  -d '{"transaccionId": 1, "accountId": 1, "categoryId": 1, "monto": 150.00, "moneda": "USD"}'
```

---

#### DELETE /api/Transaccion/{id}
Elimina una transacci�n.

**Response:** `BaseResponse<bool>`

```bash
curl -X DELETE "http://localhost:5273/api/Transaccion/1"
```

---

## 5. CONTACTOS (`/api/Contact`)

### Modelos

#### ContactDto (Response)
```json
{
  "contactsId": 1,
  "userId": 1,
  "name": "Empresa ABC",
  "type": "Proveedor",
  "email": "contacto@empresa.com",
  "phone": "+1234567890",
  "usuarioNombre": "Juan P�rez"
}
```

#### ContactCreateDto (Request)
```json
{
  "userId": 1,
  "name": "Empresa ABC",
  "type": "Proveedor",
  "email": "contacto@empresa.com",
  "phone": "+1234567890"
}
```

#### ContactUpdateDto (Request)
```json
{
  "contactsId": 1,
  "userId": 1,
  "name": "Empresa ABC Actualizada",
  "type": "Cliente",
  "email": "nuevo@empresa.com",
  "phone": "+0987654321"
}
```

### Endpoints

#### GET /api/Contact
Obtiene todos los contactos con paginaci�n.

**Response:** `BaseResponse<PagedResponse<ContactDto>>`

```bash
curl -X GET "http://localhost:5273/api/Contact?PageNumber=1&PageSize=10"
```

---

#### GET /api/Contact/{id}
Obtiene un contacto por ID.

**Response:** `BaseResponse<ContactDto>`

```bash
curl -X GET "http://localhost:5273/api/Contact/1"
```

---

#### GET /api/Contact/usuario/{userId}
Obtiene todos los contactos de un usuario.

**Response:** `BaseResponse<IEnumerable<ContactDto>>`

```bash
curl -X GET "http://localhost:5273/api/Contact/usuario/1"
```

---

#### GET /api/Contact/tipo/{type}
Obtiene contactos por tipo.

**Response:** `BaseResponse<IEnumerable<ContactDto>>`

```bash
curl -X GET "http://localhost:5273/api/Contact/tipo/Proveedor"
```

---

#### POST /api/Contact
Crea un nuevo contacto.

**Request Body:** `ContactCreateDto`

**Response:** `BaseResponse<ContactDto>` (201 Created)

```bash
curl -X POST "http://localhost:5273/api/Contact" \
  -H "Content-Type: application/json" \
  -d '{"userId": 1, "name": "Nuevo Contacto", "type": "Cliente", "email": "contacto@email.com"}'
```

---

#### PUT /api/Contact
Actualiza un contacto existente.

**Request Body:** `ContactUpdateDto`

**Response:** `BaseResponse<ContactDto>`

```bash
curl -X PUT "http://localhost:5273/api/Contact" \
  -H "Content-Type: application/json" \
  -d '{"contactsId": 1, "userId": 1, "name": "Contacto Actualizado", "type": "Proveedor"}'
```

---

#### DELETE /api/Contact/{id}
Elimina un contacto.

**Response:** `BaseResponse<bool>`

```bash
curl -X DELETE "http://localhost:5273/api/Contact/1"
```

---

## C�digos de Estado HTTP

| C�digo | Descripci�n |
|--------|-------------|
| 200 | OK - Operaci�n exitosa |
| 201 | Created - Recurso creado exitosamente |
| 400 | Bad Request - Error en la solicitud |
| 404 | Not Found - Recurso no encontrado |
| 500 | Internal Server Error - Error del servidor |

---

## Headers de Respuesta

| Header | Descripci�n |
|--------|-------------|
| `X-Correlation-Id` | ID �nico para rastrear la solicitud |
| `Content-Type` | `application/json; charset=utf-8` |

---

## Generaci�n de Clientes (Frontend)

### Obtener el archivo swagger.json

```bash
curl -o swagger.json "http://localhost:5273/swagger/v1/swagger.json"
```

### Usando NSwag (Angular/React/Vue)

```bash
# Instalar NSwag
npm install -g nswag

# Generar cliente TypeScript
nswag openapi2tsclient /input:swagger.json /output:api-client.ts
```

### Usando OpenAPI Generator

```bash
# Generar cliente TypeScript-Axios
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:5273/swagger/v1/swagger.json \
  -g typescript-axios \
  -o ./src/api
```

---

## Ejemplos de Uso con JavaScript/TypeScript

### Fetch API

```typescript
// Obtener categor�as
const response = await fetch('http://localhost:5273/api/Categoria?PageNumber=1&PageSize=10');
const data = await response.json();

if (data.isSuccess) {
  console.log(data.data.items); // Array de categor�as
  console.log(data.data.totalRecords); // Total de registros
}
```

### Axios

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5273/api',
  headers: { 'Content-Type': 'application/json' }
});

// Crear categor�a
const response = await api.post('/Categoria', {
  name: 'Nueva Categor�a',
  operationType: 'Ingreso'
});

if (response.data.isSuccess) {
  console.log('Categor�a creada:', response.data.data);
}
```

---

## Contacto

- **Repositorio:** https://github.com/AlexGP7199/HonypotTrack_API
- **Swagger UI:** http://localhost:5273/

---

*Documentaci�n generada el: $(date)*
*Versi�n API: v1*
