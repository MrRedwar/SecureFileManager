# Явно тестовые ZIP этапа 07

Созданы System.IO.Compression.ZipArchive, Store/NoCompression, 02.11.2026.
Не seed рабочего хранилища, не пользовательские документы.

- safe.zip: folder/note.txt с `STAGE07 TEST ONLY` и пустая empty/.
- unsafe-path.zip: одна запись ../stage07-outside.txt; только для проверки отказа ZIP Slip.
- duplicate.zip: same.txt и SAME.TXT; только для проверки отказа коллизии Windows.
- corrupt.zip: 25 байт ASCII, намеренно не ZIP.

Примеры меньше 1 КиБ, ZIP Bomb отсутствуют. Импортировать копии в отдельную тестовую
папку приложения. Опасные примеры не извлекать сторонними инструментами.
