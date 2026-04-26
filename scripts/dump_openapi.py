import json
from pathlib import Path
from importlib import import_module

# Укажите путь к вашему FastAPI app (будет создан при старте разработки)
app_path = "src.main:app"

def dump():
    module_path, app_name = app_path.split(":")
    module = import_module(module_path.replace("/", ".")).__dict__[app_name]
    spec = module.openapi()
    Path("docs/reference/api/openapi.json").write_text(json.dumps(spec, indent=2))
    print("✅ openapi.json успешно сгенерирован")

if __name__ == "__main__":
    dump()