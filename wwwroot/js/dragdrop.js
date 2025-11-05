// Simplified Drag and Drop helper for Blazor
window.dragDropHelper = {
    dragData: null,
    currentPreview: null,

    setDragData: function(event, data) {
        this.dragData = data;
        if (event && event.dataTransfer) {
            event.dataTransfer.effectAllowed = "move";
            event.dataTransfer.dropEffect = "move";
            event.dataTransfer.setData('text/plain', '');
        }
    },

    getDragData: function() {
        return this.dragData;
    },

    clearDragData: function() {
        this.dragData = null;
        this.currentPreview = null;
    },

    getGridCellSize: function(gridElementId) {
        const gridElement = document.getElementById(gridElementId);
        if (!gridElement) {
            return { width: 0, height: 0 };
        }

        const rect = gridElement.getBoundingClientRect();
        const firstCell = gridElement.querySelector('.grid-cell[data-grid-x="0"][data-grid-y="0"]');
        
        if (!firstCell) {
            return { width: 0, height: 0 };
        }

        const cellRect = firstCell.getBoundingClientRect();
        return {
            width: cellRect.width,
            height: cellRect.height
        };
    }
};

// Store drag handlers to prevent duplicates
const dragHandlers = new WeakMap();

// Setup drag event for a draggable element
window.setupDragAndDrop = function(element, vesselData) {
    if (!element) return;
    
    try {
        const oldHandler = dragHandlers.get(element);
        if (oldHandler) {
            element.removeEventListener('dragstart', oldHandler);
        }
        
        const handler = function(e) {
            window.dragDropHelper.setDragData(e, vesselData);
        };
        
        dragHandlers.set(element, handler);
        element.addEventListener('dragstart', handler, false);
    } catch (err) {
        // Silently handle errors
    }
};

function toClientPoint(event) {
    if (!event) {
        return { x: 0, y: 0 };
    }

    if (typeof event.clientX === "number" && typeof event.clientY === "number") {
        return { x: event.clientX, y: event.clientY };
    }

    if (event.touches && event.touches.length > 0) {
        return { x: event.touches[0].clientX, y: event.touches[0].clientY };
    }

    return { x: 0, y: 0 };
}

function getGridPositionFromMouse(event, gridElement, gridWidth, gridHeight) {
    if (!gridElement || gridWidth <= 0 || gridHeight <= 0) {
        return null;
    }

    const point = toClientPoint(event);
    const gridRect = gridElement.getBoundingClientRect();

    if (point.x < gridRect.left || point.y < gridRect.top ||
        point.x > gridRect.right || point.y > gridRect.bottom) {
        return null;
    }

    const cellWidth = gridRect.width / gridWidth;
    const cellHeight = gridRect.height / gridHeight;

    if (!isFinite(cellWidth) || !isFinite(cellHeight) || cellWidth <= 0 || cellHeight <= 0) {
        return null;
    }

    let relativeX = point.x - gridRect.left;
    let relativeY = point.y - gridRect.top;

    // Clamp to ensure we never hit the exact width/height which would push us outside the grid
    relativeX = Math.max(0, Math.min(relativeX, gridRect.width - 0.001));
    relativeY = Math.max(0, Math.min(relativeY, gridRect.height - 0.001));

    const gridX = Math.max(0, Math.min(gridWidth - 1, Math.floor(relativeX / cellWidth)));
    const gridY = Math.max(0, Math.min(gridHeight - 1, Math.floor(relativeY / cellHeight)));

    return { x: gridX, y: gridY };
}

function adjustPositionForBounds(position, effectiveWidth, effectiveHeight, gridWidth, gridHeight) {
    if (!position) {
        return null;
    }

    if (effectiveWidth > gridWidth || effectiveHeight > gridHeight) {
        return null;
    }

    let adjustedX = position.x;
    let adjustedY = position.y;

    if (adjustedX + effectiveWidth > gridWidth) {
        adjustedX = Math.max(0, gridWidth - effectiveWidth);
    }

    if (adjustedY + effectiveHeight > gridHeight) {
        adjustedY = Math.max(0, gridHeight - effectiveHeight);
    }

    adjustedX = Math.max(0, Math.min(adjustedX, gridWidth - 1));
    adjustedY = Math.max(0, Math.min(adjustedY, gridHeight - 1));

    return { x: adjustedX, y: adjustedY };
}

