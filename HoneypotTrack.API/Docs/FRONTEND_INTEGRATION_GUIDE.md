# Guía de Integración Frontend - HoneypotTrack API

Esta guía explica cómo consumir la API de HoneypotTrack desde aplicaciones frontend (React, Angular, Vue, etc.).

---

## ?? Tabla de Contenidos

1. [Configuración Inicial](#1-configuración-inicial)
2. [Autenticación](#2-autenticación)
3. [Configuración del Cliente HTTP](#3-configuración-del-cliente-http)
4. [Consumo de Endpoints](#4-consumo-de-endpoints)
5. [Manejo de Errores](#5-manejo-de-errores)
6. [Interfaces TypeScript](#6-interfaces-typescript)
7. [Ejemplos por Framework](#7-ejemplos-por-framework)
8. [Generación Automática de Clientes](#8-generación-automática-de-clientes)

---

## 1. Configuración Inicial

### URL Base de la API

```
Desarrollo: http://localhost:5273/api
Producción: https://tu-dominio.com/api
```

### Swagger/OpenAPI

- **Swagger UI:** http://localhost:5273/
- **swagger.json:** http://localhost:5273/swagger/v1/swagger.json

### Variables de Entorno (recomendado)

Crea un archivo `.env` en tu proyecto frontend:

```env
# .env.development
VITE_API_URL=http://localhost:5273/api
# o para React
REACT_APP_API_URL=http://localhost:5273/api
# o para Angular (environment.ts)
API_URL=http://localhost:5273/api
```

---

## 2. Autenticación

La API utiliza **JWT (JSON Web Tokens)**. El flujo de autenticación es:

```
???????????????     POST /api/Auth/login      ???????????????
?   Frontend  ? ???????????????????????????????    API      ?
?             ??????????????????????????????? ?             ?
???????????????   { token, refreshToken }     ???????????????
       ?
       ? Guardar token en localStorage/sessionStorage
       ?
???????????????  GET /api/Categoria           ???????????????
?   Frontend  ?  Authorization: Bearer {token}?    API      ?
?             ? ???????????????????????????????             ?
???????????????                               ???????????????
```

### 2.1 Login

```typescript
// Función de login
async function login(email: string, password: string) {
  const response = await fetch('http://localhost:5273/api/Auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email, password }),
  });

  const data = await response.json();

  if (data.isSuccess) {
    // Guardar tokens
    localStorage.setItem('token', data.data.token);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    localStorage.setItem('user', JSON.stringify({
      userId: data.data.userId,
      fullName: data.data.fullName,
      email: data.data.email
    }));
    return data.data;
  } else {
    throw new Error(data.message);
  }
}
```

### 2.2 Registro

```typescript
async function register(fullName: string, email: string, password: string) {
  const response = await fetch('http://localhost:5273/api/Auth/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ 
      fullName, 
      email, 
      password, 
      confirmPassword: password 
    }),
  });

  const data = await response.json();

  if (data.isSuccess) {
    localStorage.setItem('token', data.data.token);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    return data.data;
  } else {
    throw new Error(data.message);
  }
}
```

### 2.3 Refresh Token (cuando el token expira)

```typescript
async function refreshToken() {
  const token = localStorage.getItem('token');
  const refreshToken = localStorage.getItem('refreshToken');

  const response = await fetch('http://localhost:5273/api/Auth/refresh-token', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ token, refreshToken }),
  });

  const data = await response.json();

  if (data.isSuccess) {
    localStorage.setItem('token', data.data.token);
    localStorage.setItem('refreshToken', data.data.refreshToken);
    return data.data.token;
  } else {
    // Token inválido, redirigir a login
    logout();
    throw new Error('Sesión expirada');
  }
}
```

### 2.4 Logout

```typescript
function logout() {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  // Redirigir a login
  window.location.href = '/login';
}
```

---

## 3. Configuración del Cliente HTTP

### 3.1 Con Axios (Recomendado)

```bash
npm install axios
```

```typescript
// src/services/api.ts
import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5273/api';

// Crear instancia de Axios
const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor para agregar token a cada petición
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor para manejar errores y refresh token
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Si el error es 401 y no es un retry
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const token = localStorage.getItem('token');
        const refreshToken = localStorage.getItem('refreshToken');

        const response = await axios.post(`${API_URL}/Auth/refresh-token`, {
          token,
          refreshToken,
        });

        if (response.data.isSuccess) {
          const newToken = response.data.data.token;
          localStorage.setItem('token', newToken);
          localStorage.setItem('refreshToken', response.data.data.refreshToken);

          // Reintentar la petición original con el nuevo token
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return api(originalRequest);
        }
      } catch (refreshError) {
        // Refresh falló, cerrar sesión
        localStorage.clear();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;
```

### 3.2 Con Fetch (Nativo)

```typescript
// src/services/api.ts
const API_URL = 'http://localhost:5273/api';

async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = localStorage.getItem('token');

  const config: RequestInit = {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    },
  };

  const response = await fetch(`${API_URL}${endpoint}`, config);

  // Si es 401, intentar refresh
  if (response.status === 401) {
    const newToken = await refreshToken();
    if (newToken) {
      config.headers = {
        ...config.headers,
        Authorization: `Bearer ${newToken}`,
      };
      const retryResponse = await fetch(`${API_URL}${endpoint}`, config);
      return retryResponse.json();
    }
  }

  return response.json();
}

export { apiRequest };
```

---

## 4. Consumo de Endpoints

### 4.1 Servicios por Entidad

Crea un servicio para cada entidad:

```typescript
// src/services/categoriaService.ts
import api from './api';
import { BaseResponse, PagedResponse, CategoriaDto, CategoriaCreateDto } from '../types';

export const categoriaService = {
  // Obtener todas con paginación
  getAll: async (pageNumber = 1, pageSize = 10, isDescending = false) => {
    const response = await api.get<BaseResponse<PagedResponse<CategoriaDto>>>(
      `/Categoria?PageNumber=${pageNumber}&PageSize=${pageSize}&IsDescending=${isDescending}`
    );
    return response.data;
  },

  // Obtener todas sin paginación
  getAllWithoutPagination: async () => {
    const response = await api.get<BaseResponse<CategoriaDto[]>>('/Categoria/all');
    return response.data;
  },

  // Obtener por ID
  getById: async (id: number) => {
    const response = await api.get<BaseResponse<CategoriaDto>>(`/Categoria/${id}`);
    return response.data;
  },

  // Obtener por tipo de operación
  getByOperationType: async (operationType: 'Ingreso' | 'Egreso') => {
    const response = await api.get<BaseResponse<CategoriaDto[]>>(
      `/Categoria/tipo/${operationType}`
    );
    return response.data;
  },

  // Crear
  create: async (categoria: CategoriaCreateDto) => {
    const response = await api.post<BaseResponse<CategoriaDto>>('/Categoria', categoria);
    return response.data;
  },

  // Actualizar
  update: async (categoria: CategoriaUpdateDto) => {
    const response = await api.put<BaseResponse<CategoriaDto>>('/Categoria', categoria);
    return response.data;
  },

  // Eliminar
  delete: async (id: number) => {
    const response = await api.delete<BaseResponse<boolean>>(`/Categoria/${id}`);
    return response.data;
  },
};
```

### 4.2 Ejemplo de Uso en Componentes

```typescript
// React ejemplo
import { useState, useEffect } from 'react';
import { categoriaService } from '../services/categoriaService';
import { CategoriaDto } from '../types';

function CategoriasList() {
  const [categorias, setCategorias] = useState<CategoriaDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  useEffect(() => {
    loadCategorias();
  }, [page]);

  const loadCategorias = async () => {
    try {
      setLoading(true);
      const response = await categoriaService.getAll(page, 10);
      
      if (response.isSuccess) {
        setCategorias(response.data.items);
        setTotalPages(response.data.totalPages);
      } else {
        setError(response.message || 'Error al cargar categorías');
      }
    } catch (err) {
      setError('Error de conexión');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (window.confirm('¿Estás seguro?')) {
      const response = await categoriaService.delete(id);
      if (response.isSuccess) {
        loadCategorias(); // Recargar lista
      }
    }
  };

  if (loading) return <div>Cargando...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <h1>Categorías</h1>
      <ul>
        {categorias.map((cat) => (
          <li key={cat.categoryId}>
            {cat.name} - {cat.operationType}
            <button onClick={() => handleDelete(cat.categoryId)}>Eliminar</button>
          </li>
        ))}
      </ul>
      
      {/* Paginación */}
      <div>
        <button disabled={page === 1} onClick={() => setPage(p => p - 1)}>
          Anterior
        </button>
        <span>Página {page} de {totalPages}</span>
        <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>
          Siguiente
        </button>
      </div>
    </div>
  );
}
```

---

## 5. Manejo de Errores

### 5.1 Estructura de Error de la API

```typescript
// Cuando isSuccess es false
{
  "isSuccess": false,
  "data": null,
  "message": "Mensaje de error",
  "errors": ["Error 1", "Error 2"]  // Opcional, lista de errores de validación
}
```

### 5.2 Función de Manejo de Errores

```typescript
// src/utils/errorHandler.ts
import { BaseResponse } from '../types';

export function handleApiError<T>(response: BaseResponse<T>): void {
  if (!response.isSuccess) {
    // Si hay errores de validación
    if (response.errors && response.errors.length > 0) {
      throw new Error(response.errors.join(', '));
    }
    throw new Error(response.message || 'Error desconocido');
  }
}

// Uso
try {
  const response = await categoriaService.create({ name: '', operationType: '' });
  handleApiError(response);
  // Continuar con éxito...
} catch (error) {
  console.error(error.message);
  // Mostrar mensaje al usuario
}
```

### 5.3 Códigos de Estado HTTP

| Código | Significado | Acción Recomendada |
|--------|-------------|-------------------|
| 200 | Éxito | Procesar datos |
| 201 | Creado | Mostrar mensaje de éxito, redirigir |
| 400 | Error de validación | Mostrar `errors` al usuario |
| 401 | No autorizado | Intentar refresh token o redirigir a login |
| 404 | No encontrado | Mostrar mensaje "No encontrado" |
| 500 | Error del servidor | Mostrar mensaje genérico, reportar error |

---

## 6. Interfaces TypeScript

Crea un archivo de tipos basado en los modelos de la API:

```typescript
// src/types/index.ts

// ============ RESPUESTAS BASE ============

export interface BaseResponse<T> {
  isSuccess: boolean;
  data: T | null;
  message: string | null;
  errors: string[] | null;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ============ AUTENTICACIÓN ============

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName?: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface LoginResponse {
  userId: number;
  fullName: string;
  email: string;
  token: string;
  tokenExpiration: string;
  refreshToken: string;
}

export interface RefreshTokenRequest {
  token: string;
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// ============ USUARIO ============

export interface UsuarioDto {
  userId: number;
  fullName: string | null;
  email: string;
}

export interface UsuarioCreateDto {
  fullName?: string;
  email: string;
}

export interface UsuarioUpdateDto {
  userId: number;
  fullName?: string;
  email: string;
}

// ============ CATEGORÍA ============

export interface CategoriaDto {
  categoryId: number;
  name: string;
  operationType: 'Ingreso' | 'Egreso';
}

export interface CategoriaCreateDto {
  name: string;
  operationType: 'Ingreso' | 'Egreso';
}

export interface CategoriaUpdateDto {
  categoryId: number;
  name: string;
  operationType: 'Ingreso' | 'Egreso';
}

// ============ CUENTA ============

export interface CuentaDto {
  accountId: number;
  userId: number;
  accountName: string;
  currency: string;
  usuarioNombre: string | null;
}

export interface CuentaCreateDto {
  userId: number;
  accountName: string;
  currency: string;
}

export interface CuentaUpdateDto {
  accountId: number;
  userId: number;
  accountName: string;
  currency: string;
}

// ============ TRANSACCIÓN ============

export interface TransaccionDto {
  transaccionId: number;
  accountId: number;
  categoryId: number;
  contactsId: number | null;
  monto: number;
  moneda: string;
  descripcion: string | null;
  fecha: string | null;
  cuentaNombre: string | null;
  categoriaNombre: string | null;
  contactoNombre: string | null;
  tipoOperacion: string | null;
}

export interface TransaccionCreateDto {
  accountId: number;
  categoryId: number;
  contactsId?: number | null;
  monto: number;
  moneda: string;
  descripcion?: string;
  fecha?: string;
}

export interface TransaccionUpdateDto {
  transaccionId: number;
  accountId: number;
  categoryId: number;
  contactsId?: number | null;
  monto: number;
  moneda: string;
  descripcion?: string;
  fecha?: string;
}

// ============ CONTACTO ============

export interface ContactDto {
  contactsId: number;
  userId: number;
  name: string;
  type: string;
  email: string | null;
  phone: string | null;
  usuarioNombre: string | null;
}

export interface ContactCreateDto {
  userId: number;
  name: string;
  type: string;
  email?: string;
  phone?: string;
}

export interface ContactUpdateDto {
  contactsId: number;
  userId: number;
  name: string;
  type: string;
  email?: string;
  phone?: string;
}

// ============ FILTROS ============

export interface BaseFilters {
  pageNumber?: number;
  pageSize?: number;
  isDescending?: boolean;
}

export interface CategoriaFilters extends BaseFilters {}
export interface UsuarioFilters extends BaseFilters {}
export interface CuentaFilters extends BaseFilters {}
export interface TransaccionFilters extends BaseFilters {}
export interface ContactFilters extends BaseFilters {}
```

---

## 7. Ejemplos por Framework

### 7.1 React con Hooks

```typescript
// src/hooks/useCategorias.ts
import { useState, useEffect, useCallback } from 'react';
import { categoriaService } from '../services/categoriaService';
import { CategoriaDto, PagedResponse } from '../types';

export function useCategorias(initialPage = 1, pageSize = 10) {
  const [data, setData] = useState<PagedResponse<CategoriaDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(initialPage);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await categoriaService.getAll(page, pageSize);
      if (response.isSuccess && response.data) {
        setData(response.data);
      } else {
        setError(response.message || 'Error al cargar datos');
      }
    } catch (err) {
      setError('Error de conexión');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return {
    categorias: data?.items || [],
    totalPages: data?.totalPages || 1,
    currentPage: page,
    loading,
    error,
    setPage,
    refresh: fetchData,
  };
}
```

### 7.2 Angular Service

```typescript
// src/app/services/categoria.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CategoriaService {
  private apiUrl = `${environment.apiUrl}/Categoria`;

  constructor(private http: HttpClient) {}

  getAll(pageNumber = 1, pageSize = 10, isDescending = false): Observable<BaseResponse<PagedResponse<CategoriaDto>>> {
    const params = new HttpParams()
      .set('PageNumber', pageNumber.toString())
      .set('PageSize', pageSize.toString())
      .set('IsDescending', isDescending.toString());

    return this.http.get<BaseResponse<PagedResponse<CategoriaDto>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<BaseResponse<CategoriaDto>> {
    return this.http.get<BaseResponse<CategoriaDto>>(`${this.apiUrl}/${id}`);
  }

  create(categoria: CategoriaCreateDto): Observable<BaseResponse<CategoriaDto>> {
    return this.http.post<BaseResponse<CategoriaDto>>(this.apiUrl, categoria);
  }

  update(categoria: CategoriaUpdateDto): Observable<BaseResponse<CategoriaDto>> {
    return this.http.put<BaseResponse<CategoriaDto>>(this.apiUrl, categoria);
  }

  delete(id: number): Observable<BaseResponse<boolean>> {
    return this.http.delete<BaseResponse<boolean>>(`${this.apiUrl}/${id}`);
  }
}
```

### 7.3 Vue 3 Composable

```typescript
// src/composables/useCategorias.ts
import { ref, computed, watch } from 'vue';
import { categoriaService } from '../services/categoriaService';
import type { CategoriaDto } from '../types';

export function useCategorias() {
  const categorias = ref<CategoriaDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const page = ref(1);
  const totalPages = ref(1);
  const pageSize = ref(10);

  const fetchCategorias = async () => {
    loading.value = true;
    error.value = null;
    
    try {
      const response = await categoriaService.getAll(page.value, pageSize.value);
      if (response.isSuccess && response.data) {
        categorias.value = response.data.items;
        totalPages.value = response.data.totalPages;
      } else {
        error.value = response.message || 'Error';
      }
    } catch (e) {
      error.value = 'Error de conexión';
    } finally {
      loading.value = false;
    }
  };

  // Cargar al cambiar página
  watch(page, fetchCategorias, { immediate: true });

  return {
    categorias,
    loading,
    error,
    page,
    totalPages,
    fetchCategorias,
    nextPage: () => { if (page.value < totalPages.value) page.value++ },
    prevPage: () => { if (page.value > 1) page.value-- },
  };
}
```

---

## 8. Generación Automática de Clientes

### 8.1 Usando NSwag

```bash
# Instalar NSwag globalmente
npm install -g nswag

# Generar cliente TypeScript
nswag openapi2tsclient \
  /input:http://localhost:5273/swagger/v1/swagger.json \
  /output:src/api/apiClient.ts \
  /template:Axios
```

### 8.2 Usando OpenAPI Generator

```bash
# Generar cliente TypeScript con Axios
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:5273/swagger/v1/swagger.json \
  -g typescript-axios \
  -o src/api/generated

# Generar cliente para Angular
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:5273/swagger/v1/swagger.json \
  -g typescript-angular \
  -o src/api/generated
```

### 8.3 Descargar swagger.json manualmente

```bash
# Descargar el archivo swagger.json
curl -o swagger.json http://localhost:5273/swagger/v1/swagger.json
```

---

## ?? Checklist de Integración

- [ ] Configurar URL base de la API en variables de entorno
- [ ] Implementar servicio de autenticación (login, register, logout)
- [ ] Configurar interceptor HTTP para agregar token automáticamente
- [ ] Implementar refresh token automático
- [ ] Crear interfaces TypeScript para todos los modelos
- [ ] Crear servicios para cada entidad (Categoria, Usuario, Cuenta, etc.)
- [ ] Implementar manejo de errores global
- [ ] Implementar componentes de paginación
- [ ] Proteger rutas que requieren autenticación
- [ ] Testear todos los endpoints

---

## ?? Recursos Adicionales

- **Swagger UI:** http://localhost:5273/
- **Documentación de API:** `API_DOCUMENTATION.md`
- **Repositorio:** https://github.com/AlexGP7199/HonypotTrack_API

---

## ?? Soporte

Si tienes problemas de CORS, verifica que el backend tenga configurada la política correcta. La API ya permite todas las conexiones en desarrollo con:

```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
```

---

*Última actualización: Marzo 2025*
