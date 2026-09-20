# Backend structure

```
Application/Abstractions/Persistence/  # repository contracts
Infrastructure/Persistence/Dapper/     # SQL + Dapper implementation
Infrastructure/Persistence/Dapper/     # Dapper repositories and ADO.NET connection factory
Services/                              # business rules
Controllers/                           # HTTP endpoints
Models/                                # EF entities
DTOs/                                  # API contracts
```

Data-access flow: `Service -> repository interface -> Dapper -> ADO.NET SqlConnection -> SQL Server`.

All data access uses Dapper over ADO.NET. New business queries belong in a specific Dapper repository; services must not create a database connection or embed SQL. Commands always use parameters.
