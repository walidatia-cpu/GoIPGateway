---
trigger: always_on
description: Mandatory architecture, CQRS/MediatR, Result Pattern, thin controllers, microcomponents, frontend services, and responsive design standards for GoIP Gateway.
---

# GoIP Gateway - Architectural Rules & Standards

## 1. Clean Architecture, CQRS, MediatR & Result Pattern
- **Domain**: Pure business entities, value objects, enums (`GoIPGateway.Domain`).
- **Application**: DTOs, CQRS (Commands & Queries via MediatR), Services/Interfaces, Business Validation (`GoIPGateway.Application`).
- **Infrastructure**: Database context, EF Core repositories, hardware drivers, SignalR, Identity Services (`GoIPGateway.Infrastructure`).
- **Web**: Areas (`Admin`, `Company`), Controllers, Razor Views, Modular Frontend Services (`GoIPGateway.Web`).

### Strict Architectural Flow:
```
Controller ──► IMediator.Send(...) ──► Handler ──► Domain/Application Service ──► Result<T>
```

### Mandatory Controller Standards (100% Thin Controllers):
1. **Zero Business Logic**: Controllers MUST NEVER contain business rules, database queries, LINQ expressions, or permission catalog grouping loops.
2. **Zero Direct DB / Repository Access**: NEVER inject `IUnitOfWork`, `ApplicationDbContext`, or repository interfaces directly into Controllers.
3. **Allowed Injections in Controllers**:
   - `IMediator` (Primary orchestrator)
   - `ITenantProvider` / `ICurrentUserService` (Only for resolving current tenant context if required)
4. **Form Metadata & Lookups**:
   - Never query databases or `AppPermissions` directly in Controller GET actions to populate dropdowns or matrices.
   - Always create and dispatch dedicated MediatR queries (e.g., `GetRoleCreateFormMetadataQuery`, `GetUserCreateFormMetadataQuery`) returning `Result<FormMetadataDto>`.
5. **Result Pattern**:
   - Every Command and Query MUST return `Result<T>` or `Result`.
   - Controllers inspect `result.IsSuccess` (or `result.IsFailure`), handle UI notifications (`TempData` or `ModelState.AddModelError`), and return appropriate Views, Redirects, or JSON payloads.

## 2. Microcomponents & Partial Views Pattern
All new and modified views MUST use standardized partial view microcomponents in `Views/Shared/Partials/`:
- **`_SearchFilterBar.cshtml`**: Standard search and filter toolbar for any listing/index view. Use `SearchFilterBarModel`.
- **`_FilterDropdown.cshtml`**: Reusable searchable dropdown filter microcomponent. Use `FilterDropdownModel`.
- **`_EmptyTableState.cshtml`**: Standard empty state microcomponent with reset action button. Use `EmptyTableStateModel`.
- **Rule**: Never create static button pills for filtering that wrap awkwardly on mobile. Always use `SearchFilterBarModel` with `_SearchFilterBar.cshtml`.

## 3. Frontend Service Architecture (`wwwroot/js/services/`)
- **Rule: Zero Inline Scripts**: Never place inline JavaScript in Razor Views or Layout.
- Modular services are placed under `wwwroot/js/services/`:
  - `comboboxService.js`: Universal searchable combobox for all dropdowns.
  - `tableStickyService.js`: Frozen sticky first/dual columns with scroll shadows.
  - `confirmModalService.js`: Modal interceptors and dialogs.
  - `alertService.js`: Auto-dismiss alerts with countdown progress bar.
  - `themeService.js`: Light/Dark mode state and switcher.
  - `sidebarService.js`: Responsive navigation sidebar.
  - `signalrService.js`: Real-time WebSocket event dispatcher.
  - `userManagementService.js`: Dynamic permissions matrices, password copy-to-clipboard, temp password modals.
  - `site.js`: Central bootstrapper.

## 4. Responsive Tables
- Data tables must use `.table-custom.table-sticky-col` inside `.table-responsive-custom`.
- Every `<th>` must define an explicit `min-width` (e.g. `style="min-width: 120px;"`).
- Use dual labels for mobile: `<span class="d-none d-sm-inline">...</span><span class="d-sm-none">...</span>`.

## 5. Verification
- All tests must pass before completing tasks:
  `dotnet test GoIPGateway.Tests/GoIPGateway.Tests.csproj`
