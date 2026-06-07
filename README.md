# DOKUMENTACJA PROJEKTU

Rozszerzony Graf Zadań (Extended Task Graph - ETG)


Projekt zaliczeniowy z "Zaawansowanych Interfejsów Graficznych"
Semestr letni, rok 2026

Autorzy:
  - Maciej Kijowski
  - Kacper Krehlik
  - Łukasz Malec
  - Jan Markiewicz
  - Szymon Szeliga



## 1. WSTĘP TEORETYCZNY


Rozszerzony graf zadań (Extended Task Graph - ETG) to skierowany graf acykliczny
(DAG), którego główną cechą odróżniającą go od zwykłego grafu zadań jest podział
zadań na typy zależne od sposobu ich wykonania oraz uwzględnienie ograniczeń
zasobów.

W klasycznym grafie zadań wierzchołki reprezentują zadania, a krawędzie
zależności między nimi — graf skupia się na kolejności wykonywania oraz przepływie
danych. Wszystkie zadania traktowane są jednakowo, co ogranicza zastosowanie
w bardziej złożonych systemach.


ETG wizualizuje, jakie zadania powinny być ukończone przed innymi, a następnie
umożliwia zastosowanie algorytmów szeregowania w celu optymalnego przypisania
zadań do zasobów.


### Rodzaje zadań

| **typ zadania** | **opis** |
|-|-|
|GT  (General Task)|Zadanie ogólne. Wykonywane przez jeden zasób dowolnego typu. |
|UT  (Universal Task)| Zadanie uniwersalne. Preferuje zasób ogólny(niespecjalistyczny). Może być wykonane przez specjalistę, ale z wyższym kosztem|
|DT  (Dedicated Task)|Zadanie dedykowane. Preferuje zasób  specjalistyczny. Może być wykonane przez ogólnego, ale z wyższym kosztem|
|CGT (Common General Task)| Zadanie wspólne ogólne. Wykonywane równocześnie przez wiele zasobów dowolnego typu. Wszystkie przypisane zasoby pracują nad jednym zadaniem jednocześnie|
|CDT (Common Dedicated Task)| Zadanie wspólne dedykowane. Wykonywane równocześnie przez wiele zasobów specjalistycznych. Analogicznie jak CGT, ale preferuje specjalistów|
|CUT (Common Universal Task)|Zadanie wspólne uniwersalne. Wykonywane równocześnie przez wiele zasobów ogólnych.|


### Rodzaje zasobów (procesorów)

| **typ zasobu** | **opis** |
|-|-|
|Specjalistyczne (Specialized = 1)|Zasoby posiadające wyspecjalizowane umiejętności. Najlepiej radzą sobie z zadaniami dedykowanymi (DT, CDT) — wykonują je szybciej i/lub taniej.|
|Ogólne (Specialized = 0)|Zasoby bez specjalizacji. Zadania uniwersalne (UT, CUT) najlepiej realizowane są właśnie przez te zasoby.|



### Plik konfiguracyjny

```txt
@tasks [ilość zadań (N)]
[typ/indeks] [ilość następników] [indeks następnika 1] [indeks następnika 2] [...] [indeks następnika n]
[...]
@proc [ilość procesów (M)]
[szybkość] [typ]
[...]
@times
[macierz czasów N×M]
@cost
[macierz kosztów N×M]
```

## 2. ALGORYTMY SZEREGOWANIA

Aplikacja implementuje pięć algorytmów szeregowania. Każdy algorytm realizuje
interfejs IScheduler, co umożliwia łatwe dodawanie nowych algorytmów
i porównywanie wyników.


- ETG Scheduling Optymalizuje czas zakończenia (makespan)

- HEFT - zaawansowany algorytm dla systemów heterogenicznych (procesory o różnych
szybkościach).

- CPOP - algorytm identyfikujący ścieżkę krytyczną i przypisujący jej zadania
do jednego, najszybszego procesora.

- Greedy Time - algorytm zachłanny wybierający w każdym kroku zadanie blokujące najdłuższą
ścieżkę.

- Greedy Cost - algorytm zachłanny wybierający w każdym kroku najtańsze możliwe przypisanie.


## 3. WYKRES GANTTA


Wykres Gantta to narzędzie wizualizacji harmonogramu, przedstawiające
przypisanie zadań do zasobów w czasie.

W naszej aplikacji wykres Gantta:

  - Oś Y (pionowa) — reprezentuje procesory (zasoby). Każdy procesor
    ma osobny wiersz z etykietą (P0, P1, ...) oraz oznaczeniem typu
    (specjalistyczny / ogólny).

  - Oś X (pozioma) — reprezentuje czas. Skala dobierana automatycznie
    do zakresu harmonogramu, z siatką ułatwiającą odczyt.

  - Podsumowanie — pod wykresem wyświetlany jest makespan (całkowity czas
    wykonania) oraz łączny koszt.



Wykres Gantta generowany jest po kliknięciu przycisku "Szereguj" w zakładce
"Graf" i automatycznie wyświetlany w zakładce "Gantt". Umożliwia porównanie
wyników różnych algorytmów — ten sam graf można zaszeregować różnymi
metodami i wizualnie ocenić różnice.



## 4. Struktura plików

- [`Graph.cs`](./Graph.cs): Model danych (Task, Proc, TaskType) oraz parser pliku
                        konfiguracyjnego. Obsługuje wszystkie sekcje: @tasks,
                        @proc, @times, @cost.
- [`IScheduler.cs`](./IScheduler.cs): Interfejs IScheduler oraz modele wyników
                        (ScheduleResult, ScheduledTask). Umożliwia łatwe
                        dodawanie nowych algorytmów.

- [`EtgScheduler.cs`](./EtgScheduler.cs)    - Algorytm ETG Scheduling
- [`HeftScheduler.cs`](./HeftScheduler.cs)    - Algorytm HEFT
- [`CpopScheduler.cs`](./CpopScheduler.cs)  - Algorytm CPOP
- [`GreedyTimeScheduler.cs`](./GreedyTimeScheduler.cs)- Algorytm Greedy (min. czas)
- [`GreedyCostScheduler.cs`](./GreedyCostScheduler.cs) - Algorytm Greedy (min. koszt)

- [`GraphRenderer.cs `](./GraphRenderer.cs ) - Wizualizacja grafu DAG na WPF Canvas. Sortowanie
                        topologiczne wyznacza warstwy, węzły kolorowane
                        wg typu zadania.

- [`GanttRenderer.cs`](./GanttRenderer.cs) - Wizualizacja wykresu Gantta na WPF Canvas.
                        Automatyczne skalowanie osi czasu, etykiety
                        procesorów, podsumowanie.

- [`MainWindow.xaml`](./MainWindow.xaml)- Definicja interfejsu (XAML): trzy zakładki
                        (Otwórz Graf, Graf, Gantt), panel diagnostyczny.

- [`MainWindow.xaml.cs`](./MainWindow.xaml.cs)  - Logika aplikacji: ładowanie pliku, parsowanie,
                        diagnostyka, wywoływanie schedulerów i rendererów.




oraz folder "Wykonywalny", w którym znajduje się plik .exe oraz potrzebne pliki do uruchomienia.



## Atrybucja
Niektóre elementy graficzne w [`/assets/`](./assets/) są © Google (Material Symbols), pod licencją [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html) z modyfikacjami przez zespół.