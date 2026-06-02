# mermaid_fence.py
def mermaid_format(source, language, css_class, options, md, attrs):
    """Возвращает блок mermaid как есть, без обработки Pygments."""
    # attrs может содержать дополнительные атрибуты, например, 'title'
    title = attrs.get('title', '')
    title_html = f'<div class="mermaid-title">{title}</div>' if title else ''
    # Класс "mermaid" нужен для инициализации Mermaid.js
    return f'{title_html}<pre class="mermaid">{source}</pre>'