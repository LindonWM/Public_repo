
/**
 * Obsługa nawigacji mobilnej (Menu Hamburger)
 */
function toggleMenu() {
    const nav = document.querySelector('nav');
    nav.classList.toggle('open');
}

document.querySelector('.hamburger').addEventListener('click', toggleMenu);

/**
 * Filtrowanie projektów w sekcji Portfolio
 */
function filterProjects(category) {
    const articles = document.querySelectorAll('#projects article');
    articles.forEach(article => {
        if (category === 'all' || article.dataset.category === category) {
            article.style.display = 'flex'; // Przywrócenie widoczności (flex)
        } else {
            article.style.display = 'none';
        }
    });
}

/**
 * Funkcje Lightboxa (Powiększanie zdjęć)
 */
function openLightbox(imgSrc) {
    const lightbox = document.getElementById('lightbox');
    const lightboxImg = document.getElementById('lightbox-img');
    lightboxImg.src = imgSrc;
    lightbox.style.display = 'flex';
}

function closeLightbox() {
    const lightbox = document.getElementById('lightbox');
    lightbox.style.display = 'none';
}

/**
 * Główna logika po załadowaniu DOM
 */
document.addEventListener('DOMContentLoaded', () => {
    
    // --- OBSŁUGA LIGHTBOXA ---
    document.querySelectorAll('#projects img').forEach(img => {
        img.addEventListener('click', () => {
            openLightbox(img.src);
        });
    });

    document.querySelector('.close').addEventListener('click', closeLightbox);
    
    document.getElementById('lightbox').addEventListener('click', (e) => {
        if (e.target === document.getElementById('lightbox')) {
            closeLightbox();
        }
    });

    // --- WALIDACJA FORMULARZA W CZASIE RZECZYWISTYM ---
    const form = document.querySelector('form');
    const inputs = form.querySelectorAll('input, textarea');

    // Funkcja wyświetlająca komunikat o błędzie pod polem
    const showError = (input, message) => {
        const container = input.parentElement;
        let errorDisplay = container.querySelector('.error-message');
        
        if (!errorDisplay) {
            errorDisplay = document.createElement('span');
            errorDisplay.className = 'error-message';
            errorDisplay.style.color = '#ff4d4d';
            errorDisplay.style.fontSize = '0.8rem';
            errorDisplay.style.display = 'block';
            errorDisplay.style.marginTop = '5px';
            input.after(errorDisplay);
        }
        
        errorDisplay.textContent = message;
        input.style.borderColor = '#ff4d4d';
    };

    // Funkcja czyszcząca błąd (gdy dane są poprawne)
    const clearError = (input) => {
        const container = input.parentElement;
        const errorDisplay = container.querySelector('.error-message');
        if (errorDisplay) {
            errorDisplay.textContent = '';
        }
        input.style.borderColor = '#4CAF50'; // Zmiana na zielony przy sukcesie
    };

    // Logika sprawdzająca poprawność pojedynczego pola
    const validateInput = (input) => {
        const value = input.value.trim();

        if (input.id === 'name') {
            if (value === '') {
                showError(input, 'Proszę podać imię.');
                return false;
            }
        }
        
        if (input.id === 'email') {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                showError(input, 'Wprowadź poprawny adres e-mail.');
                return false;
            }
        }
        
        if (input.id === 'message') {
            if (value === '') {
                showError(input, 'Wiadomość nie może być pusta.');
                return false;
            }
        }

        clearError(input);
        return true;
    };

    // Nasłuchiwanie zmian podczas pisania (Real-time feedback)
    inputs.forEach(input => {
        input.addEventListener('input', () => {
            validateInput(input);
        });
    });

    // Obsługa wysyłki formularza
    form.addEventListener('submit', (e) => {
        e.preventDefault();
        let isFormValid = true;

        // Sprawdź wszystkie pola przed wysłaniem
        inputs.forEach(input => {
            if (!validateInput(input)) {
                isFormValid = false;
            }
        });

        if (isFormValid) {
            // Logika sukcesu zamiast standardowego alertu
            const successMsg = document.createElement('p');
            successMsg.className = 'success-info';
            successMsg.textContent = 'Dziękujemy! Twoja wiadomość została wysłana.';
            successMsg.style.color = '#4CAF50';
            successMsg.style.fontWeight = 'bold';
            form.appendChild(successMsg);
            
            form.reset();

            // Resetuj style pól po 3 sekundach
            setTimeout(() => {
                inputs.forEach(i => i.style.borderColor = '');
                successMsg.remove();
            }, 3000);
        }
    });
});