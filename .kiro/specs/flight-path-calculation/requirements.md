# Requirements Document

## Introduction

Ta funkcjonalność implementuje silnik obliczania ścieżki lotu dla aplikacji DroneMesh3D — kolejny krok po zdefiniowaniu obszaru skanowania na mapie. Po zapisaniu poligonu (AreaEntity) w bazie danych, system oblicza optymalną trasę lotu drona nad zdefiniowanym obszarem w celu wykonania fotogrametrycznego skanowania. Silnik wspiera dwa tryby lotu: skanowanie siatką (Grid/Lawnmower) dla ortofotomap i modeli 3D oraz orbitę wokół punktu zainteresowania (POI) dla skanowania fasad i struktur. Algorytm gwarantuje wymagane nakładanie się zdjęć (overlap), bezpieczną wysokość lotu oraz prawidłowy kąt gimbala kamery.

System wykorzystuje istniejące wzorce architektoniczne projektu: MediatR (CQRS), EF Core 10 z NetTopologySuite, OneOf dla typów zwracanych, primary constructors i sealed records. Wyniki obliczeń (waypoints) są persystowane w bazie danych PostgreSQL z PostGIS i powiązane z encją źródłowego obszaru (AreaEntity).

## Glossary

- **Flight_Path_Engine**: Silnik obliczeniowy w .NET 10 odpowiedzialny za generowanie ścieżki lotu drona na podstawie zdefiniowanego obszaru i parametrów kamery
- **Waypoint**: Punkt nawigacyjny 3D (szerokość geograficzna, długość geograficzna, wysokość AGL) wraz z parametrami gimbala, do którego dron się przemieszcza
- **Grid_Mode**: Tryb lotu siatką (Lawnmower) — równoległe linie skanowania pokrywające poligon dla generowania ortofotomap i modeli 3D
- **POI_Mode**: Tryb lotu orbitalnego (Point of Interest) — okrężna ścieżka wokół centralnego punktu dla skanowania fasad i struktur
- **GSD**: Ground Sample Distance — odległość między centrami pikseli mierzona na powierzchni ziemi (cm/piksel), wynikająca z wysokości lotu i parametrów kamery
- **Front_Overlap**: Nakładanie się zdjęć wzdłuż kierunku lotu (along-track), wymagane 75-80%
- **Side_Overlap**: Nakładanie się zdjęć między sąsiednimi liniami lotu (cross-track), wymagane 65-75%
- **AGL**: Above Ground Level — wysokość mierzona od powierzchni gruntu
- **Gimbal**: Mechaniczny uchwyt kamery sterowany w osi pitch (pochylenie) i yaw (obrót)
- **FOV**: Field of View — kątowy zakres obserwacji kamery
- **Heading**: Kierunek kompasowy linii lotu (0-360°)
- **Camera_Parameters**: Zestaw parametrów kamery drona: szerokość sensora (mm), ogniskowa (mm), rozdzielczość obrazu (szerokość × wysokość pikseli)
- **Flight_Statistics**: Statystyki obliczonego lotu: całkowita dystans, szacowany czas lotu, liczba zdjęć, pokryta powierzchnia
- **AreaEntity**: Istniejąca encja bazy danych przechowująca poligon obszaru skanowania (NTS Polygon, SRID 4326)
- **Flight_Plan_Entity**: Encja bazy danych przechowująca obliczony plan lotu powiązany z AreaEntity

## Requirements

### Requirement 1: Wybór trybu lotu

**User Story:** Jako operator drona chcę wybrać tryb lotu (siatka lub orbita POI), aby dostosować ścieżkę lotu do typu skanowania.

#### Acceptance Criteria

1. THE Flight_Path_Engine SHALL obsługiwać dwa tryby lotu: Grid_Mode oraz POI_Mode.
2. WHEN użytkownik wysyła żądanie obliczenia ścieżki lotu, THE Flight_Path_Engine SHALL wymagać jawnego określenia trybu lotu w parametrach żądania i odrzucić żądanie bez jawnego trybu nawet jeśli istnieje domyślny tryb.
3. IF żądanie zawiera nieznany lub brakujący tryb lotu, THEN THE Flight_Path_Engine SHALL zwrócić błąd walidacji z opisem dozwolonych trybów.

