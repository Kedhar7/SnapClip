# SnapClip

**A modern, smart clipboard manager for Windows.**

> SnapClip runs in the system tray, monitors your clipboard in real-time, and lets you search, pin, organize, and re-paste clips with keyboard shortcuts.

---

## Features

- [x] **Real-time clipboard monitoring** — Text, images, files, and rich text
- [x] **Instant search** — Full-text search with relevance ranking (<50ms for 1000+ clips)
- [x] **Global hotkeys** — `Ctrl+Shift+V` to toggle, customizable shortcuts
- [x] **Pin & favorite clips** — Keep important clips always accessible
- [x] **System tray integration** — Lives in the background, out of your way
- [x] **Light / Dark / High Contrast themes** — Follows your Windows settings
- [x] **AES-256 encryption** — Mark sensitive clips for encryption
- [x] **Feature flags** — Toggle experimental features independently
- [x] **Local telemetry & insights** — Usage dashboard with charts (100% local, nothing transmitted)
- [x] **Full accessibility** — Screen reader support, keyboard navigation, high contrast
- [x] **Smart categorization** — Auto-detect URLs, emails, code, phone numbers (feature flag)
- [x] **Toast notifications** — For key events like startup, sensitive content detection, hotkey conflicts

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Language | C# 12 |
| Framework | .NET 8 + WPF |
| Architecture | MVVM with DI |
| Database | SQLite via EF Core |
| Testing | xUnit + Moq + FluentAssertions |
| CI/CD | GitHub Actions |
| MVVM Toolkit | CommunityToolkit.Mvvm |
| System Tray | Hardcodet.NotifyIcon.Wpf |
| Notifications | Microsoft.Toolkit.Uwp.Notifications |
| Charts | LiveChartsCore.SkiaSharpView.WPF |

## Architecture

```
src/SnapClip/
├── Models/          # Data models (ClipItem, ClipCategory, TelemetryEvent, FeatureFlag)
├── ViewModels/      # MVVM ViewModels (Main, Settings, Insights, ClipItem)
├── Views/           # WPF XAML windows (Main, Settings, Insights)
├── Services/        # Business logic (Clipboard, Storage, Search, Hotkeys, Telemetry, etc.)
├── Data/            # EF Core DbContext and migrations
├── Converters/      # WPF value converters
├── Helpers/         # Win32 interop, image processing, encryption
└── Resources/       # Themes, icons, localized strings
```

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code with C# extension

### Build & Run

```bash
git clone https://github.com/yourusername/SnapClip.git
cd SnapClip
dotnet restore
dotnet build
dotnet run --project src/SnapClip
```

### Run Tests

```bash
dotnet test
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+V` | Toggle SnapClip window |
| `Ctrl+Shift+D` | Delete last clip |
| `Ctrl+Shift+P` | Pin last clip |
| `Enter` | Paste selected clip |
| `Delete` | Delete selected clip |
| `Escape` | Close window |
| `Down Arrow` | Move from search to clip list |
| `Up/Down` | Navigate clip list |

All shortcuts are customizable in Settings.

## Feature Flags

SnapClip includes an experimental feature flag system. Enable Developer Mode in Settings > About to access Feature Flags.

| Flag | Description | Default |
|------|-------------|---------|
| `SmartCategorization` | Auto-categorize clips (URL, email, code, phone, address) | ON |
| `ImageOcr` | Extract text from image clips for search | OFF |
| `SensitiveContentDetection` | Auto-detect credit cards, SSNs, API keys | ON |
| `ClipMerge` | Select and merge multiple clips into one | OFF |
| `SoundEffects` | Play subtle sound on clip capture | OFF |

Flags are stored in `%APPDATA%/SnapClip/features.json`.

## Telemetry

**All telemetry is stored locally on your machine. No data is ever transmitted externally.**

SnapClip tracks usage patterns to power the Insights dashboard:

- Clips captured / pasted (count, type, source app)
- Search queries (query length, result count, latency)
- Feature flag changes
- Session duration
- Peak usage hours

View your usage data in the Insights window (click the chart icon or access from system tray).

## Accessibility

SnapClip is designed to be fully accessible:

- **Keyboard navigation** — Every element reachable via Tab, logical tab order, arrow key navigation
- **Screen reader support** — `AutomationProperties` on all controls, live regions for dynamic content
- **High contrast** — Dedicated theme using Windows SystemColors
- **Focus indicators** — Clearly visible 2px focus borders
- **No color-only information** — All states use icons + text labels

## Performance

| Metric | Target |
|--------|--------|
| Startup time | < 1 second |
| Search latency (1000 clips) | < 50ms |
| Memory (idle/tray) | < 30MB |
| Memory (active window) | < 80MB |
| UI responsiveness | Never blocks UI thread |

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure:
- All tests pass (`dotnet test`)
- No build warnings in Release mode
- Follow Microsoft C# coding conventions
- Add tests for new functionality

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
