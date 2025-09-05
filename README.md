# QuickGuess 🎯

Interaktywna gra quizowa online stworzona w **Blazor WebAssembly** (frontend) i **ASP.NET Core Web API** (backend).  
Łączy tryby zgadywania (filmy, muzyka) z rankingiem graczy i rozbudowanym systemem użytkowników.

---

## ✨ Funkcje

- 🎬 **Jaki to film?** – rozpoznawanie filmów po kadrach (ranking i trening)  
- 🎵 **Jaka to piosenka?** – zgadywanie piosenek po fragmencie audio (ranking i trening)  
- 🏆 **Tryb rankingowy** – punkty, statystyki, miejsce w tabeli  
- 🧩 **Tryb treningowy** – gra bez presji punktów  
- ⏱️ **Timer z animacją** i kolorowym feedbackiem  
- 🎧 **Audio wizualizer** zsynchronizowany z fragmentem muzyki  

### 🔐 Użytkownicy
- Rejestracja i logowanie (JWT)  
- Reset hasła e-mailem  
- Zmiana hasła w panelu  
- Weryfikacja e-maila  
- Logowanie przez Google  

---

## ⚙️ Wymagania

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)  
- [Node.js](https://nodejs.org/) (dla zasobów frontendu i integracji JS)  
- Przeglądarka z obsługą WebAssembly  
- SQLite / SQL Server (konfiguracja w `appsettings.json`)  

---

## 📂 Struktura rozwiązania

```
QuickGuess.sln
│
├── Frontend/         # Blazor WebAssembly
│   ├── Layouty/      # Layouty
│   ├── Pages/        # Komponenty gier i ekrany logowania/rejestracji
│   ├── Services/     # Serwisy
│   ├── Shared/       # Audio Vizualizer
│   ├── wwwroot/   # Skrypty JS (timer, audio, googleAuth)
│   └── Program.cs
│
└── QuickGuess/       # ASP.NET Core Web API
    ├── Controllers/  # Endpointy (auth, gry, ranking, statystyki)
    ├── DTOs/         # Modele transferowe
    ├── Data/         # Kontekst bazy danych
    ├── Models/       # Modele encji
    ├── Services/     # Logika domenowa (Auth, Game, ScoreCalculator)
    ├── Validation/   # Atrybuty walidacyjne
    └── Program.cs
```

---

## 🚀 Uruchomienie projektu

### 1. Backend (ASP.NET Core Web API)

Przejdź do folderu **QuickGuess**:

```bash
cd QuickGuess
```

Skonfiguruj `appsettings.json` (np. connection string, JWT, Google ClientId).  
Uruchom API:

```bash
dotnet run
```

Domyślnie dostępne pod: **https://localhost:7236**

---

### 2. Frontend (Blazor WebAssembly)

Przejdź do folderu **Frontend**:

```bash
cd Frontend
```

Uruchom aplikację Blazor:

```bash
dotnet run
```

Domyślne adresy pod : **https://localhost:7003**  

---

## 🎮 Tryby gry

- **GraFilmRanking** – rankingowe zgadywanie filmów po kadrach  
- **GraFilmTrening** – trening filmów  
- **GraPiosenkaRanking** – rankingowe zgadywanie piosenek  
- **GraPiosenkaTrening** – trening muzyczny  

---

## 📜 Licencja

Projekt open-source na licencji **MIT**.
