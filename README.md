# MyNewApp

**Evolution of a Minimal API in .NET.**  

1. Hosting Model `var builder = WebApplication.CreateBuilder(args);` mit RouteHandlern, z. B. `app.MapGet`
2. Verschiedene Methoden (Routen) implementieren - CRUD.
3. Middleware: Middleware wird bei jedem Request ausgeführt. Reihenfolge ist entscheidend. Schlüsselwort `app.Use...`. Beispiel: `app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));`
4. Filter: Filter werden nur bei bestimmten Routen ausgeführt. Verwendung z. B. bei Validierung: `.AddEndpointFilter(async (context, next) => { ...`
5. Dependency Injection: Auslagern von Funktionen in einen Service. Registrieren des Service und Verwendung der Service-Methoden statt direkten Code in den Routen zu haben. Schlüsselwort `builder.Services.Add...`. Registriert Service im Service Container.

Der wesentliche Beispiel-Code der fertigen API  
https://www.youtube.com/playlist?list=PLdo4fOcmZ0oWunQnm3WnZxJrseIw2zSAk

