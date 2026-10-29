---
trigger: always_on
description: Mandatory architecture, microcomponents, frontend services, and responsive design standards for GoIP Gateway.
---

# GoIP Gateway - Architectural Rules & Standards

## 1. Clean Architecture & Structure
- **Domain**: Pure business entities, value objects, enums (`GoIPGateway.Domain`).
- **Application**: DTOs, CQRS/Services, business validation (`GoIPGateway.Application`).
- **Infrastructure**: Database context, EF Core repositories, hardware drivers, SignalR (`GoIPGateway.Infrastructure`).
- **Web**: Areas (`Admin`, `Company`), Controllers, Razor Views, Services (`GoIPGateway.Web`).

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
  - `site.js`: Central bootstrapper.

## 4. Responsive Tables
- Data tables must use `.table-custom.table-sticky-col` inside `.table-responsive-custom`.
- Every `<th>` must define an explicit `min-width` (e.g. `style="min-width: 120px;"`).
- Use dual labels for mobile: `<span class="d-none d-sm-inline">...</span><span class="d-sm-none">...</span>`.

## 5. Verification
- All tests must pass before completing tasks:
  `dotnet test GoIPGateway.Tests/GoIPGateway.Tests.csproj`
