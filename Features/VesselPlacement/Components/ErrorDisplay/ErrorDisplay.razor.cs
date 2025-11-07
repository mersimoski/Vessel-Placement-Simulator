using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components.ErrorDisplay;

/// <summary>
/// A reusable error display component with optional retry functionality.
/// </summary>
public partial class ErrorDisplay
{
    /// <summary>
    /// The error message to display.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "An error occurred.";

    /// <summary>
    /// Callback invoked when the retry button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnRetry { get; set; }

    /// <summary>
    /// The text to display on the retry button.
    /// </summary>
    [Parameter]
    public string RetryButtonText { get; set; } = "Try Again";

    private async Task HandleRetryClick()
    {
        await OnRetry.InvokeAsync();
    }
}
