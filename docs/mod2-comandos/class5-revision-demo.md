# Clase 5: Revision y demo del flujo completo
> Duracion estimada: ~12 min

---

## Capitulo 1: Endpoints funcionando end-to-end

### Flujo completo


### Opcion A: Interfaz Grafica


```bash
 http://localhost:5001/scalar/v1
```

### Opcion B: Comandos

Suando Postman,Insomnia para hacer las pruebas, o usar `curl` desde la terminal.

#### 1. Listar salas (datos seed)

```bash
curl -s http://localhost:5001/api/rooms
```

Respuesta esperada:
```json
[
  {
    "id": 1,
    "name": "General",
    "description": "Sala de chat general",
    "createdAt": "2025-01-01T00:00:00Z"
  },
  {
    "id": 2,
    "name": "Tecnología",
    "description": "Discusiones sobre tecnología",
    "createdAt": "2025-01-01T00:00:00Z"
  }
]
```

#### 2. Crear un usuario

```bash
curl -s -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'
```

Respuesta esperada:
```json
{
  "id": 1,
  "username": "carlos",
  "createdAt": "2026-02-04T..."
}
```

#### 3. Crear un segundo usuario

```bash
curl -s -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"maria"}'
```

Respuesta esperada:
```json
{
  "id": 2,
  "username": "maria",
  "createdAt": "2026-02-04T..."
}
```

#### 4. Intentar crear usuario duplicado

```bash
curl -s -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"carlos"}'
```

Respuesta esperada: retorna el usuario existente (HTTP 200, no 201). Notar que `id` sigue siendo 1:
```json
{
  "id": 1,
  "username": "carlos",
  "createdAt": "2026-02-04T..."
}
```

#### 5. Listar usuarios

```bash
curl -s http://localhost:5001/api/users
```

Respuesta esperada:
```json
[
  { "id": 1, "username": "carlos", "createdAt": "2026-02-04T..." },
  { "id": 2, "username": "maria", "createdAt": "2026-02-04T..." }
]
```

#### 6. Crear una sala nueva

```bash
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes","description":"Sala de deportes"}'
```

Respuesta esperada:
```json
{
  "id": 3,
  "name": "Deportes",
  "description": "Sala de deportes",
  "createdAt": "2026-02-04T..."
}
```

#### 7. Actualizar una sala

```bash
curl -s -X PUT http://localhost:5001/api/rooms/3 \
  -H "Content-Type: application/json" \
  -d '{"name":"Deportes y Fitness","description":"Deportes, fitness y vida saludable"}'
```

Respuesta esperada:
```json
{
  "id": 3,
  "name": "Deportes y Fitness",
  "description": "Deportes, fitness y vida saludable",
  "createdAt": "2026-02-04T..."
}
```

#### 8. Enviar mensajes a una sala

```bash
curl -s -X POST http://localhost:5001/api/rooms/1/messages \
  -H "Content-Type: application/json" \
  -d '{"content":"Hola a todos!","userId":1,"username":"carlos"}'

curl -s -X POST http://localhost:5001/api/rooms/1/messages \
  -H "Content-Type: application/json" \
  -d '{"content":"Hola Carlos!","userId":2,"username":"maria"}'
```

#### 9. Obtener mensajes de la sala

```bash
curl -s http://localhost:5001/api/rooms/1/messages
```

Respuesta esperada:
```json
[
  {
    "id": 1,
    "content": "Hola a todos!",
    "sentAt": "2026-02-04T...",
    "userId": 1,
    "username": "carlos",
    "roomId": 1
  },
  {
    "id": 2,
    "content": "Hola Carlos!",
    "sentAt": "2026-02-04T...",
    "userId": 2,
    "username": "maria",
    "roomId": 1
  }
]
```

#### 10. Eliminar una sala

```bash
curl -s -X DELETE http://localhost:5001/api/rooms/3 -w "\nHTTP Status: %{http_code}\n"
```

Respuesta esperada: HTTP 204 (sin body).

#### 11. Verificar documento OpenAPI

```bash
curl -s http://localhost:5001/openapi/v1.json | python3 -m json.tool | head -30
```

```bash
# Detener el servidor
kill %1 2>/dev/null
```

