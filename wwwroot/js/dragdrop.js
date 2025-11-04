// Drag and Drop helper functions for Blazor
window.dragDropHelper = {
    dragData: null,

    setDragData: function(event, data) {
        this.dragData = data;
        // Set the effectAllowed on the native event
        if (event && event.dataTransfer) {
            event.dataTransfer.effectAllowed = "move";
            event.dataTransfer.dropEffect = "move";
        }
    },

    getDragData: function() {
        return this.dragData;
    },

    clearDragData: function() {
        this.dragData = null;
    },

    allowDrop: function(event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
            if (event.dataTransfer) {
                event.dataTransfer.dropEffect = "move";
            }
            return false;
        }
    }
};

// Store drag handlers to prevent duplicates
const dragHandlers = new WeakMap();

// Setup drag event for a draggable element
window.setupDragAndDrop = function(element, vesselData) {
    if (!element) return;
    
    try {
        // Remove existing listener if any
        const oldHandler = dragHandlers.get(element);
        if (oldHandler) {
            element.removeEventListener('dragstart', oldHandler);
        }
        
        // Create new handler
        const handler = function(e) {
            window.dragDropHelper.setDragData(e, vesselData);
            if (e.dataTransfer) {
                e.dataTransfer.effectAllowed = "move";
                e.dataTransfer.dropEffect = "move";
                // Also set empty data to enable drop
                e.dataTransfer.setData('text/plain', '');
            }
        };
        
        // Store handler for cleanup
        dragHandlers.set(element, handler);
        
        // Add new listener
        element.addEventListener('dragstart', handler, false);
    } catch (err) {
        // Silently handle setup errors
    }
};

