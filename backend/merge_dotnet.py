import os
from pathlib import Path

# === НАСТРОЙКИ ===
PROJECT_DIR = Path(".")
OUTPUT_FILE = Path("dotnet_source_code.txt")

# Папки, которые точно не нужны
EXCLUDE_DIRS = {
    ".git", ".vs", ".idea", "bin", "obj", "packages", ".nuget",
    "node_modules", "wwwroot/lib", "dist", "build", "test_results"
}

# Расширения исходного кода .NET
ALLOWED_EXTENSIONS = {
    ".cs", ".csproj", ".sln", ".json", ".xml", ".config",
    ".razor", ".cshtml", ".html", ".css", ".scss", ".js", ".ts",
    ".sql", ".md", ".txt"
}
# ==================

with open(OUTPUT_FILE, "w", encoding="utf-8") as out:
    for root, dirs, files in os.walk(PROJECT_DIR):
        # Рекурсивно отключаем обход мусорных папок
        dirs[:] = sorted([d for d in dirs if d not in EXCLUDE_DIRS])
        
        for file in sorted(files):
            filepath = Path(root) / file
            if filepath.suffix.lower() in ALLOWED_EXTENSIONS:
                rel_path = filepath.relative_to(PROJECT_DIR)
                out.write(f"\n{'='*60}\n# Файл: {rel_path}\n{'='*60}\n\n")
                
                try:
                    with open(filepath, "r", encoding="utf-8", errors="replace") as f:
                        out.write(f.read())
                except Exception as e:
                    out.write(f"[ОШИБКА ЧТЕНИЯ: {e}]\n")
                
                out.write("\n")

print(f"✅ Готово. Исходный код .NET собран в: {OUTPUT_FILE.resolve()}")