---

## Capitulo 2: Validacion aplicada en todos los endpoints

### Comandos

```bash
cd src/EDChat.Api
dotnet run &
sleep 2
```

#### Test 1: Nombre vacio

```bash
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"","description":"test"}'
```

Respuesta esperada (HTTP 400):
```json
{
  "title": "One or more validation errors occurred.",
  "errors": {
    "Name": [
      "El nombre de la sala es requerido"
    ]
  }
}
```

#### Test 2: Nombre demasiado largo (mas de 100 caracteres)

```bash
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Este es un nombre extremadamente largo que supera el limite de cien caracteres establecido en el validador de CreateRoomDto","description":"test"}'
```

Respuesta esperada (HTTP 400):
```json
{
  "title": "One or more validation errors occurred.",
  "errors": {
    "Name": [
      "El nombre no puede exceder 100 caracteres"
    ]
  }
}
```

#### Test 3: Sala con nombre duplicado

```bash
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"General","description":"Ya existe"}'
```

Respuesta esperada (HTTP 409 Conflict) - este error viene del **endpoint filter**, no de Data Annotations:
```json
{
  "error": "Ya existe una sala con el nombre 'General'"
}
```

#### Test 4: Request valido (para confirmar que pasa)

```bash
curl -s -X POST http://localhost:5001/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"Sala Valida","description":"Esta sala si es valida"}'
```

Respuesta esperada (HTTP 201): La sala se crea correctamente.

#### Test 5: Sala no encontrada

```bash
curl -s -X PUT http://localhost:5001/api/rooms/999 \
  -H "Content-Type: application/json" \
  -d '{"name":"No existe","description":"test"}' -w "\nHTTP Status: %{http_code}\n"
```

Respuesta esperada: HTTP 404.

```bash
# Detener el servidor
kill %1 2>/dev/null
```

---

## Resumen de endpoints

| Metodo | Ruta | Descripcion | Status codes |
|--------|------|-------------|--------------|
| GET | `/api/users` | Listar usuarios | 200 |
| POST | `/api/users` | Crear usuario (o retornar existente) | 201, 200, 400 |
| GET | `/api/rooms` | Listar salas | 200 |
| POST | `/api/rooms` | Crear sala (con validacion) | 201, 400, 409 |
| PUT | `/api/rooms/{id}` | Actualizar sala | 200, 400, 404 |
| DELETE | `/api/rooms/{id}` | Eliminar sala | 204, 404 |
| GET | `/api/rooms/{roomId}/messages` | Obtener mensajes de sala | 200 |
| POST | `/api/rooms/{roomId}/messages` | Enviar mensaje a sala | 201 |
| GET | `/openapi/v1.json` | Documento OpenAPI | 200 |

---

## Resumen de archivos del proyecto

```
EDChat.Api/
├── DTOs/
│   ├── UserDto.cs              # UserDto, CreateUserDto (con Data Annotations)
│   ├── RoomDto.cs              # RoomDto, CreateRoomDto, UpdateRoomDto (con Data Annotations)
│   └── MessageDto.cs           # MessageDto, CreateMessageDto
├── Endpoints/
│   ├── UserEndpoints.cs        # GET, POST /api/users (TypedResults)
│   ├── RoomEndpoints.cs        # GET, POST, PUT, DELETE /api/rooms (TypedResults + Filter)
│   └── MessageEndpoints.cs     # GET, POST /api/rooms/{id}/messages (TypedResults)
├── Mappers/
│   └── DtoMappers.cs           # Extension members (C# 14) ToDto/ToEntity
├── Middlewares/
│   └── RequestLoggingMiddleware.cs  # Log de requests con Stopwatch
├── Models/
│   ├── User.cs                 # Id, Username, CreatedAt
│   ├── Room.cs                 # Id, Name, Description, CreatedAt
│   └── Message.cs              # Id, Content, SentAt, UserId, Username, RoomId
├── Services/
│   └── ChatStore.cs            # Almacen in-memory singleton
├── Program.cs                  # Configuracion y pipeline (AddValidation, OpenAPI, CORS)
└── EDChat.Api.csproj           # Dependencias del proyecto
```
