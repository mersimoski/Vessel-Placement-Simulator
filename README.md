# Blazor Vessel Placement App

A Blazor WebAssembly Single Page Application (SPA) for solving the bin-packing problem of placing vessels in an anchorage area. This application allows users to manually place vessels in an anchorage using drag-and-drop functionality.

## Project Setup

This project was created using .NET 9 SDK with the Blazor WebAssembly template.

### Prerequisites
- .NET 9 SDK installed
- A modern web browser

### Running the Application

To run the application:

```bash
cd BlazorApp
dotnet run
```

The application will be available at:
- HTTP: `http://localhost:5139`
- HTTPS: `https://localhost:7106`

### Project Structure

```
BlazorApp/
├── Components/           # Reusable Blazor components
│   ├── AnchorageGrid.razor
│   └── DraggableVessel.razor
├── Models/               # Data models
│   ├── FleetData.cs      # API response models
│   ├── PlacedVessel.cs   # Vessel placed in anchorage
│   └── AvailableVessel.cs # Vessel available to place
├── Pages/               # Razor pages/components
│   └── VesselPlacement.razor  # Main application page
├── Services/            # Business logic services
│   ├── IFleetApiService.cs
│   └── FleetApiService.cs
├── Layout/             # Layout components
│   └── MainLayout.razor
├── wwwroot/            # Static files (CSS, JS, images)
│   ├── css/
│   │   └── app.css
│   └── js/
│       └── dragdrop.js  # Drag and drop JavaScript interop
├── BlazorApp.Tests/    # Unit tests project
│   ├── PlacedVesselTests.cs
│   └── FleetApiServiceTests.cs
├── Program.cs          # Application entry point
└── BlazorApp.csproj    # Project file
```

## Features

### ✅ Implemented Features

1. **API Integration**
   - ✅ Connects to `https://esa.instech.no/api/fleets/random`
   - ✅ Fetches anchorage size and vessel fleet data
   - ✅ Handles JSON response parsing with error handling

2. **Vessel Placement UI**
   - ✅ Displays anchorage area as a grid based on `anchorageSize`
   - ✅ Displays available vessels as draggable cards
   - ✅ Drag-and-drop functionality to place vessels
   - ✅ Vessel rotation (90 degrees) via double-click
   - ✅ Collision detection prevents vessel overlap
   - ✅ Bounds checking ensures vessels stay within anchorage
   - ✅ Status display showing remaining vessels count

3. **State Management**
   - ✅ Tracks placed vessels and their positions
   - ✅ Tracks remaining available vessels
   - ✅ Detects completion state when all vessels are placed

4. **User Experience**
   - ✅ "Try again!" button to request new fleet data
   - ✅ Success screen when all vessels are placed
   - ✅ Loading states during API calls
   - ✅ Error handling for API failures
   - ✅ Click placed vessels to remove them
   - ✅ Modern, responsive UI with Bootstrap

## Architecture & Design Principles

### SOLID Principles Applied

1. **Single Responsibility Principle (SRP)**
   - `FleetApiService`: Only handles API communication
   - `AnchorageGrid`: Only handles grid display and drop handling
   - `DraggableVessel`: Only handles vessel display and drag initiation
   - `PlacedVessel`: Contains vessel placement logic and validation

2. **Open/Closed Principle (OCP)**
   - `IFleetApiService` interface allows for easy extension/swap of implementations
   - Models are extensible without modifying existing code

3. **Liskov Substitution Principle (LSP)**
   - Models use proper inheritance and polymorphism where applicable

4. **Interface Segregation Principle (ISP)**
   - `IFleetApiService` contains only the methods needed by consumers

5. **Dependency Inversion Principle (DIP)**
   - Components depend on `IFleetApiService` interface, not concrete implementation
   - Dependency injection configured in `Program.cs`

### Code Organization

- **Models**: Data structures for API responses and vessel states
- **Services**: Business logic separated from UI components
- **Components**: Reusable UI components with encapsulated logic
- **Pages**: Main application pages that compose components

### Testing

The project includes comprehensive unit tests using xUnit. The following areas are tested:

1. **Collision Detection Logic** (`PlacedVessel.OverlapsWith()`)
   - ✅ Overlap scenarios (vessels that overlap)
   - ✅ Non-overlapping vessels
   - ✅ Edge cases (adjacent vessels, same position, rotated vessels)

2. **Bounds Checking** (`PlacedVessel.IsWithinBounds()`)
   - ✅ Vessels within bounds
   - ✅ Vessels partially out of bounds
   - ✅ Vessels completely out of bounds
   - ✅ Negative coordinates
   - ✅ Boundary conditions

3. **API Service** (`FleetApiService`)
   - ✅ JSON deserialization with correct API structure
   - ✅ Error handling for invalid JSON
   - ✅ Empty response handling
   - ✅ Multiple fleet handling

4. **Position Calculation** (`PlacedVessel.GetOccupiedPositions()`)
   - ✅ Rotation scenarios (normal and rotated)
   - ✅ Different vessel sizes
   - ✅ Effective width/height calculations

**Running Tests:**
```bash
cd BlazorApp
dotnet test BlazorApp.Tests/BlazorApp.Tests.csproj
```

**Test Coverage:**
- 18 tests for `PlacedVessel` model
- 5 tests for `FleetApiService` JSON deserialization
- Total: 24 passing tests

## API Response Format

```json
{
  "anchorageSize": {
    "width": 12,
    "height": 15
  },
  "fleets": [
    {
      "singleShipDimensions": { "width": 6, "height": 5 },
      "shipDesignation": "LNG Unit",
      "shipCount": 2
    },
    {
      "singleShipDimensions": { "width": 3, "height": 12 },
      "shipDesignation": "Science & Engineering Ship",
      "shipCount": 5
    }
  ]
}
```

## Usage

1. **Load Fleet Data**: On page load, the app automatically fetches random fleet data from the API.

2. **Place Vessels**: 
   - Drag vessels from the right panel into the anchorage grid
   - Drop on any valid grid cell (within bounds, no overlaps)

3. **Rotate Vessels**:
   - Double-click an available vessel to rotate it 90 degrees
   - Rotated vessels show a badge indicator

4. **Remove Vessels**:
   - Click on a placed vessel to remove it and return it to the available pool

5. **Complete the Puzzle**:
   - Place all vessels without overlaps
   - Success screen appears when complete
   - Use "Try again!" to load a new puzzle

## Technologies Used

- **.NET 9**: Latest .NET framework
- **Blazor WebAssembly**: Client-side web framework
- **Bootstrap 5**: CSS framework for styling
- **C# 12**: Modern C# features (records, nullable reference types, etc.)

## Future Enhancements

Potential improvements for the application:

1. **Visual Feedback**: Highlight invalid drop zones, show grid coordinates
2. **Persistence**: Save/load vessel placements (localStorage)
3. **Animation**: Smooth animations for vessel placement and rotation
4. **Accessibility**: Improve keyboard navigation and screen reader support
5. **Performance**: Optimize rendering for large anchorage sizes
6. **Validation**: Visual feedback for placement attempts (success/failure)
7. **Integration Tests**: Add end-to-end tests for the complete drag-and-drop flow

## License

This project was created as a coding assessment task.