// Store grid handlers to prevent duplicates
const gridHandlers = new WeakMap();

// Setup grid drop handlers - much simpler approach
window.setupGridDropHandlers = function(gridElement, width, height, dotNetHelper) {
    if (!gridElement) return;
    
    try {
        // Check if handlers already exist for this grid
        if (gridHandlers.has(gridElement)) {
            // Remove old handlers before setting up new ones
            const oldHandlers = gridHandlers.get(gridElement);
            if (oldHandlers) {
                gridElement.removeEventListener('dragover', oldHandlers.dragover);
                gridElement.removeEventListener('drop', oldHandlers.drop);
                gridElement.removeEventListener('dragleave', oldHandlers.dragleave);
            }
        }
        
        let lastPreviewX = -1;
        let lastPreviewY = -1;
        let lastPreviewWidth = -1;
        let lastPreviewHeight = -1;
        let lastPreviewCollision = null;
        let previewRequestId = 0;
        let previewElement = null;

        const ensurePreviewElement = () => {
            if (!previewElement) {
                previewElement = document.createElement('div');
                previewElement.className = 'preview-box';
                previewElement.style.position = 'absolute';
                previewElement.style.pointerEvents = 'none';
                previewElement.style.boxSizing = 'border-box';
                previewElement.style.border = '2px solid transparent';
                previewElement.style.background = 'rgba(0, 0, 0, 0.04)';
                previewElement.style.zIndex = '20';
                previewElement.style.display = 'none';
                previewElement.style.display = 'none';
                gridElement.appendChild(previewElement);
            }
            return previewElement;
        };

        const hidePreview = () => {
            if (previewElement) {
                previewElement.style.display = 'none';
                previewElement.classList.remove('preview-valid', 'preview-invalid');
            }
        };

        const updatePreview = (x, y, widthCells, heightCells, hasCollision) => {
            const element = ensurePreviewElement();
            const gridWidthPx = gridElement.clientWidth;
            const gridHeightPx = gridElement.clientHeight;

            if (gridWidthPx === 0 || gridHeightPx === 0) {
                return;
            }

            const cellWidth = gridWidthPx / width;
            const cellHeight = gridHeightPx / height;

            element.style.display = 'block';
            element.style.left = `${x * cellWidth}px`;
            element.style.top = `${y * cellHeight}px`;
            element.style.width = `${widthCells * cellWidth}px`;
            element.style.height = `${heightCells * cellHeight}px`;
            element.classList.remove('preview-valid', 'preview-invalid');
            element.style.borderColor = hasCollision ? '#ef4444' : '#22c55e';
            element.style.background = hasCollision ? 'rgba(239, 68, 68, 0.25)' : 'rgba(34, 197, 94, 0.18)';
            element.classList.add(hasCollision ? 'preview-invalid' : 'preview-valid');
        };

        const resetPreviewState = () => {
            lastPreviewX = -1;
            lastPreviewY = -1;
            lastPreviewWidth = -1;
            lastPreviewHeight = -1;
            lastPreviewCollision = null;
        };
        
        // Store handler functions so we can remove them later if needed
        const handlers = {};
        
        // Handle dragover on the entire grid
        handlers.dragover = function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            if (e.dataTransfer) {
                e.dataTransfer.dropEffect = "move";
            }
            
            const vesselData = window.dragDropHelper.getDragData();
            if (!vesselData) return;
            
            const parts = vesselData.split('|');
            if (parts.length < 3) return;
            
            const effectiveWidth = parseInt(parts[1], 10);
            const effectiveHeight = parseInt(parts[2], 10);

            if (Number.isNaN(effectiveWidth) || Number.isNaN(effectiveHeight)) {
                return;
            }
            
            // Get precise grid position from mouse coordinates
            // This is the EXACT cell the mouse cursor is over - use this directly
            const gridPos = getGridPositionFromMouse(e, gridElement, width, height);
            if (!gridPos) {
                previewRequestId++;
                hidePreview();
                resetPreviewState();
                return;
            }
            
            const adjustedPos = adjustPositionForBounds(gridPos, effectiveWidth, effectiveHeight, width, height);
            if (!adjustedPos) {
                hidePreview();
                resetPreviewState();
                return;
            }
            
            // Update preview immediately - don't wait for position change for better responsiveness
            // Only update if position changed to avoid unnecessary work
            if (adjustedPos.x !== lastPreviewX || adjustedPos.y !== lastPreviewY ||
                effectiveWidth !== lastPreviewWidth || effectiveHeight !== lastPreviewHeight) {
                lastPreviewX = adjustedPos.x;
                lastPreviewY = adjustedPos.y;
                lastPreviewWidth = effectiveWidth;
                lastPreviewHeight = effectiveHeight;

                const requestId = ++previewRequestId;
                hidePreview();

                const previewX = adjustedPos.x;
                const previewY = adjustedPos.y;
                const previewWidth = effectiveWidth;
                const previewHeight = effectiveHeight;
                const previewVesselId = parts.length >= 5 ? parts[0] : null;
                
                // Check collision asynchronously - we'll show ALL cells regardless
                // Use the exact coordinates captured above
                dotNetHelper.invokeMethodAsync('CheckPlacementCollision', previewX, previewY, previewWidth, previewHeight, previewVesselId)
                        .then(function(hasCollision) {
                            if (requestId !== previewRequestId) {
                                return;
                            }
                            
                            lastPreviewCollision = hasCollision;
                            updatePreview(previewX, previewY, previewWidth, previewHeight, hasCollision);
                        })
                        .catch(function(err) {
                            if (requestId === previewRequestId) {
                                hidePreview();
                            }
                        });
            }
        };
        
        handlers.drop = function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            previewRequestId++;
            hidePreview();
            resetPreviewState();
            
            const vesselData = window.dragDropHelper.getDragData();
            if (!vesselData) return;
            
            // Get precise grid position from mouse coordinates
            const gridPos = getGridPositionFromMouse(e, gridElement, width, height);
            if (!gridPos) {
                window.dragDropHelper.clearDragData();
                resetPreviewState();
                return;
            }
            
            // Parse vessel data to get dimensions for adjustment
            const parts = vesselData.split('|');
            if (parts.length >= 3) {
                const effectiveWidth = parseInt(parts[1], 10);
                const effectiveHeight = parseInt(parts[2], 10);
                
                if (!Number.isNaN(effectiveWidth) && !Number.isNaN(effectiveHeight)) {
                    const adjustedPos = adjustPositionForBounds(gridPos, effectiveWidth, effectiveHeight, width, height);
                    if (adjustedPos) {
                        dotNetHelper.invokeMethodAsync('HandleDropFromJS', adjustedPos.x, adjustedPos.y, vesselData);
                    }
                }
            } else {
                // Fallback to original position if parsing fails
                dotNetHelper.invokeMethodAsync('HandleDropFromJS', gridPos.x, gridPos.y, vesselData);
            }
            
            window.dragDropHelper.clearDragData();
        };
        
        handlers.dragleave = function(e) {
            // Only clear if actually leaving the grid area
            const point = toClientPoint(e);
            const rect = gridElement.getBoundingClientRect();
            
            if (point.x < rect.left || point.x > rect.right || point.y < rect.top || point.y > rect.bottom) {
                previewRequestId++;
                hidePreview();
                resetPreviewState();
            }
        };
        
        // Add event listeners
        gridElement.addEventListener('dragover', handlers.dragover, false);
        gridElement.addEventListener('drop', handlers.drop, false);
        gridElement.addEventListener('dragleave', handlers.dragleave, false);
        
        // Store handlers for cleanup
        gridHandlers.set(gridElement, handlers);
        
    } catch (err) {
        // Silently handle errors
    }
};
