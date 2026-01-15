# Blog API

A RESTful Web API built with ASP.NET Core using Onion Architecture for blog management functionality.

## Architecture

This project follows the Onion Architecture pattern with the following layers:

### 1. Domain Layer (BlogApi.Domain)
- **Entities**: Core business entities (User, Blog, FileEntity)
- **Interfaces**: Repository and service interfaces
- **Services**: Domain service interfaces

### 2. Application Layer (BlogApi.Application)
- **Commands**: CQRS command objects for write operations
- **Queries**: CQRS query objects for read operations
- **Services**: Application service interfaces and implementations
- **DTOs**: Data Transfer Objects for API communication

### 3. Infrastructure Layer (BlogApi.Infrastructure)
- **Data**: Database context and configurations
- **Repositories**: Data access implementations
- **Services**: External service implementations (file storage, markdown processing, etc.)

### 4. Presentation Layer (BlogApi.Api)
- **Controllers**: API endpoints
- **Middleware**: Custom middleware components
- **Configuration**: Startup and dependency injection setup

## Technology Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: MySQL with Entity Framework Core
- **Authentication**: JWT Bearer Token
- **Markdown Processing**: Markdig
- **Password Hashing**: BCrypt.Net-Next
- **API Documentation**: Swagger/OpenAPI
- **ORM**: Entity Framework Core with Pomelo MySQL provider

## Project Dependencies

```
BlogApi.Api
├── BlogApi.Application
│   └── BlogApi.Domain
└── BlogApi.Infrastructure
    ├── BlogApi.Application
    └── BlogApi.Domain
```

## Configuration

The application uses the following configuration sections in `appsettings.json`:

- **ConnectionStrings**: Database connection configuration
- **JwtSettings**: JWT token configuration
- **FileStorage**: File upload and storage settings

## Getting Started

1. Ensure MySQL is installed and running
2. Update connection strings in `appsettings.json`
3. Run the application: `dotnet run --project BlogApi.Api`
4. Access Swagger UI at: `https://localhost:7152/swagger`

## Development

This project is set up for development with:
- Swagger UI enabled for API testing
- Development-specific configuration in `appsettings.Development.json`
- Enhanced logging for Entity Framework in development mode