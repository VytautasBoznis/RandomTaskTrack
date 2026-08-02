# Reference — CartFees-admin as the pattern springboard

`C:\Dev\PracticalApps\CartFees-admin\v2\server` is the source of the backend
conventions here. When adding anything new, look there first — matching it
matters more than any local preference.

## Copied as-is

| Pattern | Where |
|---|---|
| Four-project split `.API` / `.Business` / `.Data` / `.Interfaces` | solution root |
| `UnitOfWork` wrapping a raw `NpgsqlConnection` + `IUnitOfWorkFactory` | `Business/Base/` |
| Repositories are **stateless**; the `IUnitOfWork` is passed as the last arg to every method | `Business/Repositories/` |
| `BaseOperation<TReq,TResp>` + `OperationFactory` (DI resolver) + FluentValidation | `Business/Base/` |
| `ErrorHandlingFilterAttribute` → `BaseAppException` → `ApiError` | `API/ActionFilters/` |
| `AuthorizationFilter` as a `TypeFilter` with a minimum-role argument | `API/ActionFilters/` |
| `BaseController.GetSessionModelFromJwt()` populating `request.SessionUserData` | `API/Controllers/Base/` |
| JWT + BCrypt (work factor 12), claim-name constants, `UserRole` enum | `Business/Auth/`, `Data/Models/Constants/` |
| `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true`, snake_case tables | `Program.cs` |
| Serilog config shape, multi-stage Dockerfile, `Section__Key` env binding | `appsettings.json`, `Dockerfile`, compose |

## Deliberate deviations

Three, all noted here so nobody "fixes" them back:

1. **`BaseOperation.RequiresTransaction`** — new virtual, default `false`. Write
   operations override it to `true`. CartFees had no transaction wrapping; this
   app needs it because an AI chat turn can create half a dozen tasks through
   tool calls and a partially-applied plan is worse than a failed one. A nested
   operation joins the caller's transaction rather than opening a second.
2. **Validation failures throw `BadRequestException`** with the FluentValidation
   messages in `Description`. CartFees threw a bare `Exception("Invalid request")`
   with a `// TODO` next to it — this is that TODO done, not a redesign.
3. **CORS is an allow-list** (`Cors:AllowedOrigins`), not `WithOrigins("*")`, and
   `UseHttpsRedirection` is omitted. This API is internet-facing behind a
   reverse proxy that terminates TLS; in-app redirection double-redirects there.

## Naming

Tables are `<group>_<name>` inside one schema: CartFees used `admin.user_users`,
`admin.rule_rules`; here it is `tracker.user_users`, `tracker.task_tasks`,
`tracker.chat_messages`.
