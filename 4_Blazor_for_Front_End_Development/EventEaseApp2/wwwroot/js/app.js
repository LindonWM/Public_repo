window.setupScrollListener = function (element, dotnetHelper) {
    if (!element) return;

    element.addEventListener('scroll', () => {
        const scrollPosition = element.scrollTop + element.clientHeight;
        const scrollHeight = element.scrollHeight;
        
        // Trigger load when user is 200px from bottom
        if (scrollHeight - scrollPosition <= 200) {
            dotnetHelper.invokeMethodAsync('OnScroll');
        }
    });
};
