// Add these functions to your wwwroot/index.html or a separate .js file
// Then include the script in your index.html

// Scroll to the bottom of the chat container
function scrollToBottom(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

// Get the current scroll position
function getScrollPosition(element) {
    if (element) {
        return element.scrollTop;
    }
    return 0;
}

// Restore scroll position after loading new messages
function restoreScrollPosition(element, position) {
    if (element) {
        element.scrollTop = position;
    }
}

// Setup scroll listener for infinite scrolling
function setupScrollListener(element, dotNetHelper) {
    if (!element) return;

    // Add event listener for scroll
    element.addEventListener('scroll', function () {
        // If scrolled to near top (with a small threshold)
        if (element.scrollTop < 50) {
            // Call the .NET method
            dotNetHelper.invokeMethodAsync('LoadMoreMessages');
        }
    });
}