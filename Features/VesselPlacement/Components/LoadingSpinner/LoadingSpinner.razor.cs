using Microsoft.AspNetCore.Components;

namespace BlazorApp.Features.VesselPlacement.Components.LoadingSpinner;

/// <summary>
/// A reusable loading spinner component with customizable message.
/// </summary>
public partial class LoadingSpinner
{
    /// <summary>
    /// The message to display below the spinner.
    /// </summary>
    [Parameter]
    public string Message { get; set; } = "Loading...";
}
