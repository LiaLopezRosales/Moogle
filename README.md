# Moogle! — Smart Text Search Engine

![](moogle.png)

A full-text search engine built with **.NET 8** and **Blazor Server**. Indexes a collection of `.txt` documents and returns relevant results ranked by **TF-IDF** scoring with sparse inverted-index retrieval — designed for speed even at 50K+ documents.

## Features

### Search operators
| Operator | Effect |
|----------|--------|
| `!word` | Exclude documents containing `word` |
| `^word` | Require `word` in every result |
| `word1 ~ word2` | Boost score when terms appear close together |
| `*word` / `**word` | Weight multiplier (2× / 4×) |

### Core search
- **TF-IDF vector-space model** with cosine-like scoring
- **Automatic stop word removal** (IDF-based, no hardcoded list)
- **Spanish stemming** (Snowball algorithm — handles verb/noun/adjective suffixes)
- **Synonym expansion** via `Content/synonyms.txt` (~7,300 groups from Open Multilingual WordNet)
- **Levenshtein-based suggestions** (distance ≤ 3) for misspelled queries
- **Snippet extraction** with highlighted `<strong>` terms via sliding window

### UI
- **Google-like** centered search, results as cards with shadow/hover
- **Dark/light theme** persisted in `localStorage`, no flash on load
- **ES/EN toggle** — all strings localized
- **Score bars** — gradient bar normalized to max score
- **Stats** — "X results in Y ms"
- **Responsive** — single breakpoint at 600px
- **Accessible** — `:focus-visible`, `aria-label`, semantic HTML
- **Animations** — staggered result fade/slide, gated by `prefers-reduced-motion`

## Architecture

```
Moogle/
├── MoogleEngine/        # Class library (search logic)
│   ├── InvertedIndex.cs  # Parallel builder, sparse postings
│   ├── DataBase.cs       # Query executor, TF-IDF scoring
│   ├── Auxiliaries.cs    # Snippet extraction, Levenshtein
│   ├── SpanishStemmer.cs # Snowball stemmer for Spanish
│   ├── Synonyms.cs       # Synonym graph loader
│   └── Moogle.cs         # Public API: Moogle.Query()
├── MoogleServer/        # Blazor Server web app
│   ├── Pages/            # Index.razor (search UI)
│   ├── Shared/           # MainLayout.razor (theme toggle)
│   └── wwwroot/css/      # site.css (full design system)
└── Content/              # .txt documents (gitignored)
```

### Performance

| Metric | Value |
|--------|-------|
| Startup with 50K docs / 500K vocab | < 30s |
| Postings memory | ~2 GB (sparse `List<int>[]`) |
| IDF storage | ~4 MB (`double[]`) |
| Snippet re-read | On-demand from disk (top 30) |
| Query time | O(|query| × avg postings length) |

## Quick start

**Prerequisite:** .NET 8.0 SDK

```bash
make dev      # hot-reload via dotnet watch
make build    # compile only
```

Or manually:

```bash
dotnet watch run --project MoogleServer
```

Place `.txt` files in `Content/` before starting.

## Technology stack

| Layer | Technology |
|-------|-----------|
| Language | C# 12 |
| Runtime | .NET 8.0 |
| Web framework | Blazor Server (Razor components) |
| UI | Vanilla CSS (custom properties, no Bootstrap) |
| Search | Inverted index, TF-IDF, Snowball stemmer |
| Suggestions | Levenshtein distance (O(m+n) memory) |

## What makes this project stand out

- **Sparse inverted index** replaces dense matrices — reduced memory from ~300 GB to ~2 GB for large corpora
- **Parallel document processing** via `Parallel.For` with single-threaded merge phase
- **Automatic IDF-based stop word filtering** — no manual blacklist, adapts to each corpus
- **Synonym expansion** without external API calls (local graph traversal)
- **No JavaScript framework** — Blazor Server handles interactivity, CSS-only animations
- **Full design audit at OPTIK score ~84/100** — modular typography, 4px grid, `prefers-reduced-motion`, dark theme, accessible focus management

## Career relevance

Demonstrates proficiency in:
- **Data structures**: sparse inverted index, postings lists, graph traversal (synonyms)
- **Algorithms**: TF-IDF, cosine similarity, Levenshtein distance, Snowball stemming
- **Systems design**: build-time indexing vs. query-time scoring, memory-aware caching
- **Full-stack**: C# backend + Blazor frontend + CSS design system
- **Performance engineering**: profiling bottlenecks, sparse vs. dense representations

## License

MIT
