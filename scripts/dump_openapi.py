"""
Скрипт для генерации OpenAPI спецификации из ASP.NET Core приложения.

Использование:
    python scripts/dump_openapi.py

Требования:
    - .NET 10 SDK установлен
    - backend/src/Api/Api.csproj существует
"""

import json
import subprocess
import time
import signal
import sys
from pathlib import Path
from urllib.request import urlopen, HTTPError
from urllib.error import URLError

# Пути
BASE_DIR = Path(__file__).parent.parent
BACKEND_DIR = BASE_DIR / "backend" / "src" / "Api"
OUTPUT_FILE = BASE_DIR / "docs" / "reference" / "api" / "openapi.json"
PORT = 5000  # Порт для локального запуска


def run_command(cmd: list[str], cwd: Path = None, timeout: int = 60) -> subprocess.CompletedProcess:
    """Запуск команды с обработкой ошибок."""
    kwargs: dict = {
        "args": cmd,
        "cwd": cwd,
        "capture_output": True,
        "text": True,
        "timeout": timeout,
    }
    if sys.platform == "win32":
        kwargs["creationflags"] = 0x08000000  # CREATE_NO_WINDOW
    return subprocess.run(**kwargs)


def wait_for_server(url: str, max_retries: int = 10, delay: float = 2.0) -> bool:
    """Ждать пока сервер не станет доступен."""
    for i in range(max_retries):
        try:
            response = urlopen(url, timeout=5)
            if response.status == 200:
                return True
        except (HTTPError, URLError, ConnectionRefusedError):
            pass
        print(f"  Ожидание сервера... ({i + 1}/{max_retries})")
        time.sleep(delay)
    return False


def dump_openapi():
    """Генерация OpenAPI спецификации из ASP.NET Core приложения."""
    print("=" * 50)
    print("🚀 Генерация OpenAPI спецификации")
    print("=" * 50)

    # Проверяем существование проекта
    csproj = BACKEND_DIR / "Api.csproj"
    if not csproj.exists():
        print(f"❌ Файл не найден: {csproj}")
        sys.exit(1)

    # Шаг 1: Восстановление зависимостей
    print("\n📦 Восстановление зависимостей...")
    result = run_command(["dotnet", "restore"], cwd=BACKEND_DIR)
    if result.returncode != 0:
        print(f"❌ Ошибка restore:\n{result.stderr}")
        sys.exit(1)
    print("✅ Зависимости восстановлены")

    # Шаг 2: Сборка проекта
    print("\n🔨 Сборка проекта...")
    result = run_command(["dotnet", "build", "--no-restore", "-c", "Debug"], cwd=BACKEND_DIR)
    if result.returncode != 0:
        print(f"❌ Ошибка сборки:\n{result.stderr}")
        sys.exit(1)
    print("✅ Проект собран")

    # Шаг 3: Запуск приложения в фоне
    print(f"\n🏃 Запуск приложения на порту {PORT}...")
    popen_kwargs: dict = {
        "args": ["dotnet", "run", "--no-build", "--no-launch-profile", f"--urls=http://localhost:{PORT}"],
        "cwd": BACKEND_DIR,
        "stdout": subprocess.PIPE,
        "stderr": subprocess.PIPE,
        "text": True,
    }
    if sys.platform == "win32":
        popen_kwargs["creationflags"] = 0x08000000  # CREATE_NO_WINDOW
    process = subprocess.Popen(**popen_kwargs)

    try:
        # Ждем пока сервер запустится
        server_url = f"http://localhost:{PORT}/swagger/v1/swagger.json"
        if not wait_for_server(server_url):
            print("❌ Сервер не запустился в отведенное время")
            process.terminate()
            sys.exit(1)

        print("✅ Сервер запущен")

        # Получаем OpenAPI спецификацию
        print(f"\n📥 Загрузка OpenAPI спецификации из {server_url}...")
        response = urlopen(server_url, timeout=10)
        openapi_json = json.loads(response.read().decode("utf-8"))

        # Сохраняем в docs
        OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
        OUTPUT_FILE.write_text(json.dumps(openapi_json, indent=2, ensure_ascii=False))
        print(f"✅ OpenAPI сохранен в {OUTPUT_FILE}")

    finally:
        # Останавливаем процесс
        print("\n⏹️ Остановка сервера...")
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
        print("✅ Сервер остановлен")

    print("\n" + "=" * 50)
    print("🎉 OpenAPI спецификация успешно сгенерирована!")
    print("=" * 50)


if __name__ == "__main__":
    dump_openapi()
