# GoIP Gateway - System Architecture & Development Standards

This document defines the mandatory architecture, development standards, and code conventions for **GoIP Gateway**. Any new features, views, controllers, or services added to this repository MUST strictly follow these guidelines.

---

## 1. High-Level Architecture Overview

GoIP Gateway is built on **ASP.NET Core 10** using **Clean Architecture**:

```
GoIPGateway/
├── GoIPGateway.Domain/          # Core Domain Entities, Enums, Value Objects (No external deps)
├── GoIPGateway.Application/     # CQRS/Services, Interfaces, DTOs, Business Rules
├── GoIPGateway.Infrastructure/  # EF Core, UDP/Telnet Drivers, SignalR Hubs, Background Services
├── GoIPGateway.Web/             # ASP.NET Core MVC (Admin & Company Areas), Views, Frontend Services
└── GoIPGateway.Tests/           # Unit & Integration Tests (xUnit, Moq, EF InMemory)
```

### Multi-Tenancy & Security
- All tenant-bound entities implement tenant filtering via `ITenantProvider`.
- EF Core global query filters isolate tenant data automatically.
- Security roles: `SuperAdmin` (Admin Area) and `CompanyAdmin` / `CompanyUser` (Company Area).

---

## 2. Frontend Architecture & Modular Services (`wwwroot/js/services/`)

### Rule: ZERO Inline Scripts
**Never write large inline `<script>` blocks inside Razor Views or `_Layout.cshtml`.**
All client-side behavior MUST be encapsulated into reusable, modular services in `wwwroot/js/services/` following the IIFE pattern and exposing clean APIs on `window`.

### Standard Frontend Services:
1. **`ComboboxService` (`comboboxService.js`)**:
   - Universal searchable dropdown engine.
   - Automatically enhances any `<select>` with classes `.form-select`, `.searchable-select`, or attribute `data-searchable="true"` (unless marked `data-searchable="false"`).
   - Features: Sticky internal search box, badges with custom colors (`data-badge`, `data-badge-color`), icons (`data-icon`), keyboard navigation (Arrow keys, Enter, Escape), clear button (`data-allow-clear="true"`).
2. **`TableStickyService` (`tableStickyService.js`)**:
   - Handles Google Standings-style sticky first/dual column freezing for responsive tables on horizontal scroll.
   - Automatically computes scroll positions and applies `.table-scrolled` to render smooth gradient shadows.
   - Full RTL and LTR support.
3. **`ConfirmModalService` (`confirmModalService.js`)**:
   - Automatically intercepts `<form data-confirm="true">` with customizable title, message, detail, and consequence.
   - Programmatic modal dialogs: `window.showConfirmModal(options)` and `window.showAlertModal(options)`.
4. **`AlertService` (`alertService.js`)**:
   - Handles auto-dismissing alerts with countdown progress bars (`.auto-dismiss-alert`). Pauses on hover.
5. **`ThemeService` (`themeService.js`)**:
   - Manages Light/Dark theme switching, persistence in `localStorage`, and event dispatching.
6. **`SidebarService` (`sidebarService.js`)**:
   - Manages responsive sidebar collapse for desktop, tablet auto-collapse, and mobile offcanvas.
7. **`SignalRService` (`signalrService.js`)**:
   - Real-time WebSocket connection to `/hubs/goip`.
   - Dispatches browser custom events: `goip:simBalanceChanged`, `goip:transactionUpdated`, `goip:deviceStatusChanged`.
8. **`site.js`**:
   - The central orchestrator that bootstraps all services on `DOMContentLoaded`.

---

## 3. Microcomponents & Partial Views Pattern (`Views/Shared/Partials/`)

When building or updating pages, use the standardized microcomponents in `Views/Shared/Partials/`:

