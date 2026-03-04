# User Management API Documentation

## Overview
This API provides CRUD operations for managing user records in the ManagementApp system.

## Base URL
`http://localhost:5000/api/users` (adjust port as needed)

## Endpoints

### 1. Get All Users
**GET** `/api/users`

**Query Parameters:**
- `isActive` (optional): Filter by active status (true/false)
- `department` (optional): Filter by department name

**Response:** 200 OK
```json
[
  {
    "id": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phoneNumber": "+1234567890",
    "department": "IT",
    "position": "Developer",
    "createdAt": "2026-03-04T10:00:00Z",
    "updatedAt": null,
    "isActive": true
  }
]
```

### 2. Get User by ID
**GET** `/api/users/{id}`

**Response:** 200 OK or 404 Not Found
```json
{
  "id": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "+1234567890",
  "department": "IT",
  "position": "Developer",
  "createdAt": "2026-03-04T10:00:00Z",
  "updatedAt": null,
  "isActive": true
}
```

### 3. Create User
**POST** `/api/users`

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "+1234567890",
  "department": "IT",
  "position": "Developer"
}
```

**Notes:**
- `firstName`, `lastName`, and `email` cannot be whitespace-only.
- `email` is normalized (trim + lowercase) before uniqueness checks.
- System-managed fields (`id`, `createdAt`, `updatedAt`, `isActive`) are not accepted in this request body.

**Response:** 201 Created
- Location header: `/api/users/{id}`
- Returns the created user object

### 4. Update User
**PUT** `/api/users/{id}`

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "+1234567890",
  "department": "IT",
  "position": "Senior Developer"
}
```

**Response:** 200 OK or 404 Not Found

**Notes:**
- Route `id` must be a positive integer.
- `isActive` is managed through dedicated PATCH endpoints.

### 5. Delete User
**DELETE** `/api/users/{id}`

**Response:** 204 No Content or 404 Not Found

### 6. Deactivate User
**PATCH** `/api/users/{id}/deactivate`

**Response:** 200 OK
```json
{
  "message": "User deactivated successfully",
  "user": { /* user object */ }
}
```

### 7. Activate User
**PATCH** `/api/users/{id}/activate`

**Response:** 200 OK
```json
{
  "message": "User activated successfully",
  "user": { /* user object */ }
}
```

## Error Responses

### 400 Bad Request
```json
{
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
```

Or for invalid route id:
```json
{
  "message": "ID must be a positive integer"
}
```

### 404 Not Found
```json
{
  "message": "User with ID 123 not found"
}
```

### 409 Conflict
```json
{
  "message": "A user with this email already exists"
}
```

### 500 Internal Server Error

All unhandled exceptions are returned by the global exception middleware with a consistent JSON format.

Development example:
```json
{
  "message": "An unexpected error occurred: Object reference not set to an instance of an object."
}
```

Production example:
```json
{
  "message": "An unexpected error occurred."
}
```

## Field Validations

- **FirstName**: Required, max 100 characters, cannot be whitespace-only
- **LastName**: Required, max 100 characters, cannot be whitespace-only
- **Email**: Required, valid email format, max 255 characters, cannot be whitespace-only, unique (case-insensitive after normalization)
- **PhoneNumber**: Optional, max 20 characters, format `+`, digits, spaces, `(`, `)`, `-` (7-20 chars)
- **Department**: Optional, max 255 characters
- **Position**: Optional, max 100 characters
- **IsActive**: Boolean, defaults to true, updated via activate/deactivate endpoints

## Database Configuration

The API currently uses an **In-Memory Database** for development and testing.

### Switching to SQL Server

1. Update `Program.cs`:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

2. Run migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Running the API

1. Restore packages:
```bash
dotnet restore
```

2. Run the application:
```bash
dotnet run
```

3. Access the API at: `https://localhost:5001/api/users` or `http://localhost:5000/api/users`

## Testing with curl

### Create a user:
```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@example.com",
    "department": "HR",
    "position": "Manager"
  }'
```

### Get all users:
```bash
curl https://localhost:5001/api/users
```

### Get user by ID:
```bash
curl https://localhost:5001/api/users/1
```

### Update user:
```bash
curl -X PUT https://localhost:5001/api/users/1 \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane.smith@example.com",
    "department": "HR",
    "position": "Senior Manager"
  }'
```

### Delete user:
```bash
curl -X DELETE https://localhost:5001/api/users/1
```
