using System.Collections.Generic;

namespace GoIPGateway.Web.Models.Components;

/// <summary>
/// Reusable model for search & filter toolbar microcomponent.
/// </summary>
public class SearchFilterBarModel
{
    public string SearchInputId { get; set; } = "tableSearch";
    public string SearchPlaceholder { get; set; } = "ابحث هنا...";
    public string OnSearchInput { get; set; } = "";
    public List<FilterDropdownModel> Filters { get; set; } = new();
    public string? CounterId { get; set; }
    public string? CounterLabel { get; set; } = "الإجمالي:";
    public int? InitialCount { get; set; }
    public ActionButtonModel? PrimaryAction { get; set; }
    public string? ResetFiltersFunction { get; set; }
}

/// <summary>
/// Reusable model for a searchable filter dropdown microcomponent.
/// </summary>
public class FilterDropdownModel
{
    public string Id { get; set; } = "";
    public string? Label { get; set; }
    public string? Icon { get; set; }
    public string OnChange { get; set; } = "";
    public string? Placeholder { get; set; }
    public List<FilterOptionModel> Options { get; set; } = new();
}

/// <summary>
/// Option item for a filter dropdown.
/// </summary>
public class FilterOptionModel
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public string? Icon { get; set; }
    public bool IsSelected { get; set; }

    public FilterOptionModel() { }

    public FilterOptionModel(string value, string text, string? badge = null, string? color = null, string? icon = null, bool isSelected = false)
    {
        Value = value;
        Text = text;
        BadgeText = badge;
        BadgeColor = color;
        Icon = icon;
        IsSelected = isSelected;
    }
}

/// <summary>
/// Primary or secondary action button in the toolbar.
/// </summary>
public class ActionButtonModel
{
    public string Text { get; set; } = "";
    public string? Url { get; set; }
    public string? OnClick { get; set; }
    public string? Icon { get; set; } = "bi-plus-lg";
    public string ColorClass { get; set; } = "btn-primary";
}

/// <summary>
/// Reusable model for empty table states microcomponent.
/// </summary>
public class EmptyTableStateModel
{
    public string Icon { get; set; } = "bi-inbox";
    public string Title { get; set; } = "لا توجد بيانات متاحة";
    public string? Message { get; set; } = "لم يتم العثور على أي سجلات تطابق معايير البحث الحالية.";
    public string? ResetActionClick { get; set; }
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}
