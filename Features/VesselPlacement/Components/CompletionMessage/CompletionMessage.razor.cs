using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components.CompletionMessage;

/// <summary>
/// A component that displays a completion message with a reset button.
/// </summary>
public partial class CompletionMessage
{
    /// <summary>
    /// The title of the completion message.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "You did it!";

    /// <summary>
    /// The message to display.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "All vessels have been placed successfully in the anchorage!";

    /// <summary>
    /// The text to display on the reset button.
    /// </summary>
    [Parameter]
    public string ResetButtonText { get; set; } = "Try again!";

    /// <summary>
    /// Callback invoked when the reset button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback OnReset { get; set; }

    private async Task HandleResetClick()
    {
        await OnReset.InvokeAsync();
    }
}