### 1. `_SearchFilterBar.cshtml` (Search & Filter Bar)
- **Model**: `GoIPGateway.Web.Models.Components.SearchFilterBarModel`
- **Purpose**: Unified search input, dropdown filter criteria with internal live search, item counter, reset button, and primary action button.
- **Rule**: Never create static button pills for filtering that wrap awkwardly on mobile. Always use `SearchFilterBarModel` with `_SearchFilterBar.cshtml`.

```csharp
@{
    var filterBar = new SearchFilterBarModel
    {
        SearchInputId = "mySearchInput",
        SearchPlaceholder = "ابحث بالاسم أو الكود...",
        OnSearchInput = "filterItems()",
        CounterId = "visibleCount",
        CounterLabel = "العناصر الظاهرة:",
        InitialCount = Model.Count,
        ResetFiltersFunction = "resetFilters()",
        Filters = new List<FilterDropdownModel>
        {
            new FilterDropdownModel
            {
                Id = "statusFilter",
                Label = "معيار الحالة:",
                Icon = "bi-funnel-fill",
                OnChange = "onStatusChange(this.value)",
                Options = new List<FilterOptionModel>
                {
                    new FilterOptionModel("ALL", "الكل", badge: Model.Count.ToString(), color: "primary", isSelected: true),
                    new FilterOptionModel("ACTIVE", "النشطة فقط", badge: activeCount.ToString(), color: "success")
                }
            }
        },
        PrimaryAction = new ActionButtonModel
        {
            Text = "إضافة جديد",
            Url = Url.Action("Create"),
            Icon = "bi-plus-lg",
            ColorClass = "btn-primary"
        }
    };
}
<div class="portal-card mb-4 shadow-sm">
    <partial name="Partials/_SearchFilterBar" model="filterBar" />
</div>
```

### 2. `_FilterDropdown.cshtml` (Single Searchable Filter Dropdown)
- **Model**: `GoIPGateway.Web.Models.Components.FilterDropdownModel`
- Can be used independently or inside toolbars. Automatically rendered by `ComboboxService`.

### 3. `_EmptyTableState.cshtml` (Empty State)
- **Model**: `GoIPGateway.Web.Models.Components.EmptyTableStateModel`
- Standard empty state with icon, title, description, and optional reset filter button or action link.

---

## 4. Responsive Table Standards

All data tables in GoIP Gateway must follow these guidelines:
1. **Container**: Must be wrapped inside `<div class="portal-card shadow-sm table-card-container mb-4"><div class="table-responsive table-responsive-custom">`.
2. **Sticky Column**: The table must include class `table-custom table-sticky-col align-middle mb-0`.
3. **Column Widths**: Every `<th>` must have an explicit `style="min-width: ...px;"` to prevent column squishing on mobile.
4. **Responsive Labels**: Long column headers and data should provide shortened labels for small screens:
   ```html
   <th style="min-width: 125px;">
       <span class="d-none d-sm-inline">اسم الشركة</span>
       <span class="d-sm-none">الشركة</span>
   </th>
   ```
5. **No Text Truncation in Sticky Column**: The sticky column must never wrap or overlap adjacent cells.

---

## 5. Multi-Device Responsive Design System

The system must render flawlessly across all standard viewport tiers:
- **Mobile (< 768px)**: Offcanvas sidebar, bottom floating navigation bar, compact cards, horizontal table scrolling with sticky first column.
- **Tablet (768px - 991.98px)**: Auto-collapsed sidebar, touch target min-height 40px, full container padding.
- **Laptop / Desktop (992px - 1399.98px)**: Expanded sidebar, full toolbars.
- **Ultra-Wide / 4K (>= 1400px)**: Centered main layout container max-width 1760px.

---

## 6. Testing & Quality Verification

- Every change MUST build cleanly without errors.
- Always run the test suite to verify no regressions:
  ```bash
  dotnet test GoIPGateway.Tests/GoIPGateway.Tests.csproj
  ```
- Preserve existing business logic, error handling, and authorization attributes.
