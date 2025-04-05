// wwwroot/js/chat.js

// Автоматическое изменение высоты текстового поля
function autoResizeTextarea(textarea) {
    const minHeight = 35;
    const maxHeight = 150;

    // Сбрасываем высоту
    textarea.style.height = 'auto';

    // Устанавливаем новую высоту
    if (textarea.scrollHeight <= minHeight) {
        textarea.style.height = `${minHeight}px`;
        textarea.style.overflowY = 'hidden';
    } else if (textarea.scrollHeight <= maxHeight) {
        textarea.style.height = `${textarea.scrollHeight}px`;
        textarea.style.overflowY = 'hidden';
    } else {
        textarea.style.height = `${maxHeight}px`;
        textarea.style.overflowY = 'auto';
    }
}

// Прокрутка вниз
function scrollToBottom(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

// Получение текущей позиции прокрутки
function getScrollPosition(element) {
    return element.scrollTop;
}

// Восстановление позиции прокрутки
function restoreScrollPosition(element, position) {
    element.scrollTop = position;
}

// Установка обработчика прокрутки для подгрузки сообщений
let isLoadingMore = false;
function setupScrollListener(element, dotNetHelper) {
    element.addEventListener('scroll', async function () {
        // Если пользователь прокрутил почти до верха, загружаем еще сообщения
        if (element.scrollTop < 50 && !isLoadingMore) {
            isLoadingMore = true;
            await dotNetHelper.invokeMethodAsync('LoadMoreMessages');
            isLoadingMore = false;
        }
    });
}