### Requirement 2: Obliczanie ścieżki lotu w trybie siatki (Grid)

**User Story:** Jako operator drona chcę, aby system obliczył trasę lotu siatką nad zdefiniowanym obszarem, aby pokryć cały poligon równoległymi liniami skanowania z zachowaniem wymaganego nakładania się zdjęć.

#### Acceptance Criteria

1. WHEN użytkownik zleca obliczenie w Grid_Mode, THE Flight_Path_Engine SHALL przyjąć jako parametry wejściowe: identyfikator AreaEntity, wysokość lotu (m AGL), Camera_Parameters (szerokość sensora mm, ogniskowa mm, szerokość obrazu px, wysokość obrazu px), żądany Front_Overlap (%) oraz żądany Side_Overlap (%).
2. WHEN Flight_Path_Engine otrzyma parametry Grid_Mode, THE Flight_Path_Engine SHALL obliczyć GSD na podstawie wysokości lotu, szerokości sensora i ogniskowej według wzoru: GSD = (wysokość × szerokość_sensora) / (ogniskowa × szerokość_obrazu).
3. WHEN Flight_Path_Engine oblicza ścieżkę siatki, THE Flight_Path_Engine SHALL wyznaczyć footprint zdjęcia na gruncie (szerokość i wysokość pokrycia pojedynczego zdjęcia) na podstawie GSD i rozdzielczości obrazu.
4. WHEN Flight_Path_Engine oblicza ścieżkę siatki, THE Flight_Path_Engine SHALL wyznaczyć odstęp między kolejnymi zdjęciami wzdłuż linii lotu według wzoru: photo_spacing = footprint_height × (1 - front_overlap), gdzie front_overlap jest wartością dziesiętną.
5. WHEN Flight_Path_Engine oblicza ścieżkę siatki, THE Flight_Path_Engine SHALL wyznaczyć odstęp między sąsiednimi liniami lotu na podstawie footprintu i żądanego Side_Overlap, traktując Side_Overlap jako wartość procentową (0-100) konwertowaną na ułamek dziesiętny.
6. WHEN Flight_Path_Engine oblicza orientację linii lotu, THE Flight_Path_Engine SHALL domyślnie zorientować linie wzdłuż najdłuższej osi poligonu; IF użytkownik określi heading poza zakresem 0-360°, THEN THE Flight_Path_Engine SHALL użyć domyślnej orientacji wzdłuż najdłuższej osi poligonu.
7. WHEN Flight_Path_Engine generuje waypoints w Grid_Mode, THE Flight_Path_Engine SHALL przycinać (clip) linie lotu do granic poligonu, generując waypoints wyłącznie wewnątrz lub na krawędzi zdefiniowanego obszaru.
8. THE Flight_Path_Engine SHALL zwrócić uporządkowaną listę Waypoints tworzących równoległe linie skanowania z punktami zwrotnymi (turnaround points) między liniami.

### Requirement 3: Obliczanie ścieżki lotu w trybie orbitalnym (POI)

**User Story:** Jako operator drona chcę, aby system obliczył trasę lotu orbitalnego wokół punktu zainteresowania, aby wykonać skanowanie fasady lub struktury z wielu kątów.

#### Acceptance Criteria

1. WHEN użytkownik zleca obliczenie w POI_Mode, THE Flight_Path_Engine SHALL przyjąć jako parametry wejściowe: punkt centralny (szerokość i długość geograficzna), promień orbity (m), wysokość orbity (m AGL), liczbę zdjęć lub żądany overlap, oraz kąt gimbala (pitch).
2. WHEN Flight_Path_Engine generuje waypoints w POI_Mode, THE Flight_Path_Engine SHALL rozmieścić waypoints równomiernie na okręgu o zadanym promieniu i wysokości, z kątem między kolejnymi punktami równym 360° / liczba_zdjęć.
3. WHEN Flight_Path_Engine generuje waypoints w POI_Mode, THE Flight_Path_Engine SHALL ustawić kierunek gimbala (yaw) każdego waypointa w stronę punktu centralnego.
4. THE Flight_Path_Engine SHALL zwrócić uporządkowaną listę Waypoints tworzących zamkniętą orbitę wokół punktu centralnego.

