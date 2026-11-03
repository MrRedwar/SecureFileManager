# WPF-render этапа 08

03.11.2026. RenderTargetBitmap на реальных Views/ResourceDictionary/DI.
05 — журнал, 06 — настройки; 1024×640/1440×900 — размеры WPF layout в DIP.
Populated journal содержит ТОЛЬКО изолированные UI-test fixtures (TEST-journal.txt),
не вымышленные данные рабочего приложения. Масштаб bitmap 150% — не фактическая смена Windows DPI.
Прозрачные фоны вложенного view не заменяют тему: standalone-рендер помещён на Sand100.
Остальные рендеры — ignored artifacts/stage08-renders, полный сценарий — WpfShellTests.
Нужны native manual auth/navigation/keyboard/DPI по STAGE08_MANUAL_TEST.md.
