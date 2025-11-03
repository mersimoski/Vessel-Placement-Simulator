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
            console.log('Drag started with data:', vesselData);
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
        console.error('Error setting up drag and drop:', err);
    }
};

// Setup drop handlers for grid cells
window.setupGridDropHandlers = function(gridElement, width, height, dotNetHelper) {
    if (!gridElement) return;
    
    try {
        const cells = gridElement.querySelectorAll('.grid-cell.drop-zone');
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
                            
                            // Only show preview if vessel can fit within bounds
                            if (adjustedX + effectiveWidth <= width && adjustedY + effectiveHeight <= height &&
                                adjustedX >= 0 && adjustedY >= 0) {
                                
                                // Get vessel ID to exclude it from collision check (if moving an existing vessel)
                                const vesselId = parts.length >= 5 ? parts[0] : null;
                                
                                // Check for collisions with placed vessels by calling C#
                                dotNetHelper.invokeMethodAsync('CheckPlacementCollision', adjustedX, adjustedY, effectiveWidth, effectiveHeight, vesselId)
                                    .then(function(hasCollision) {
                                        // Only show preview if placement is valid (no collision)
                                        if (!hasCollision) {
                                            // Calculate which cells would be occupied
                                            const occupiedCells = [];
                                            for (let dx = 0; dx < effectiveWidth; dx++) {
                                                for (let dy = 0; dy < effectiveHeight; dy++) {
                                                    const cellX = adjustedX + dx;
                                                    const cellY = adjustedY + dy;
                                                    if (cellX >= 0 && cellX < width && cellY >= 0 && cellY < height) {
                                                        occupiedCells.push({ x: cellX, y: cellY });
                                                    }
                                                }
                                            }
                                            
                                            // Highlight occupied cells with green preview
                                            occupiedCells.forEach(pos => {
                                                const targetCell = gridElement.querySelector(`[data-grid-x="${pos.x}"][data-grid-y="${pos.y}"]`);
                                                if (targetCell) {
                                                    targetCell.classList.remove('drop-preview', 'drop-preview-invalid');
                                                    targetCell.classList.add('drop-preview');
                                                }
                                            });
                                        } else {
                                            // Clear any existing preview if invalid
                                            gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                                c.classList.remove('drop-preview', 'drop-preview-invalid');
                                            });
                                        }
                                    })
                                    .catch(function(err) {
                                        console.error('Error checking collision:', err);
                                        // On error, don't show preview
                                        gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                                            c.classList.remove('drop-preview', 'drop-preview-invalid');
                                        });
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
                    // Clear all previews when leaving the grid
                    gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                        c.classList.remove('drop-preview', 'drop-preview-invalid');
                    });
                }
            }, false);
            
            cell.addEventListener('drop', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                // Reset preview tracking
                lastPreviewX = -1;
                lastPreviewY = -1;
                
                // Clear all preview highlights
                gridElement.querySelectorAll('.grid-cell.drop-preview, .grid-cell.drop-preview-invalid').forEach(c => {
                    c.classList.remove('drop-preview', 'drop-preview-invalid');
                });
                
                const vesselData = window.dragDropHelper.getDragData();
                
                // Calculate precise position within the grid
                const gridRect = gridElement.getBoundingClientRect();
                const dropX = e.clientX - gridRect.left;
                const dropY = e.clientY - gridRect.top;
                
                // Calculate which grid cell based on actual mouse position
                const gridColumns = width;
                const gridRows = height;
                
                const cellWidth = gridRect.width / gridColumns;
                const cellHeight = gridRect.height / gridRows;
                
                const preciseGridX = Math.floor(dropX / cellWidth);
                const preciseGridY = Math.floor(dropY / cellHeight);
                
                // Use the cell's data attributes as fallback
                const finalX = Math.max(0, Math.min(preciseGridX, gridColumns - 1));
                const finalY = Math.max(0, Math.min(preciseGridY, gridRows - 1));
                
                console.log('Drop on cell (' + finalX + ', ' + finalY + ') at pixel (' + dropX + ', ' + dropY + ') with data: ' + vesselData);
                
                if (vesselData) {
                    dotNetHelper.invokeMethodAsync('HandleDropFromJS', finalX, finalY, vesselData);
                }
            }, false);
        });
        
        console.log('Setup drop handlers for ' + cells.length + ' cells');
    } catch (err) {
        console.error('Error setting up grid drop handlers:', err);
    }
};

