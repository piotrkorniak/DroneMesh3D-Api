# Dokumentacja Projektu: DroneMesh3D

**Krótki opis projektu:** Kompletna dokumentacja koncepcyjno-techniczna systemu DroneMesh3D. Projekt zakłada stworzenie aplikacji ułatwiającej generowanie modeli 3D obiektów z wykorzystaniem drona DJI Mini 5 Pro. Dokument ten łączy definicję problemu biznesowego, proponowane rozwiązanie architektoniczne oraz szczegółowy przepływ pracy (User Flow) ze wskazaniem na konkretne technologie (.NET 10, C#, PostgreSQL + PostGIS). Stanowi gotowy fundament pod stworzenie zadań w systemie wersjonowania i rozpoczęcie kodowania.

---

## Struktura projektu

| Folder | Rola | Technologia |
|--------|------|-------------|
| `Api/` | Backend — warstwa HTTP/prezentacji | .NET 10, MediatR, Scalar (OpenAPI) |
| `Core/` | Logika biznesowa i dostęp do danych | EF Core 10, NetTopologySuite, PostGIS |
| `Web/` | Frontend (SPA) | Angular 21, OpenLayers |
| `Tests/` | Testy jednostkowe i integracyjne | xUnit, WebApplicationFactory |

---

## 1. Streszczenie (Executive Summary)
**DroneMesh3D** to system programistyczny służący do automatyzacji procesu pozyskiwania i przetwarzania danych przestrzennych z drona w celu generowania trójwymiarowych modeli budynków. Projekt eliminuje potrzebę ręcznego sterowania dronem podczas robienia zdjęć oraz automatyzuje skomplikowany proces obróbki fotogrametrycznej.

## 2. Definicja Problemu
Ręczne wykonywanie zdjęć obiektu w celu stworzenia modelu 3D jest nieefektywne. Wymaga idealnego zachowania odległości, stałego kąta pochylenia kamery oraz odpowiedniego nakładania się kadrów (overlap). Błędy ludzkie skutkują lukami w końcowym modelu 3D (tzw. dziurami w siatce) i zmuszają do powtarzania lotu. Ponadto, samodzielne zarządzanie zewnętrznymi programami do renderowania modeli 3D bywa żmudne i wymaga ciągłego nadzoru.

## 3. Proponowane Rozwiązanie
Rozwiązaniem jest aplikacja webowa pełniąca funkcję inteligentnego asystenta fotogrametrii. Użytkownik wskazuje na mapie obszar, który chce zeskanować, a system:
1. Przelicza ten obszar na precyzyjną ścieżkę lotu.
2. Generuje gotowy plik z instrukcjami lotu autonomicznego.
3. Przejmuje surowe zdjęcia po zakończonym locie i automatycznie koordynuje pracę silników renderujących (np. WebODM).

---

## 4. Szczegółowy Przebieg Procesu (User Flow) i Architektura

Poniżej znajduje się rozszerzony opis przepływu pracy oraz tego, co dokładnie dzieje się pod spodem w warstwie programistycznej podczas każdego z etapów generowania modelu 3D.

### Krok 1: Definicja Obszaru na Mapie (Frontend i GIS)
Użytkownik otwiera aplikację z osadzoną, interaktywną mapą. Znajdując swoją posesję, za pomocą narzędzi wektorowych rysuje wielokąt (poligon) dokładnie okalający wybrany budynek.
* **Warstwa techniczna:** Interfejs waliduje kształt i przesyła zbiór współrzędnych geograficznych (długość i szerokość) w formacie GeoJSON bezpośrednio do backendu.

### Krok 2: Przeliczanie Trasy Lotu (C# Backend)
Główny silnik aplikacji, napisany w **.NET 10**, przejmuje przesłane współrzędne. To tutaj odbywa się główna kalkulacja matematyczna.
* **Warstwa techniczna:** Algorytm w C# wylicza siatkę lotu (Grid) lub trajektorię orbitalną (Point of Interest). System oblicza bezpieczną wysokość początkową, kąt nachylenia gimbala (np. -45 do -60 stopni) oraz odległość między kolejnymi Waypointami. Algorytm rygorystycznie pilnuje 75-80% nakładania się zdjęć na siebie (overlap).

### Krok 3: Generowanie Pliku Misji
Aplikacja tworzy plik konfiguracyjny z parametrami lotu, przystosowany do ograniczeń lekkich dronów.
* **Warstwa techniczna:** Zamiast komunikacji w czasie rzeczywistym, backend w C# generuje gotowy plik w formacie **CSV** lub **KML** (zrozumiały dla zewnętrznych aplikacji wspierających planowanie misji, jak Litchi czy Dronelink). Plik ten zawiera dokładne komendy: leć na współrzędne X/Y, obniż wysokość do Z, ustaw gimbal, zrób zdjęcie.

### Krok 4: Realizacja Lotu Autonomicznego
Użytkownik ładuje wygenerowany plik do aplikacji sterującej na smartfonie/kontrolerze i ustawia drona na ziemi.
* **Przebieg:** Po wciśnięciu startu, maszyna całkowicie autonomicznie wznosi się na ustaloną wysokość, dolatuje do pierwszego punktu i realizuje zaplanowaną misję, samoczynnie wyzwalając migawkę. Po skanowaniu dron wraca do punktu startu (Return To Home).

### Krok 5: Ingestia Danych i Orkiestracja (.NET Worker Service)
Kluczowy etap automatyzacji, który zdejmuje z barków programisty konieczność ręcznego pilnowania silnika renderującego.
* **Warstwa techniczna:** Po zgraniu zdjęć z karty SD, usługa działająca w tle (**BackgroundService** w .NET) natychmiast wykrywa nową paczkę danych. Serwis przez REST API inicjuje nowe zadanie w silniku fotogrametrycznym (np. WebODM), a statusy postępu loguje bezpiecznie do bazy **PostgreSQL**.

### Krok 6: Wizualizacja i Alternatywne Zastosowania
Po kilkudziesięciu minutach, gdy ciężkie obliczenia na GPU zostaną zakończone, system jest gotowy do prezentacji.
* **Warstwa techniczna:** Backend odbiera powiadomienie z silnika renderującego i za pomocą **SignalR** przesyła informację w czasie rzeczywistym do przeglądarki. Wygenerowany model `.obj` lub `.gltf` wyświetla się w interfejsie.
* **Skalowalność:** Architekturę można elastycznie rozwijać. Zmieniając jedynie parametry w algorytmie nawigacyjnym (Krok 2) na mniejsze odległości i niższe wysokości, ten sam system można wykorzystać w przyszłości do generowania tras dla precyzyjnych mikro-skanów innych obiektów.
