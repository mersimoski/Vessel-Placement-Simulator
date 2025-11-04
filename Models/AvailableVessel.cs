    namespace BlazorApp.Models;

    /// <summary>
    /// Represents a vessel that is available to be placed.
    /// </summary>
    public class AvailableVessel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ShipDimensions Dimensions { get; set; } = new();
        public string Designation { get; set; } = string.Empty;
        public bool IsRotated { get; set; }
        
        /// <summary>
        /// Gets the effective width considering rotation.
        /// </summary>
        public int EffectiveWidth => IsRotated ? Dimensions.Height : Dimensions.Width;
        
        /// <summary>
        /// Gets the effective height considering rotation.
        /// </summary>
        public int EffectiveHeight => IsRotated ? Dimensions.Width : Dimensions.Height;
        
        /// <summary>
        /// Creates a PlacedVessel from this AvailableVessel at the specified position.
        /// </summary>
        public PlacedVessel ToPlacedVessel(int x, int y)
        {
            return new PlacedVessel
            {
                Dimensions = Dimensions,
                Designation = Designation,
                X = x,
                Y = y,
                IsRotated = IsRotated
            };
        }
    }

