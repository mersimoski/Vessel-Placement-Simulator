    namespace BlazorApp.Models;

    /// <summary>
    /// Represents a vessel that is available to be placed.
    /// </summary>
    public class AvailableVessel
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public ShipDimensions Dimensions { get; init; } = new();
        public string Designation { get; init; } = string.Empty;
        public bool IsRotated { get; set; }

        /// <summary>
        /// Gets the effective width considering rotation.
        /// </summary>
        public int EffectiveWidth => IsRotated ? Dimensions.Height : Dimensions.Width;

        /// <summary>
        /// Gets the effective height considering rotation.
        /// </summary>
        public int EffectiveHeight => IsRotated ? Dimensions.Width : Dimensions.Height;

    }