### Requirement 4: Obliczanie kąta gimbala

**User Story:** Jako operator drona chcę, aby system obliczył optymalny kąt gimbala dla każdego waypointa, aby kamera była prawidłowo skierowana na skanowany obiekt.

#### Acceptance Criteria

1. WHEN Flight_Path_Engine oblicza waypoints w Grid_Mode, THE Flight_Path_Engine SHALL ustawić kąt pitch gimbala na wartość nadir (-90°) domyślnie lub na wartość konfigurowalną w zakresie od -45° do -90° określoną przez użytkownika, stosując odpowiednią metodę obliczeniową dla trybu Grid_Mode.
2. WHEN Flight_Path_Engine oblicza waypoints w POI_Mode, THE Flight_Path_Engine SHALL obliczyć kąt pitch gimbala na podstawie promienia orbity, wysokości orbity i wysokości struktury, stosując odpowiednią metodę obliczeniową dla trybu POI_Mode, aby utrzymać obiekt w centrum kadru.
3. THE Flight_Path_Engine SHALL ograniczyć obliczony kąt pitch gimbala do zakresu od -90° (nadir) do -45° (pochylenie).
4. WHEN waypoints są generowane w sesji obejmującej oba tryby, THE Flight_Path_Engine SHALL stosować metodę obliczania kąta gimbala odpowiednią dla trybu każdego konkretnego waypointa.

### Requirement 5: Egzekwowanie bezpiecznej wysokości lotu

**User Story:** Jako operator drona chcę, aby system wymuszał limity wysokości lotu, aby zapewnić bezpieczeństwo operacji i zgodność z regulacjami lotniczymi.

#### Acceptance Criteria

1. THE Flight_Path_Engine SHALL egzekwować maksymalną wysokość lotu wynoszącą 120 metrów AGL zgodnie z regulacjami EU EASA — bez tolerancji powyżej limitu.
2. IF żądana wysokość lotu przekracza 120 m AGL, THEN THE Flight_Path_Engine SHALL odrzucić cały plan lotu z błędem walidacji wskazującym naruszenie maksymalnej wysokości regulacyjnej.
3. THE Flight_Path_Engine SHALL akceptować dowolną wysokość lotu w zakresie od 0 do 120 m AGL włącznie.

### Requirement 6: Gwarancja nakładania się zdjęć (Overlap)

**User Story:** Jako operator drona chcę, aby system gwarantował wymagane nakładanie się zdjęć, aby zapewnić jakość rekonstrukcji fotogrametrycznej.

#### Acceptance Criteria

1. WHEN Flight_Path_Engine oblicza odstępy w Grid_Mode, THE Flight_Path_Engine SHALL zapewnić Front_Overlap na poziomie co najmniej 75% i nie więcej niż 80%.
2. WHEN Flight_Path_Engine oblicza odstępy w Grid_Mode, THE Flight_Path_Engine SHALL zapewnić Side_Overlap na poziomie co najmniej 65% i nie więcej niż 75%.
3. IF użytkownik poda wartość Front_Overlap poza zakresem 75-80%, THEN THE Flight_Path_Engine SHALL zwrócić błąd walidacji z dopuszczalnym zakresem.
4. IF użytkownik poda wartość Side_Overlap poza zakresem 65-75%, THEN THE Flight_Path_Engine SHALL zwrócić błąd walidacji z dopuszczalnym zakresem.
5. WHEN Flight_Path_Engine oblicza ścieżkę w POI_Mode z parametrem overlap zamiast liczby zdjęć, THE Flight_Path_Engine SHALL obliczyć minimalną liczbę zdjęć zapewniającą żądany overlap na podstawie promienia orbity i FOV kamery.

### Requirement 7: Struktura danych wyjściowych

**User Story:** Jako programista chcę, aby wynik obliczeń zawierał kompletne dane waypoints i statystyki lotu, aby frontend mógł wyświetlić trasę i użytkownik mógł ją ocenić.

#### Acceptance Criteria

