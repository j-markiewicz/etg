# Rozszerzone Grafy Zadań (ETG)

**Projekt na Zaawansowane Interfejsy Graficzne** dla grupy:

- Maciej Kijowski
- Kacper Krehlik
- Łukasz Malec
- Jan Markiewicz
- Szymon Szeliga

Interfejs do przeglądania rozszerzonych grafów zadań.

...

## Format Pliku

Przy ładowaniu grafu z pliku program oczekuje, że dane są w poniższym formacie:

```txt
@tasks [ilość zadań (N)]
[typ/indeks] [ilość wymaganych zadań] [indeks wymagania 1] [indeks wymagania 2] [...] [indeks wymagania n]
[...]
@proc [ilość procesów (M)]
[szybkość] [typ]
[...]
@times
[macierz czasów N×M]
@cost
[macierz kosztów N×M]
```

Na przykład:

```txt
@tasks 10
GT0 2 1 2
GT1 2 3 5
UT2 3 9 4 6
CDT3 2 7 9
CGT4 1 8
GT5 1 9
CGT6 0
CGT7 0
DT8 0
CGT9 0
@proc 4
100 1
200 1
500 0
300 0
@times
30 10 3 4
50 20 6 5
20 10 3 5
10 8  1 2
30 15 4 10
50 30 5 5
40 15 10 12
30 15 5 8
20 5  2 4
10 5  3 4
@cost
3 2 50 10
5 4 80 20
3 3 60 20
3 1 20 5
3 2 70 30
5 3 80 15
3 2 70 15
3 2 50 18
3 1 30 10
3 1 40 12
```

## Atrybucja

Niektóre elementy graficzne w [`/assets/`](./assets/) są © Google (Material Symbols), pod licencją [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html) z modyfikacjami przez zespół.