// Setup drop handlers for grid cells
window.setupGridDropHandlers = function(gridElement, width, height, dotNetHelper) {
    if (!gridElement) return;
    
    try {
        const cells = gridElement.querySelectorAll('.grid-cell.drop-zone');
        
        // Track current preview request to cancel stale ones
        let currentPreviewRequest = null;
        
        cells.forEach(function(cell) {
            const gridX = parseInt(cell.getAttribute('data-grid-x'));
            const gridY = parseInt(cell.getAttribute('data-grid-y'));
            
            // Track last preview position to avoid unnecessary updates
            let lastPreviewX = -1;
            let lastPreviewY = -1;
            
            cell.addEventListener('dragover', function(e) {
                e.preventDefault();
                e.stopPropagation();
                if (e.dataTransfer) {
                    e.dataTransfer.dropEffect = "move";
                }
                
                // Get vessel data and calculate preview
                const vesselData = window.dragDropHelper.getDragData();
                if (vesselData) {
                    const parts = vesselData.split('|');
                    if (parts.length >= 3) {
                        const effectiveWidth = parseInt(parts[1]);
                        const effectiveHeight = parseInt(parts[2]);
                        
                        // Only update if position changed
                        if (lastPreviewX !== gridX || lastPreviewY !== gridY) {
                            lastPreviewX = gridX;
                            lastPreviewY = gridY;
                            
                            // Clear previous highlights
                            gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                c.classList.remove('drop-preview', 'drop-preview-invalid');
                            });
                            
                            // Calculate adjusted position to ensure vessel stays within bounds
                            let adjustedX = gridX;
                            let adjustedY = gridY;
                            
                            // Adjust if vessel would go out of bounds horizontally
                            if (adjustedX + effectiveWidth > width) {
                                adjustedX = Math.max(0, width - effectiveWidth);
                            }
                            if (adjustedX < 0) {
                                adjustedX = 0;
                            }
                            
                            // Adjust if vessel would go out of bounds vertically
                            if (adjustedY + effectiveHeight > height) {
                                adjustedY = Math.max(0, height - effectiveHeight);
                            }
                            if (adjustedY < 0) {
                                adjustedY = 0;
                            }
                            
                            // Only proceed if vessel can fit within bounds
                            if (adjustedX + effectiveWidth <= width && adjustedY + effectiveHeight <= height &&
                                adjustedX >= 0 && adjustedY >= 0) {
                                
                                // Get vessel ID to exclude it from collision check (if moving an existing vessel)
                                const vesselId = parts.length >= 5 ? parts[0] : null;
                                
                                // Cancel any previous preview request
                                if (currentPreviewRequest) {
                                    // Clear previous preview immediately
                                    gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                        c.classList.remove('drop-preview', 'drop-preview-invalid');
                                    });
                                }
                                
                                // Create a unique request ID for this preview
                                const requestId = adjustedX + '_' + adjustedY + '_' + effectiveWidth + '_' + effectiveHeight;
                                currentPreviewRequest = requestId;
                                
                                // Check for collisions with placed vessels by calling C#
                                dotNetHelper.invokeMethodAsync('CheckPlacementCollision', adjustedX, adjustedY, effectiveWidth, effectiveHeight, vesselId)
                                    .then(function(hasCollision) {
                                        // Only process if this is still the current request (prevents stale highlights)
                                        if (currentPreviewRequest !== requestId) {
                                            return;
                                        }
                                        
                                        // Always clear previous highlights first
                                        gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                            c.classList.remove('drop-preview', 'drop-preview-invalid');
                                        });
                                        
                                        // Only show preview if placement is valid (no collision and within bounds)
                                        if (!hasCollision) {
                                            // Calculate which cells would be occupied - only valid cells
                                            const occupiedCells = [];
                                            for (let dx = 0; dx < effectiveWidth; dx++) {
                                                for (let dy = 0; dy < effectiveHeight; dy++) {
                                                    const cellX = adjustedX + dx;
                                                    const cellY = adjustedY + dy;
                                                    // Double-check bounds before adding
                                                    if (cellX >= 0 && cellX < width && cellY >= 0 && cellY < height) {
                                                        occupiedCells.push({ x: cellX, y: cellY });
                                                    }
                                                }
                                            }
                                            
                                            // Only highlight if all cells are valid and we have the exact count
                                            // The C# collision check already ensures no overlap with placed vessels
                                            if (occupiedCells.length === effectiveWidth * effectiveHeight) {
                                                occupiedCells.forEach(pos => {
                                                    const targetCell = gridElement.querySelector(`[data-grid-x="${pos.x}"][data-grid-y="${pos.y}"]`);
                                                    if (targetCell) {
                                                        targetCell.classList.add('drop-preview');
                                                    }
                                                });
                                            }
                                        }
                                    })
                                    .catch(function(err) {
                                        // Only clear if this is still the current request
                                        if (currentPreviewRequest === requestId) {
                                            gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                                c.classList.remove('drop-preview', 'drop-preview-invalid');
                                            });
                                        }
                                    });
                            } else {
                                // Clear preview if out of bounds
                                currentPreviewRequest = null;
                                gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                    c.classList.remove('drop-preview', 'drop-preview-invalid');
                                });
                            }
                        }
                    }
                }
            }, false);
            
            cell.addEventListener('dragleave', function(e) {
                // Reset preview tracking
                lastPreviewX = -1;
                lastPreviewY = -1;
                
                // Only clear if we're actually leaving the grid area
                const rect = e.currentTarget.getBoundingClientRect();
                const x = e.clientX;
                const y = e.clientY;
                
                if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) {
                    // Cancel current preview request and clear all previews when leaving the grid
                    currentPreviewRequest = null;
                    gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                        c.classList.remove('drop-preview', 'drop-preview-invalid');
                    });
                }
            }, false);
            
            cell.addEventListener('drop', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                // Reset preview tracking and cancel any pending preview requests
                lastPreviewX = -1;
                lastPreviewY = -1;
                currentPreviewRequest = null;
                
                // Clear all preview highlights
                gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                    c.classList.remove('drop-preview', 'drop-preview-invalid');
                });
                
                const vesselData = window.dragDropHelper.getDragData();
                
                // Use the cell's data attributes directly - this is the cell where drop event fired
                // This is more accurate than calculating from mouse coordinates
                const finalX = gridX;
                const finalY = gridY;
                
                if (vesselData) {
                    dotNetHelper.invokeMethodAsync('HandleDropFromJS', finalX, finalY, vesselData);
                }
            }, false);
        });
    } catch (err) {
        // Silently handle setup errors
    }
};

