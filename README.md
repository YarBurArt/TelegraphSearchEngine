# TelegraphSearchEngine

This is a lightweight, cross-platform desktop utility that searches telegra.ph, built on `Avalonia` and `.NET 10`. Just for learning

telegra.ph has no search API, but its URLs are predictable: article-name-M-D, where M and D are the month and day of publication, and article-name-N-M-D when several articles share one day. The app generates that URL space, 372 addresses per search and 4092 in advanced mode, checks which ones answer with 8 parallel sockets, and extracts the most frequent words from each article body.

Found articles go into a per-user `SQLite` index (url and keywords), so the next search answers from the index instantly. The scan still runs after that, so newly published articles keep showing up, and known urls get refreshed keywords. Advanced mode also checks the indexed N variants, which finds duplicates but takes much longer.

# Installation

Works on Windows 10 or higher (x64, x86, arm64) and on Linux (x64, arm64, with X11 or Wayland). The only requirement is the .NET 10 SDK, dependencies restore automatically on first build.

clone the repository
```bash
git clone https://github.com/YarBurArt/TelegraphSearchEngine.git
```
```bash
cd TelegraphSearchEngine
```
build and run
```bash
dotnet run --project TelegraphSearchEngine
```
run tests
```bash
dotnet test
```

- the SQLite index lands in `%LOCALAPPDATA%\TelegraphSearchEngine\articles.db` on Windows and in `~/.local/share/TelegraphSearchEngine/articles.db` on Linux
- delete it when it feels stale, it rebuilds by scanning
- if 20 requests in a row fail and nothing is found, the app reports the site as unreachable instead of waiting out every timeout. You know how tg and telegraph already solved it :)

## License
- MIT License (see `LICENSE.txt`).