1. THE Flight_Path_Engine SHALL zwrócić dla każdego Waypoint: szerokość geograficzną, długość geograficzną, wysokość AGL, kąt pitch gimbala oraz kierunek yaw gimbala (heading w stronę centrum dla POI_Mode).
2. THE Flight_Path_Engine SHALL zwrócić Flight_Statistics zawierające: całkowitą dystans trasy (m), szacowany czas lotu (s), liczbę zdjęć do wykonania oraz pokrytą powierzchnię (m²).
3. WHEN obliczenie zakończy się sukcesem, THE Flight_Path_Engine SHALL zwrócić wynik zawierający zarówno listę Waypoints, jak i Flight_Statistics.

### Requirement 8: Persystencja wyników obliczeń

**User Story:** Jako programista chcę, aby obliczony plan lotu był zapisywany w bazie danych, aby był dostępny do późniejszego pobrania i przesłania do drona.

#### Acceptance Criteria

1. WHEN obliczenie ścieżki lotu zakończy się sukcesem, THE Flight_Path_Engine SHALL zapisać wynik jako Flight_Plan_Entity powiązany z AreaEntity poprzez identyfikator obszaru; IF zapis do bazy danych nie powiedzie się, THEN THE Flight_Path_Engine SHALL traktować operację jako nieudaną i zwrócić błąd użytkownikowi.
2. THE Flight_Plan_Entity SHALL przechowywać: tryb lotu, parametry wejściowe, listę Waypoints, Flight_Statistics oraz znacznik czasu utworzenia.
3. THE Flight_Path_Engine SHALL udostępniać endpoint GET do pobrania zapisanego planu lotu po identyfikatorze; endpoint GET SHALL pozostać dostępny nawet podczas awarii operacji zapisu.
4. IF zapis do bazy danych nie powiedzie się, THEN THE Flight_Path_Engine SHALL zwrócić odpowiedź HTTP 500 z ogólnym komunikatem błędu bez ujawniania szczegółów wewnętrznych.

### Requirement 9: Endpoint API i wzorzec CQRS

**User Story:** Jako programista chcę, aby obliczanie ścieżki lotu było wystawione przez API REST zgodnie z istniejącym wzorcem MediatR (CQRS), aby zachować spójność architektury.

#### Acceptance Criteria

1. THE Flight_Path_Engine SHALL udostępniać endpoint POST pod adresem `/api/flight-plans` akceptujący żądanie obliczenia ścieżki lotu.
2. THE Flight_Path_Engine SHALL udostępniać endpoint GET pod adresem `/api/flight-plans/{id}` zwracający zapisany plan lotu.
3. WHEN endpoint POST otrzyma żądanie, THE Flight_Path_Engine SHALL przetworzyć je przez MediatR command z OneOf jako typem zwracanym (sukces, błąd walidacji, błąd ogólny).
4. IF żądanie nie przejdzie walidacji parametrów wejściowych, THEN THE Flight_Path_Engine SHALL zwrócić odpowiedź HTTP 422 ze szczegółami naruszeń walidacji; walidacja jest warunkiem koniecznym dla zwrócenia HTTP 201.
5. WHEN obliczenie zakończy się sukcesem i walidacja przeszła pomyślnie, THE Flight_Path_Engine SHALL zwrócić odpowiedź HTTP 201 z utworzonym planem lotu zawierającym unikalny identyfikator.
6. IF wskazany AreaEntity nie istnieje w bazie danych, THEN THE Flight_Path_Engine SHALL zwrócić odpowiedź HTTP 404.

### Requirement 10: Wydajność obliczeniowa

**User Story:** Jako operator drona chcę, aby obliczenie ścieżki lotu było szybkie, abym nie czekał długo na wynik.

#### Acceptance Criteria

1. WHEN obszar o powierzchni do 5 hektarów jest przetwarzany, THE Flight_Path_Engine SHALL zakończyć obliczenie ścieżki lotu w czasie poniżej 2 sekund.
2. THE Flight_Path_Engine SHALL wykorzystywać układ współrzędnych WGS84 (SRID 4326) spójnie z istniejącym systemem przechowywania geometrii.
