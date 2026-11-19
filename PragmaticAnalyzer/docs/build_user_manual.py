from pathlib import Path
import math
import textwrap

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor
from PIL import Image, ImageDraw, ImageFont, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
ASSETS = DOCS / "manual_assets"
SCREENSHOTS = DOCS / "manual_screenshots"
OUT = DOCS / "Руководство_пользователя_PragmaticAnalyzer.docx"

BLUE = RGBColor(46, 116, 181)
DARK_BLUE = RGBColor(31, 77, 120)
NAVY = RGBColor(11, 37, 69)
GRAY = RGBColor(90, 98, 110)
LIGHT_BLUE = "E8EEF5"
LIGHT_GRAY = "F2F4F7"
CALLOUT = "F4F6F9"
BORDER = "B8C2CC"
WHITE = "FFFFFF"


def font_path(name):
    candidates = [
        Path(r"C:\Windows\Fonts") / name,
        Path("/usr/share/fonts/truetype/dejavu") / name,
    ]
    for candidate in candidates:
        if candidate.exists():
            return str(candidate)
    return None


FONT_REG = font_path("arial.ttf") or font_path("DejaVuSans.ttf")
FONT_BOLD = font_path("arialbd.ttf") or font_path("DejaVuSans-Bold.ttf") or FONT_REG


def pil_font(size, bold=False):
    try:
        return ImageFont.truetype(FONT_BOLD if bold else FONT_REG, size)
    except Exception:
        return ImageFont.load_default()


def wrap_draw_text(draw, text, font, max_width):
    words = str(text).split()
    lines = []
    line = ""
    for word in words:
        probe = word if not line else f"{line} {word}"
        bbox = draw.textbbox((0, 0), probe, font=font)
        if bbox[2] - bbox[0] <= max_width:
            line = probe
        else:
            if line:
                lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines or [""]


def draw_center_text(draw, box, text, font, fill="#172033", max_lines=4):
    x1, y1, x2, y2 = box
    lines = wrap_draw_text(draw, text, font, max(40, x2 - x1 - 24))[:max_lines]
    heights = [draw.textbbox((0, 0), line, font=font)[3] - draw.textbbox((0, 0), line, font=font)[1] for line in lines]
    total_h = sum(heights) + (len(lines) - 1) * 6
    y = y1 + ((y2 - y1) - total_h) / 2
    for line, h in zip(lines, heights):
        bbox = draw.textbbox((0, 0), line, font=font)
        w = bbox[2] - bbox[0]
        draw.text((x1 + ((x2 - x1) - w) / 2, y), line, font=font, fill=fill)
        y += h + 6


def round_rect(draw, box, fill, outline="#B7C2D0", radius=16, width=2):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def arrow(draw, start, end, color="#6C7A89", width=4):
    draw.line([start, end], fill=color, width=width)
    angle = math.atan2(end[1] - start[1], end[0] - start[0])
    length = 18
    for delta in (math.pi * 0.84, -math.pi * 0.84):
        x = end[0] + length * math.cos(angle + delta)
        y = end[1] + length * math.sin(angle + delta)
        draw.line([end, (x, y)], fill=color, width=width)


def create_canvas(path, title):
    img = Image.new("RGB", (1600, 900), "#FFFFFF")
    draw = ImageDraw.Draw(img)
    title_font = pil_font(38, True)
    draw.text((54, 36), title, font=title_font, fill="#0B2545")
    draw.line([(54, 92), (1546, 92)], fill="#D6DEE8", width=3)
    return img, draw


def save_img(img, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, quality=94)
    return path


def diagram_navigation():
    path = ASSETS / "01_main_navigation.png"
    img, draw = create_canvas(path, "Главное окно и навигация")
    tab_font = pil_font(23, True)
    body_font = pil_font(24)
    tabs = [
        "Загрузить БД", "Работа с БД", "Онтологии", "Модели",
        "Работа с ИЗ", "Отчет", "Общение", "Информация"
    ]
    x = 62
    y = 132
    for tab in tabs:
        w = max(145, int(draw.textlength(tab, font=tab_font)) + 36)
        fill = "#DDEBFF" if tab == "Общение" else "#F5F7FA"
        outline = "#2F6FDB" if tab == "Общение" else "#C7D0DA"
        round_rect(draw, (x, y, x + w, y + 62), fill, outline, radius=10, width=3 if tab == "Общение" else 2)
        draw_center_text(draw, (x, y, x + w, y + 62), tab, tab_font, fill="#0B2545", max_lines=1)
        x += w + 14
    round_rect(draw, (62, 245, 1538, 790), "#F8FAFC", "#D1DAE5", radius=18, width=2)
    draw.text((100, 286), "Рабочая область выбранной вкладки", font=pil_font(32, True), fill="#172033")
    bullets = [
        "верхняя панель показывает активный раздел цветом;",
        "каждая вкладка открывает отдельный сценарий работы;",
        "служебные сообщения и ошибки показываются внутри текущего сценария или отдельным окном."
    ]
    yy = 355
    for item in bullets:
        draw.text((128, yy), item, font=body_font, fill="#27364A")
        yy += 58
    return save_img(img, path)


def diagram_load_db():
    path = ASSETS / "02_load_db.png"
    img, draw = create_canvas(path, "Вкладка «Загрузить БД»")
    body = pil_font(22)
    header = pil_font(24, True)
    rows = [
        ("Уязвимости", "локальный JSON", "...", "обновить"),
        ("Угрозы", "локальный JSON", "...", "обновить"),
        ("Эксплойты", "локальный JSON", "...", "обновить"),
        ("Меры защиты", "JSON", "...", ""),
        ("Риски", "JSON", "...", ""),
        ("Техники и тактики", "JSON", "...", ""),
        ("Нарушители", "JSON", "...", ""),
        ("ПФ СОПКА", "текущее/эталонное", "...", ""),
    ]
    round_rect(draw, (70, 135, 1530, 790), "#F8FAFC", "#CED8E4", radius=18, width=2)
    draw.text((115, 165), "База данных", font=header, fill="#0B2545")
    draw.text((560, 165), "Источник", font=header, fill="#0B2545")
    draw.text((985, 165), "Загрузка", font=header, fill="#0B2545")
    draw.text((1240, 165), "Состояние", font=header, fill="#0B2545")
    y = 218
    for i, (name, src, pick, update) in enumerate(rows):
        fill = "#FFFFFF" if i % 2 == 0 else "#F2F6FA"
        draw.rounded_rectangle((100, y, 1500, y + 58), radius=10, fill=fill, outline="#D8E0EA")
        draw.text((120, y + 16), name, font=body, fill="#172033")
        draw.text((560, y + 16), src, font=body, fill="#344154")
        round_rect(draw, (985, y + 9, 1060, y + 49), "#E8EEF5", "#9FB2C7", radius=8, width=2)
        draw_center_text(draw, (985, y + 9, 1060, y + 49), pick, body)
        if update:
            round_rect(draw, (1085, y + 9, 1212, y + 49), "#E8EEF5", "#9FB2C7", radius=8, width=2)
            draw_center_text(draw, (1085, y + 9, 1212, y + 49), "⟳", pil_font(25, True))
        status_color = "#DDF5E5" if i in (0, 1, 2) else "#FFE3E0"
        status_text = "загружено" if i in (0, 1, 2) else "ожидает"
        round_rect(draw, (1240, y + 9, 1430, y + 49), status_color, "#A7B4C2", radius=8, width=1)
        draw_center_text(draw, (1240, y + 9, 1430, y + 49), status_text, body)
        y += 68
    return save_img(img, path)


def diagram_db_search():
    path = ASSETS / "03_db_search.png"
    img, draw = create_canvas(path, "Поиск по базам данных")
    font = pil_font(22)
    bold = pil_font(24, True)
    # search bar
    round_rect(draw, (70, 135, 470, 188), "#FFFFFF", "#8EA3BD", radius=10, width=2)
    draw.text((92, 151), "powershell .locked", font=font, fill="#27364A")
    round_rect(draw, (480, 135, 535, 188), "#E8EEF5", "#8EA3BD", radius=10, width=2)
    draw_center_text(draw, (480, 135, 535, 188), "⚙", pil_font(25, True))
    round_rect(draw, (548, 135, 670, 188), "#2F6FDB", "#2F6FDB", radius=10, width=2)
    draw_center_text(draw, (548, 135, 670, 188), "Найти", bold, fill="#FFFFFF")
    columns = [
        ("1. Запрос", "Пользователь вводит слово или словосочетание и выбирает режим поиска."),
        ("2. Результаты", "Открывается вкладка со ссылками на найденные записи во всех подключенных БД."),
        ("3. Карточка", "При выборе результата показываются полные поля записи и выделяются найденные слова."),
    ]
    x = 95
    for title, text in columns:
        round_rect(draw, (x, 275, x + 420, 680), "#F8FAFC", "#CAD5E2", radius=18, width=2)
        draw.text((x + 28, 305), title, font=bold, fill="#0B2545")
        yy = 370
        for line in wrap_draw_text(draw, text, font, 350):
            draw.text((x + 28, yy), line, font=font, fill="#27364A")
            yy += 34
        x += 510
    arrow(draw, (515, 480), (592, 480))
    arrow(draw, (1025, 480), (1102, 480))
    return save_img(img, path)


def diagram_db_sections():
    path = ASSETS / "04_db_sections.png"
    img, draw = create_canvas(path, "Разделы вкладки «Работа с БД»")
    font = pil_font(21)
    bold = pil_font(23, True)
    labels = [
        "Угрозы", "Нарушитель", "Уязвимости", "Эксплойты",
        "Меры защиты", "Техники и тактики", "Риски ИБ", "Специалист по ЗИ",
        "Текущее состояние ПФ СОПКА", "Эталонное состояние ПФ СОПКА"
    ]
    positions = [(95, 170), (430, 170), (765, 170), (1100, 170),
                 (95, 330), (430, 330), (765, 330), (1100, 330),
                 (260, 520), (920, 520)]
    for label, (x, y) in zip(labels, positions):
        round_rect(draw, (x, y, x + 310, y + 105), "#EEF6EC", "#A8B99F", radius=14, width=2)
        draw_center_text(draw, (x + 12, y, x + 298, y + 105), label, bold, fill="#172033", max_lines=3)
        round_rect(draw, (x + 24, y + 122, x + 286, y + 165), "#FFFFFF", "#9FB2C7", radius=9, width=2)
        draw_center_text(draw, (x + 24, y + 122, x + 286, y + 165), "локальный поиск", font, fill="#27364A")
    draw.text((95, 745), "В каждом блоке поиск ограничен своей базой: в «Угрозах» ищет только угрозы, в «Эксплойтах» только эксплойты и т.д.", font=font, fill="#27364A")
    return save_img(img, path)


def diagram_ontology_iz():
    path = ASSETS / "05_ontology_iz.png"
    img, draw = create_canvas(path, "Онтологии и источники знаний")
    font = pil_font(21)
    bold = pil_font(24, True)
    # ontology
    round_rect(draw, (70, 145, 750, 760), "#F8FAFC", "#CAD5E2", radius=18, width=2)
    draw.text((105, 175), "Работа с онтологиями", font=bold, fill="#0B2545")
    round_rect(draw, (105, 230, 360, 690), "#FFFFFF", "#D6DEE8", radius=12, width=2)
    draw.text((130, 260), "Дерево", font=bold, fill="#27364A")
    items = ["Информационная безопасность", "  └ Актив", "  └ Угроза", "  └ Нарушитель"]
    yy = 315
    for item in items:
        draw.text((130, yy), item, font=font, fill="#27364A")
        yy += 46
    round_rect(draw, (390, 230, 710, 690), "#FFFFFF", "#D6DEE8", radius=12, width=2)
    draw.text((420, 260), "Описание", font=bold, fill="#27364A")
    for line in wrap_draw_text(draw, "Правая область показывает описание выбранной онтологии или сущности. Снизу доступны загрузка, добавление, изменение и удаление.", font, 250):
        draw.text((420, yy - 150), line, font=font, fill="#27364A")
        yy += 34
    # dynamic db
    round_rect(draw, (850, 145, 1530, 760), "#F8FAFC", "#CAD5E2", radius=18, width=2)
    draw.text((885, 175), "Работа с ИЗ", font=bold, fill="#0B2545")
    round_rect(draw, (885, 230, 1118, 690), "#FFFFFF", "#D6DEE8", radius=12, width=2)
    draw.text((910, 260), "Базы ИЗ", font=bold, fill="#27364A")
    for i, item in enumerate(["Методики", "Регламенты", "Справки"]):
        draw.text((910, 315 + i * 46), item, font=font, fill="#27364A")
    round_rect(draw, (1148, 230, 1490, 690), "#FFFFFF", "#D6DEE8", radius=12, width=2)
    draw.text((1178, 260), "Записи и поля", font=bold, fill="#27364A")
    lines = ["Описание", "Поле 1", "Поле 2", "Пользовательские поля"]
    for i, item in enumerate(lines):
        draw.text((1178, 315 + i * 46), item, font=font, fill="#27364A")
    return save_img(img, path)


def diagram_models():
    path = ASSETS / "06_models.png"
    img, draw = create_canvas(path, "Вкладка «Работа с моделями»")
    font = pil_font(21)
    bold = pil_font(24, True)
    for x, title in [(80, "Word2Vec"), (840, "FastText")]:
        round_rect(draw, (x, 150, x + 680, 760), "#F8FAFC", "#CAD5E2", radius=18, width=2)
        draw.text((x + 38, 185), title, font=pil_font(30, True), fill="#0B2545")
        round_rect(draw, (x + 38, 250, x + 550, 302), "#FFFFFF", "#8EA3BD", radius=10, width=2)
        draw.text((x + 60, 266), "Выбор конфигурации модели", font=font, fill="#27364A")
        buttons = ["Загрузить модель", "Обучить модель", "Использовать", "Удалить", "Дообучить"]
        yy = 340
        for button in buttons:
            fill = "#2F6FDB" if button in ("Загрузить модель", "Использовать") else "#E8EEF5"
            text_fill = "#FFFFFF" if fill == "#2F6FDB" else "#172033"
            round_rect(draw, (x + 38, yy, x + 300, yy + 48), fill, "#8EA3BD", radius=9, width=2)
            draw_center_text(draw, (x + 38, yy, x + 300, yy + 48), button, font, fill=text_fill)
            yy += 64
        draw.text((x + 340, 344), "Прогресс и статус", font=bold, fill="#27364A")
        round_rect(draw, (x + 340, 390, x + 620, 432), "#FFFFFF", "#C5D0DD", radius=8, width=2)
        round_rect(draw, (x + 340, 390, x + 485, 432), "#A8D5BA", "#A8D5BA", radius=8, width=1)
        draw.text((x + 340, 466), "Работает только со", font=font, fill="#27364A")
        draw.text((x + 340, 498), "структурой Word2Vec/FastText.", font=font, fill="#27364A")
        draw.text((x + 340, 530), "GGUF-модель чата здесь", font=font, fill="#9B1C1C")
        draw.text((x + 340, 562), "не обучается.", font=font, fill="#9B1C1C")
    return save_img(img, path)


def diagram_report():
    path = ASSETS / "07_report.png"
    img, draw = create_canvas(path, "Формирование отчета")
    font = pil_font(21)
    bold = pil_font(24, True)
    boxes = [
        ((90, 230, 360, 410), "Описание", "Вставляется текстовое описание ситуации, актива, угрозы или объекта защиты."),
        ((470, 230, 740, 410), "Настройки поиска", "Выбираются фильтры: меры защиты, специалист, последствия, алгоритм и ИЗ."),
        ((850, 230, 1120, 410), "Matcher", "Проверяется сервер, готовится модель и выполняется сопоставление."),
        ((1230, 230, 1500, 410), "Отчет Word", "Выводятся коэффициенты, рекомендации и сохраняемый отчет."),
    ]
    for box, title, text in boxes:
        round_rect(draw, box, "#F8FAFC", "#CAD5E2", radius=18, width=2)
        draw.text((box[0] + 28, box[1] + 28), title, font=bold, fill="#0B2545")
        yy = box[1] + 82
        for line in wrap_draw_text(draw, text, font, box[2] - box[0] - 55):
            draw.text((box[0] + 28, yy), line, font=font, fill="#27364A")
            yy += 31
    for x in [360, 740, 1120]:
        arrow(draw, (x + 24, 320), (x + 85, 320))
    round_rect(draw, (90, 545, 1500, 715), "#F4F6F9", "#C5D0DD", radius=18, width=2)
    draw.text((130, 580), "Контрольные точки:", font=bold, fill="#0B2545")
    draw.text((130, 630), "модель выбрана и загружена; источники знаний доступны; фильтры не исключают нужные результаты; отчет сохранен после просмотра.", font=font, fill="#27364A")
    return save_img(img, path)


def diagram_chat():
    path = ASSETS / "08_chat.png"
    img, draw = create_canvas(path, "Общение с моделью")
    font = pil_font(20)
    bold = pil_font(23, True)
    round_rect(draw, (70, 130, 1530, 770), "#F8FAFC", "#CAD5E2", radius=18, width=2)
    # status
    round_rect(draw, (105, 170, 1470, 270), "#FFFFFF", "#D6DEE8", radius=14, width=2)
    draw.text((130, 198), "Статус модели", font=bold, fill="#0B2545")
    for i, b in enumerate(["Запустить", "Остановить", "Проверить", "Тест скорости", "⚙"]):
        x = 430 + i * 180
        round_rect(draw, (x, 190, x + 150, 238), "#E8EEF5" if b != "Запустить" else "#2F6FDB", "#8EA3BD", radius=9, width=2)
        draw_center_text(draw, (x, 190, x + 150, 238), b, font, fill="#FFFFFF" if b == "Запустить" else "#172033")
    # chat
    round_rect(draw, (105, 310, 1470, 640), "#FFFFFF", "#D6DEE8", radius=14, width=2)
    round_rect(draw, (150, 350, 650, 430), "#DDEBFF", "#B9CBE5", radius=18, width=2)
    draw.text((175, 374), "Вопрос пользователя", font=font, fill="#172033")
    round_rect(draw, (910, 465, 1425, 575), "#F2F4F7", "#D6DEE8", radius=18, width=2)
    draw.text((935, 490), "Потоковый ответ модели", font=font, fill="#172033")
    draw.text((935, 522), "с RAG-источниками и рекомендациями", font=font, fill="#172033")
    round_rect(draw, (105, 675, 1330, 735), "#FFFFFF", "#8EA3BD", radius=12, width=2)
    draw.text((130, 692), "Введите вопрос по инциденту информационной безопасности...", font=font, fill="#647386")
    round_rect(draw, (1350, 675, 1470, 735), "#2F6FDB", "#2F6FDB", radius=12, width=2)
    draw_center_text(draw, (1350, 675, 1470, 735), "→", pil_font(32, True), fill="#FFFFFF")
    return save_img(img, path)


def diagram_arena():
    path = ASSETS / "09_arena.png"
    img, draw = create_canvas(path, "Режим арены")
    font = pil_font(21)
    bold = pil_font(23, True)
    blocks = [
        ((95, 230, 340, 395), "Модель 1", "загрузка → ответ → остановка"),
        ((430, 230, 675, 395), "Модель 2", "загрузка → ответ → остановка"),
        ((765, 230, 1010, 395), "Модель N", "загрузка → ответ → остановка"),
        ((1115, 230, 1485, 395), "Модель-судья", "сравнивает ответы и формирует итог арены"),
    ]
    for box, title, text in blocks:
        round_rect(draw, box, "#F8FAFC", "#CAD5E2", radius=18, width=2)
        draw.text((box[0] + 24, box[1] + 24), title, font=bold, fill="#0B2545")
        yy = box[1] + 72
        for line in wrap_draw_text(draw, text, font, box[2] - box[0] - 48):
            draw.text((box[0] + 24, yy), line, font=font, fill="#27364A")
            yy += 32
    for start, end in [((340, 312), (430, 312)), ((675, 312), (765, 312)), ((1010, 312), (1115, 312))]:
        arrow(draw, start, end)
    round_rect(draw, (190, 555, 1405, 730), "#F4F6F9", "#C5D0DD", radius=18, width=2)
    draw.text((230, 590), "Зачем последовательный запуск:", font=bold, fill="#0B2545")
    draw.text((230, 640), "компьютер не держит несколько GGUF-моделей одновременно; каждая модель освобождает память перед запуском следующей.", font=font, fill="#27364A")
    return save_img(img, path)


def diagram_info():
    path = ASSETS / "10_info.png"
    img, draw = create_canvas(path, "Вкладка «Информация»")
    font = pil_font(21)
    bold = pil_font(25, True)
    for i, title in enumerate(["FastText", "Word2Vec", "Tf-Idf"]):
        x = 110 + i * 490
        round_rect(draw, (x, 190, x + 395, 650), "#F8FAFC", "#CAD5E2", radius=18, width=2)
        draw.ellipse((x + 145, 235, x + 250, 340), fill="#DDEBFF", outline="#8EA3BD", width=3)
        draw_center_text(draw, (x + 145, 235, x + 250, 340), title[:2], bold, fill="#0B2545")
        draw.text((x + 45, 385), title, font=bold, fill="#0B2545")
        for line in wrap_draw_text(draw, "Краткая справка об алгоритме и кнопки открытия приложенных PDF-материалов.", font, 315):
            draw.text((x + 45, 430), line, font=font, fill="#27364A")
            break
        round_rect(draw, (x + 45, 545, x + 350, 600), "#E8EEF5", "#8EA3BD", radius=9, width=2)
        draw_center_text(draw, (x + 45, 545, x + 350, 600), "Открыть материал", font)
    return save_img(img, path)


def diagram_threat_simple():
    path = ASSETS / "11_threat_scheme_simple.png"
    img, draw = create_canvas(path, "Схема модели угроз: логика чтения")
    font = pil_font(19)
    bold = pil_font(22, True)
    nodes = [
        ((80, 370, 270, 455), "Факторы возникновения"),
        ((355, 250, 560, 335), "Ошибки и сбои"),
        ((355, 485, 560, 570), "Физические воздействия"),
        ((650, 370, 875, 455), "Угроза, воздействующая на процесс функционирования"),
        ((980, 250, 1185, 335), "Пассивные воздействия"),
        ((980, 485, 1185, 570), "Активные воздействия"),
        ((1295, 370, 1515, 455), "Последствия для ИБ"),
    ]
    for box, text in nodes:
        round_rect(draw, box, "#EEF6EC", "#A8B99F", radius=12, width=2)
        draw_center_text(draw, box, text, bold if len(text) < 18 else font, fill="#172033", max_lines=3)
    for start, end in [
        ((270, 410), (355, 293)), ((270, 410), (355, 528)),
        ((560, 293), (650, 410)), ((560, 528), (650, 410)),
        ((875, 410), (980, 293)), ((875, 410), (980, 528)),
        ((1185, 293), (1295, 410)), ((1185, 528), (1295, 410)),
    ]:
        arrow(draw, start, end, width=3)
    draw.text((90, 720), "В программе каждый блок схемы открывает описание в правой панели; колесо мыши меняет масштаб схемы.", font=font, fill="#27364A")
    return save_img(img, path)


def create_illustrations():
    ASSETS.mkdir(parents=True, exist_ok=True)

    required = {
        "load_db": SCREENSHOTS / "01_load_database.png",
        "database": SCREENSHOTS / "02_database_viewer.png",
        "threats": SCREENSHOTS / "03_threats.png",
        "threat_scheme": SCREENSHOTS / "04_threat_scheme.png",
        "violators": SCREENSHOTS / "05_violators.png",
        "ontology": SCREENSHOTS / "06_ontologies.png",
        "models": SCREENSHOTS / "07_models.png",
        "knowledge_sources": SCREENSHOTS / "08_knowledge_sources.png",
        "report": SCREENSHOTS / "09_report.png",
        "chat": SCREENSHOTS / "10_chat.png",
        "chat_settings": SCREENSHOTS / "11_chat_settings.png",
        "chat_advanced": SCREENSHOTS / "12_chat_advanced.png",
        "arena": SCREENSHOTS / "13_arena_settings.png",
        "arena_models": SCREENSHOTS / "14_arena_models.png",
        "info": SCREENSHOTS / "15_information.png",
    }
    missing = [str(path) for path in required.values() if not path.exists()]
    if missing:
        raise FileNotFoundError(
            "Не найдены снимки текущей сборки:\n" + "\n".join(missing)
        )

    navigation_path = ASSETS / "00_actual_navigation.png"
    search_path = ASSETS / "00_actual_database_search.png"

    with Image.open(required["load_db"]) as source:
        source.crop((0, 0, source.width, min(118, source.height))).save(navigation_path)

    with Image.open(required["database"]) as source:
        left = round(source.width * 0.013)
        top = round(source.height * 0.38)
        right = round(source.width * 0.245)
        bottom = round(source.height * 0.47)
        source.crop((left, top, right, bottom)).save(search_path)

    return {
        "navigation": navigation_path,
        "db_search": search_path,
        **required,
    }


def set_run_font(run, name="Calibri", size=None, color=None, bold=None, italic=None):
    run.font.name = name
    run._element.rPr.rFonts.set(qn("w:ascii"), name)
    run._element.rPr.rFonts.set(qn("w:hAnsi"), name)
    run._element.rPr.rFonts.set(qn("w:eastAsia"), name)
    run._element.rPr.rFonts.set(qn("w:cs"), name)
    if size is not None:
        run.font.size = Pt(size)
    if color is not None:
        run.font.color.rgb = color
    if bold is not None:
        run.bold = bold
    if italic is not None:
        run.italic = italic


def set_paragraph_spacing(paragraph, before=0, after=6, line=1.25):
    fmt = paragraph.paragraph_format
    fmt.space_before = Pt(before)
    fmt.space_after = Pt(after)
    fmt.line_spacing = line


def add_page_number(paragraph):
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    run = paragraph.add_run("Страница ")
    set_run_font(run, size=9, color=GRAY)
    fld_begin = OxmlElement("w:fldChar")
    fld_begin.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = "PAGE"
    fld_sep = OxmlElement("w:fldChar")
    fld_sep.set(qn("w:fldCharType"), "separate")
    fld_text = OxmlElement("w:t")
    fld_text.text = "1"
    fld_end = OxmlElement("w:fldChar")
    fld_end.set(qn("w:fldCharType"), "end")
    r = OxmlElement("w:r")
    r.append(fld_begin)
    r.append(instr)
    r.append(fld_sep)
    r.append(fld_text)
    r.append(fld_end)
    paragraph._p.append(r)


def set_cell_shading(cell, fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_margins(cell, top=80, start=120, bottom=80, end=120):
    tc_pr = cell._tc.get_or_add_tcPr()
    mar = tc_pr.first_child_found_in("w:tcMar")
    if mar is None:
        mar = OxmlElement("w:tcMar")
        tc_pr.append(mar)
    for m, v in [("top", top), ("start", start), ("bottom", bottom), ("end", end)]:
        node = mar.find(qn(f"w:{m}"))
        if node is None:
            node = OxmlElement(f"w:{m}")
            mar.append(node)
        node.set(qn("w:w"), str(v))
        node.set(qn("w:type"), "dxa")


def set_row_cant_split(row):
    tr_pr = row._tr.get_or_add_trPr()
    if tr_pr.find(qn("w:cantSplit")) is None:
        tr_pr.append(OxmlElement("w:cantSplit"))


def set_repeat_table_header(row):
    tr_pr = row._tr.get_or_add_trPr()
    if tr_pr.find(qn("w:tblHeader")) is None:
        header = OxmlElement("w:tblHeader")
        header.set(qn("w:val"), "true")
        tr_pr.append(header)


def set_table_geometry(table, widths_dxa, indent=120):
    tbl = table._tbl
    tbl_pr = tbl.tblPr
    for child_name in ("w:tblW", "w:tblInd", "w:tblLayout"):
        existing = tbl_pr.find(qn(child_name))
        if existing is not None:
            tbl_pr.remove(existing)
    tbl_w = OxmlElement("w:tblW")
    tbl_w.set(qn("w:w"), str(sum(widths_dxa)))
    tbl_w.set(qn("w:type"), "dxa")
    tbl_pr.append(tbl_w)
    tbl_ind = OxmlElement("w:tblInd")
    tbl_ind.set(qn("w:w"), str(indent))
    tbl_ind.set(qn("w:type"), "dxa")
    tbl_pr.append(tbl_ind)
    layout = OxmlElement("w:tblLayout")
    layout.set(qn("w:type"), "fixed")
    tbl_pr.append(layout)
    grid = tbl.tblGrid
    if grid is None:
        grid = OxmlElement("w:tblGrid")
        tbl.insert(0, grid)
    for child in list(grid):
        grid.remove(child)
    for width in widths_dxa:
        col = OxmlElement("w:gridCol")
        col.set(qn("w:w"), str(width))
        grid.append(col)
    for row in table.rows:
        set_row_cant_split(row)
        for cell, width in zip(row.cells, widths_dxa):
            tc_pr = cell._tc.get_or_add_tcPr()
            tc_w = tc_pr.find(qn("w:tcW"))
            if tc_w is None:
                tc_w = OxmlElement("w:tcW")
                tc_pr.append(tc_w)
            tc_w.set(qn("w:w"), str(width))
            tc_w.set(qn("w:type"), "dxa")
            set_cell_margins(cell)


def add_para(doc, text="", style=None, bold_prefix=None, after=6):
    p = doc.add_paragraph(style=style)
    set_paragraph_spacing(p, after=after)
    if bold_prefix and text.startswith(bold_prefix):
        r1 = p.add_run(bold_prefix)
        set_run_font(r1, bold=True)
        r2 = p.add_run(text[len(bold_prefix):])
        set_run_font(r2)
    else:
        r = p.add_run(text)
        set_run_font(r)
    return p


def add_bullets(doc, items):
    for item in items:
        p = doc.add_paragraph(style="List Bullet")
        set_paragraph_spacing(p, after=4)
        r = p.add_run(item)
        set_run_font(r)


def create_numbering_instance(doc, start=1):
    numbering = doc.part.numbering_part.element
    style = doc.styles["List Number"]
    style_num_id = int(style._element.pPr.numPr.numId.val)

    abstract_num_id = None
    max_num_id = 0
    for num in numbering.findall(qn("w:num")):
        num_id = int(num.get(qn("w:numId")))
        max_num_id = max(max_num_id, num_id)
        if num_id == style_num_id:
            abstract = num.find(qn("w:abstractNumId"))
            abstract_num_id = int(abstract.get(qn("w:val")))

    if abstract_num_id is None:
        raise RuntimeError("Не удалось определить стиль нумерованного списка.")

    new_num_id = max_num_id + 1
    num = OxmlElement("w:num")
    num.set(qn("w:numId"), str(new_num_id))

    abstract = OxmlElement("w:abstractNumId")
    abstract.set(qn("w:val"), str(abstract_num_id))
    num.append(abstract)

    level_override = OxmlElement("w:lvlOverride")
    level_override.set(qn("w:ilvl"), "0")
    start_override = OxmlElement("w:startOverride")
    start_override.set(qn("w:val"), str(start))
    level_override.append(start_override)
    num.append(level_override)
    numbering.append(num)
    return new_num_id


def add_numbers(doc, items):
    num_id = create_numbering_instance(doc, start=1)
    for item in items:
        p = doc.add_paragraph(style="List Number")
        p_pr = p._p.get_or_add_pPr()
        existing_num_pr = p_pr.find(qn("w:numPr"))
        if existing_num_pr is not None:
            p_pr.remove(existing_num_pr)
        num_pr = OxmlElement("w:numPr")
        ilvl = OxmlElement("w:ilvl")
        ilvl.set(qn("w:val"), "0")
        num_id_node = OxmlElement("w:numId")
        num_id_node.set(qn("w:val"), str(num_id))
        num_pr.append(ilvl)
        num_pr.append(num_id_node)
        p_pr.append(num_pr)
        set_paragraph_spacing(p, after=4)
        r = p.add_run(item)
        set_run_font(r)


def add_note(doc, title, body):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_ALIGN_PARAGRAPH.LEFT
    set_table_geometry(table, [9360], indent=120)
    set_row_cant_split(table.rows[0])
    cell = table.cell(0, 0)
    set_cell_shading(cell, CALLOUT)
    p = cell.paragraphs[0]
    set_paragraph_spacing(p, after=2)
    r = p.add_run(title + " ")
    set_run_font(r, bold=True, color=NAVY)
    r = p.add_run(body)
    set_run_font(r)
    doc.add_paragraph()


def add_key_value_table(doc, rows, widths=(2700, 6660), header=None):
    table = doc.add_table(rows=0, cols=2)
    table.style = "Table Grid"
    if header:
        row = table.add_row().cells
        row[0].merge(row[1])
        set_cell_shading(row[0], LIGHT_BLUE)
        p = row[0].paragraphs[0]
        r = p.add_run(header)
        set_run_font(r, bold=True, color=NAVY)
    for label, value in rows:
        cells = table.add_row().cells
        cells[0].vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        cells[1].vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        set_cell_shading(cells[0], LIGHT_GRAY)
        p = cells[0].paragraphs[0]
        r = p.add_run(label)
        set_run_font(r, bold=True, color=NAVY)
        p = cells[1].paragraphs[0]
        r = p.add_run(value)
        set_run_font(r)
    set_table_geometry(table, list(widths), indent=120)
    if header:
        set_repeat_table_header(table.rows[0])
    for row in table.rows:
        for cell in row.cells:
            for p in cell.paragraphs:
                set_paragraph_spacing(p, after=2)
    doc.add_paragraph()
    return table


def add_matrix_table(doc, headers, rows, widths):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    for i, header in enumerate(headers):
        cell = table.cell(0, i)
        set_cell_shading(cell, LIGHT_BLUE)
        p = cell.paragraphs[0]
        r = p.add_run(header)
        set_run_font(r, bold=True, color=NAVY)
    for row_values in rows:
        cells = table.add_row().cells
        for i, value in enumerate(row_values):
            p = cells[i].paragraphs[0]
            r = p.add_run(value)
            set_run_font(r, size=10.5)
    set_table_geometry(table, widths, indent=120)
    set_repeat_table_header(table.rows[0])
    for row in table.rows:
        for cell in row.cells:
            for p in cell.paragraphs:
                set_paragraph_spacing(p, after=2, line=1.15)
    doc.add_paragraph()
    return table


def add_figure(doc, path, caption, width=6.15):
    if not path or not Path(path).exists():
        return
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.keep_with_next = True
    p.paragraph_format.keep_together = True
    run = p.add_run()
    inline_shape = run.add_picture(str(path), width=Inches(width))
    inline_shape._inline.docPr.set("descr", caption)
    inline_shape._inline.docPr.set("title", caption.rstrip("."))
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.keep_together = True
    set_paragraph_spacing(p, after=8)
    r = p.add_run(caption)
    set_run_font(r, size=9.5, italic=True, color=GRAY)


def add_heading(doc, text, level=1):
    p = doc.add_heading(text, level=level)
    p.paragraph_format.keep_with_next = True
    for run in p.runs:
        set_run_font(run, color=BLUE if level < 3 else DARK_BLUE, bold=True)
    return p


def configure_styles(doc):
    section = doc.sections[0]
    section.page_width = Inches(8.5)
    section.page_height = Inches(11)
    section.top_margin = Inches(1)
    section.bottom_margin = Inches(1)
    section.left_margin = Inches(1)
    section.right_margin = Inches(1)
    section.header_distance = Inches(0.492)
    section.footer_distance = Inches(0.492)
    section.different_first_page_header_footer = True

    styles = doc.styles
    normal = styles["Normal"]
    normal.font.name = "Calibri"
    normal._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
    normal._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
    normal._element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri")
    normal.font.size = Pt(11)
    normal.paragraph_format.space_after = Pt(6)
    normal.paragraph_format.line_spacing = 1.25

    for name, size, color, before, after in [
        ("Heading 1", 16, BLUE, 18, 10),
        ("Heading 2", 13, BLUE, 14, 7),
        ("Heading 3", 12, DARK_BLUE, 10, 5),
    ]:
        style = styles[name]
        style.font.name = "Calibri"
        style._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
        style._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
        style._element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri")
        style.font.size = Pt(size)
        style.font.bold = True
        style.font.color.rgb = color
        style.paragraph_format.space_before = Pt(before)
        style.paragraph_format.space_after = Pt(after)
        style.paragraph_format.line_spacing = 1.25

    for name in ("List Bullet", "List Number"):
        style = styles[name]
        style.font.name = "Calibri"
        style._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
        style._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
        style._element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri")
        style.font.size = Pt(11)
        style.paragraph_format.left_indent = Inches(0.375)
        style.paragraph_format.first_line_indent = Inches(-0.188)
        style.paragraph_format.space_after = Pt(4)
        style.paragraph_format.line_spacing = 1.25

    header = section.header.paragraphs[0]
    header.text = ""
    header.alignment = WD_ALIGN_PARAGRAPH.LEFT
    r = header.add_run("PragmaticAnalyzer | Руководство пользователя")
    set_run_font(r, size=9, color=GRAY)
    footer = section.footer.paragraphs[0]
    add_page_number(footer)


def make_doc():
    illustrations = create_illustrations()
    doc = Document()
    configure_styles(doc)
    doc.core_properties.title = "Руководство пользователя PragmaticAnalyzer"
    doc.core_properties.subject = "Руководство по работе с вкладками программы"
    doc.core_properties.author = "PragmaticAnalyzer"
    doc.core_properties.comments = "Документ создан автоматически по текущей структуре проекта."

    # Cover
    for _ in range(5):
        doc.add_paragraph()
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ")
    set_run_font(r, size=24, bold=True, color=NAVY)
    set_paragraph_spacing(p, after=14)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Модуль работы с источниками знаний ИБ v1.1.2")
    set_run_font(r, size=18, bold=True, color=BLUE)
    set_paragraph_spacing(p, after=6)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("PragmaticAnalyzer")
    set_run_font(r, size=15, color=GRAY)
    set_paragraph_spacing(p, after=42)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Подробное руководство по загрузке баз данных, поиску, онтологиям, моделям, источникам знаний, отчетам и общению с локальной RAG-моделью.")
    set_run_font(r, size=12, italic=True, color=GRAY)
    set_paragraph_spacing(p, after=120)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Версия документа: 1.0\nДата: 16 августа 2026 г.")
    set_run_font(r, size=11, color=GRAY)

    doc.add_page_break()

    add_heading(doc, "Содержание", 1)
    add_para(doc, "Руководство построено по вкладкам программы. Каждый раздел описывает назначение вкладки, порядок работы, ожидаемый результат и типовые ошибки.")
    add_matrix_table(
        doc,
        ["Раздел", "Что описано"],
        [
            ("1. Назначение и подготовка", "роль программы, состав файлов, запуск на другом компьютере"),
            ("2. Главное окно", "панель вкладок, активный раздел, общая логика навигации"),
            ("3. Загрузить БД", "подключение JSON-баз, обновление поддерживаемых источников"),
            ("4. Работа с БД", "карты баз, поиск по всем базам и поиск внутри отдельных блоков"),
            ("5. Работа с онтологиями", "загрузка, просмотр, добавление, изменение и удаление сущностей"),
            ("6. Работа с моделями", "Word2Vec и FastText, обучение, дообучение, выбор рабочей модели"),
            ("7. Работа с ИЗ", "создание пользовательских баз источников знаний и записей"),
            ("8. Сформировать отчет", "описание ситуации, фильтры, сопоставление и сохранение отчета"),
            ("9. Общение с моделью", "GGUF-модель, RAG, потоковый вывод, режимы ответа и арена"),
            ("10. Информация", "справочные материалы по алгоритмам и открытие PDF"),
            ("11. Типовые сценарии", "готовые последовательности действий пользователя"),
            ("12. Ошибки и диагностика", "частые проблемы и что проверить в первую очередь"),
        ],
        [2300, 7060],
    )

    add_heading(doc, "1. Назначение и подготовка к работе", 1)
    add_para(doc, "PragmaticAnalyzer предназначен для работы с источниками знаний по информационной безопасности: загрузки и просмотра баз данных, ведения онтологий, работы с моделями Word2Vec/FastText, формирования отчетов и общения с локальной GGUF-моделью через RAG.")
    add_note(doc, "Важно.", "Вкладка «Работа с моделями» относится к Word2Vec и FastText. GGUF-модель чата не обучается и не дообучается в этой вкладке; она подключается отдельно во вкладке «Общение с моделью».")
    add_key_value_table(doc, [
        ("Основные папки", "Database, Models, Config, Resource, NativeLlama, Translator, KnowledgeBase."),
        ("Запуск модели чата", "Через llama-server.exe из NativeLlama и GGUF-файл из Translator."),
        ("Материалы алгоритмов", "PDF-файлы для вкладки «Информация» лежат в Resource."),
        ("Перенос на другой ПК", "Нужно переносить опубликованную папку приложения вместе с базами, ресурсами, NativeLlama и выбранными моделями, кроме случаев когда модель передается отдельно."),
    ], header="Что должно быть рядом с программой")
    add_heading(doc, "Порядок первого запуска", 2)
    add_numbers(doc, [
        "Запустите исполняемый файл программы из опубликованной папки.",
        "Откройте вкладку «Загрузить БД» и убедитесь, что нужные базы отмечены как загруженные.",
        "Во вкладке «Работа с моделями» выберите и загрузите модель Word2Vec или FastText, если планируется формирование отчетов.",
        "Во вкладке «Общение с моделью» проверьте путь к llama-server.exe и GGUF-модели, затем нажмите «Запустить модель».",
        "Задайте тестовый вопрос и убедитесь, что ответ выводится в чат.",
    ])

    add_heading(doc, "2. Главное окно и навигация", 1)
    add_para(doc, "Верхняя панель содержит основные вкладки. Активная вкладка выделяется цветом, чтобы было понятно, в каком разделе находится пользователь.")
    add_figure(doc, illustrations["navigation"], "Иллюстрация 1. Фактическая панель вкладок текущей версии программы.", width=6.2)
    add_bullets(doc, [
        "«Загрузить БД» используется для подключения исходных JSON-файлов.",
        "«Работа с БД» открывает базовые справочники, схемы и поиск.",
        "«Работа с онтологиями» показывает дерево онтологий и сущностей.",
        "«Работа с моделями» управляет Word2Vec/FastText.",
        "«Работа с ИЗ» позволяет создавать пользовательские базы источников знаний.",
        "«Сформировать отчет» строит отчет по описанию и выбранным настройкам.",
        "«Общение с моделью» запускает локальную GGUF-модель и RAG-ответы.",
        "«Информация» открывает справочные материалы по алгоритмам.",
    ])

    add_heading(doc, "3. Вкладка «Загрузить БД»", 1)
    add_para(doc, "Вкладка нужна для подключения баз данных, которые затем используются в просмотре, поиске, отчетах и RAG-контексте. Для каждой строки отображается название базы, путь к файлу, дата обновления и кнопки действий.")
    add_figure(doc, illustrations["load_db"], "Иллюстрация 2. Фактический вид вкладки «Загрузить БД».", width=6.2)
    add_note(doc, "Примечание к снимкам.", "Пути к файлам, даты и выбранные модели отражают состояние компьютера, на котором сделано руководство. На другом компьютере значения путей и состав данных могут отличаться, а расположение элементов останется тем же.")
    doc.add_page_break()
    add_heading(doc, "Поддерживаемые базы", 2)
    add_matrix_table(
        doc,
        ["База", "Для чего используется"],
        [
            ("Уязвимости", "просмотр записей ФСТЭК/NVD/JVN, поиск CVE, использование в отчетах и RAG"),
            ("Угрозы", "каталог угроз, идентификаторы угроз, схема модели угроз"),
            ("Эксплойты", "сведения об эксплойтах, платформах, авторах и связанных CVE"),
            ("Меры защиты", "меры и группы мер защиты для рекомендаций и отчетов"),
            ("Риски", "последствия, технологии оценки и параметры риска"),
            ("Техники и тактики", "MITRE-подобная структура тактик и техник атак"),
            ("Нарушители", "описание групп нарушителей, целей, инструментов и атак"),
            ("Специалист по ЗИ", "сведения о специалисте, подразделениях, действиях и ПО"),
            ("ПФ СОПКА", "текущее и эталонное состояние программной функции СОПКА"),
        ],
        [2600, 6760],
    )
    add_heading(doc, "Как загрузить базу", 2)
    add_numbers(doc, [
        "Нажмите кнопку выбора файла в нужной строке.",
        "Выберите JSON-файл соответствующей базы данных.",
        "Проверьте, что строка изменила состояние на успешную загрузку.",
        "Если для базы доступно обновление, используйте кнопку обновления.",
    ])
    add_note(doc, "Если база не открывается.", "Проверьте, что файл соответствует ожидаемой структуре JSON. Нельзя подставлять произвольный файл в строку другой базы: например, файл угроз не должен загружаться как база эксплойтов.")

    add_heading(doc, "4. Вкладка «Работа с БД»", 1)
    add_para(doc, "Это основной раздел просмотра справочных баз. На стартовом экране расположена схема баз данных и кнопки перехода к отдельным блокам. Общий поиск по всем базам находится слева от схемы примерно по центру рабочей области по вертикали.")
    add_figure(doc, illustrations["database"], "Иллюстрация 3. Фактический стартовый экран вкладки «Работа с БД».", width=6.2)
    add_figure(doc, illustrations["threats"], "Иллюстрация 4. Фактическая карточка базы угроз с локальным поиском.", width=6.2)
    add_heading(doc, "Общий поиск по всем базам", 2)
    add_para(doc, "Общий поиск нужен, когда пользователь не знает, в какой именно базе находится термин. Запрос выполняется сразу по доступным базам: угрозам, нарушителям, уязвимостям, эксплойтам, мерам защиты, тактикам, рискам, специалистам и состояниям ПФ СОПКА.")
    add_figure(doc, illustrations["db_search"], "Иллюстрация 5. Строка общего поиска на фактическом экране программы.", width=5.9)
    add_numbers(doc, [
        "Введите слово или словосочетание в строку поиска.",
        "Нажмите кнопку с иконкой настроек рядом со строкой и выберите режим поиска.",
        "Нажмите «Найти». Откроется вкладка результатов.",
        "Просмотрите список найденных записей: каждая строка показывает базу, название и фрагмент совпадения.",
        "Нажмите на результат, чтобы открыть полную карточку.",
        "Используйте «Назад», чтобы вернуться к списку результатов или к карте баз.",
    ])
    add_heading(doc, "Режимы поиска", 2)
    add_key_value_table(doc, [
        ("Содержит текст", "ищет введенную строку как фрагмент поля"),
        ("Слово целиком", "сравнивает совпадение с границами слова"),
        ("Все слова", "подходит для длинного запроса: запись должна содержать все слова"),
        ("Любое слово", "возвращает запись, если найдено хотя бы одно слово из запроса"),
    ], header="Кнопка настроек поиска")
    add_heading(doc, "Поиск внутри отдельных блоков", 2)
    add_para(doc, "В каждом блоке есть свой поисковик. Он работает так же, как общий, но область поиска ограничена текущей базой. Например, во вкладке «Угрозы» поиск не показывает эксплойты или уязвимости, а во вкладке «Нарушитель» ищет только по базе нарушителей.")
    add_heading(doc, "Блок «Угрозы»", 2)
    add_bullets(doc, [
        "Показывает идентификаторы, названия, описания, источник, объект воздействия, нарушения конфиденциальности, целостности и доступности.",
        "Схема модели угроз открывается отдельным режимом просмотра.",
        "В схеме можно менять масштаб колесом мыши и нажимать на блоки для вывода описания справа.",
    ])
    add_figure(doc, illustrations["threat_scheme"], "Иллюстрация 6. Фактический режим просмотра схемы модели угроз.", width=6.2)
    add_heading(doc, "Блок «Нарушитель»", 2)
    violator = ROOT / "Images" / "ViolatorModelScheme.png"
    add_para(doc, "Блок описывает группы нарушителей, принадлежность, альтернативные имена, цели атак, известные атаки, применяемые инструменты, цели и тактики. В схеме нарушителя элементы открывают пояснения в правой области.")
    add_figure(doc, illustrations["violators"], "Иллюстрация 7. Фактическая карточка базы нарушителей.", width=6.2)
    add_figure(doc, violator, "Иллюстрация 8. Схема модели нарушителя, встроенная в проект.", width=6.25)
    add_heading(doc, "Остальные блоки баз", 2)
    add_matrix_table(
        doc,
        ["Блок", "Какие сведения смотреть"],
        [
            ("Уязвимости", "идентификатор, описание, CVSS, CWE, ПО, ОС, ссылки, меры устранения"),
            ("Эксплойты", "название, CVE, тип, платформа, автор, дата публикации"),
            ("Меры защиты", "группа, номер меры, полное название, классы защищенности"),
            ("Техники и тактики", "названия тактик, техники и описания методов атаки"),
            ("Риски ИБ", "негативные последствия, ущерб, технологии оценки риска"),
            ("Специалист по ЗИ", "организация, вышестоящие и подчиненные органы, действия и используемые меры"),
            ("ПФ СОПКА", "текущее и эталонное состояние, параметры, источник, настройки"),
        ],
        [2300, 7060],
    )

    add_heading(doc, "5. Вкладка «Работа с онтологиями»", 1)
    add_para(doc, "Вкладка предназначена для просмотра и редактирования онтологий. Слева находится дерево онтологий и сущностей, справа выводится описание выбранного элемента.")
    add_figure(doc, illustrations["ontology"], "Иллюстрация 9. Фактический вид вкладки «Работа с онтологиями».", width=6.2)
    add_heading(doc, "Просмотр онтологии", 2)
    add_numbers(doc, [
        "Нажмите «Загрузить», чтобы прочитать доступные онтологии.",
        "Выберите онтологию или сущность в дереве.",
        "Проверьте описание в правой части окна.",
    ])
    add_heading(doc, "Добавление и изменение", 2)
    add_numbers(doc, [
        "Нажмите «Добавить» или «Изменить».",
        "Заполните название и описание.",
        "Если нужно добавить сущность внутрь выбранной онтологии, отметьте соответствующий переключатель.",
        "Нажмите «Применить» и вернитесь назад.",
    ])
    add_note(doc, "Практический совет.", "Названия сущностей лучше вести единообразно: один термин в одной форме. Это уменьшает дубли и помогает поиску, отчетам и RAG-контексту.")

    add_heading(doc, "6. Вкладка «Работа с моделями»", 1)
    add_para(doc, "Эта вкладка управляет классическими моделями обработки текстов: Word2Vec и FastText. Они применяются для сопоставления текстового описания с базами и формирования отчетов.")
    add_figure(doc, illustrations["models"], "Иллюстрация 10. Фактический вид управления моделями Word2Vec и FastText.", width=6.2)
    doc.add_page_break()
    add_heading(doc, "Основные действия", 2)
    add_key_value_table(doc, [
        ("Выбор модели", "выпадающий список конфигураций Word2Vec или FastText"),
        ("Загрузить модель", "читает уже подготовленный файл модели"),
        ("Обучить модель", "создает модель по подготовленным текстовым данным"),
        ("Использовать модель", "назначает модель рабочей для поиска и отчетов"),
        ("Удалить модель", "удаляет выбранную конфигурацию/модель"),
        ("Дообучить модель", "добавляет новые данные только для Word2Vec/FastText-структуры"),
    ], header="Кнопки вкладки")
    add_note(doc, "Ограничение.", "Кнопка дообучения не должна применяться к GGUF-модели чата. GGUF — это отдельный формат локальной языковой модели, он подключается во вкладке «Общение с моделью».")
    add_heading(doc, "Типовая подготовка модели", 2)
    add_numbers(doc, [
        "Выберите нужный алгоритм: Word2Vec или FastText.",
        "Выберите конфигурацию из списка.",
        "Если модель уже обучена, нажмите «Загрузить модель».",
        "Если модель создается впервые, нажмите «Обучить модель» и дождитесь завершения прогресса.",
        "Нажмите «Использовать модель», чтобы программа брала ее для формирования отчетов.",
    ])

    add_heading(doc, "7. Вкладка «Работа с ИЗ»", 1)
    add_para(doc, "Раздел «Работа с ИЗ» нужен для создания и ведения пользовательских источников знаний. Пользователь может создавать собственные базы, задавать набор полей и добавлять записи.")
    add_figure(doc, illustrations["knowledge_sources"], "Иллюстрация 11. Фактический вид вкладки «Работа с ИЗ».", width=6.2)
    add_heading(doc, "Создание базы источников знаний", 2)
    add_numbers(doc, [
        "Нажмите «Создать БД».",
        "Укажите название базы и название индексатора.",
        "Добавьте пользовательские поля кнопкой добавления поля.",
        "Нажмите «Применить».",
    ])
    add_heading(doc, "Работа с записями", 2)
    add_numbers(doc, [
        "Выберите нужную базу в левом списке.",
        "Нажмите «Добавить запись».",
        "Заполните описание и значения пользовательских полей.",
        "Сохраните запись кнопкой «Применить».",
        "Для правки используйте «Редактировать запись», для удаления — «Удалить запись».",
    ])
    add_note(doc, "Как писать записи.", "Старайтесь добавлять в описание точные термины, нормативные формулировки и контекст применения. Это повышает качество поиска и ответов модели.")

    add_heading(doc, "8. Вкладка «Сформировать отчет»", 1)
    add_para(doc, "Вкладка формирует отчет по текстовому описанию. Пользователь вводит описание ситуации, выбирает настройки поиска, запускает сопоставление и сохраняет результат.")
    add_figure(doc, illustrations["report"], "Иллюстрация 12. Фактический вид вкладки формирования отчета.", width=6.2)
    add_heading(doc, "Порядок работы", 2)
    add_numbers(doc, [
        "Введите текстовое описание в большое поле.",
        "При необходимости откройте «Настройки поиска» и задайте фильтры.",
        "Нажмите «Сформировать отчет».",
        "Дождитесь окончания прогресса и просмотрите найденные коэффициенты/рекомендации.",
        "Выберите нужный результат в списке.",
        "Нажмите «Сохранить отчет», чтобы выгрузить результат в Word.",
    ])
    add_heading(doc, "Фильтры поиска", 2)
    add_matrix_table(
        doc,
        ["Фильтр", "Назначение"],
        [
            ("Мера защиты", "ограничивает поиск выбранными мерами"),
            ("Специалист по ЗИ", "учитывает сведения об ответственном специалисте/органе"),
            ("Негативное последствие", "фокусирует отчет на конкретном последствии"),
            ("Технология оценки риска", "выбирает методику оценки"),
            ("Алгоритм", "задает алгоритм сопоставления"),
            ("Используемые ИЗ", "ограничивает набор источников знаний"),
            ("CVSS", "добавляет фильтрацию по CVSS при работе с уязвимостями"),
        ],
        [2500, 6860],
    )

    add_heading(doc, "9. Вкладка «Общение с моделью»", 1)
    add_para(doc, "Вкладка предназначена для общения с локальной GGUF-моделью. Программа запускает llama-server, подключает модель, готовит RAG-контекст из баз знаний и выводит ответ в чат.")
    add_figure(doc, illustrations["chat"], "Иллюстрация 13. Фактический вид чата и панели управления моделью.", width=6.2)
    add_heading(doc, "Запуск и остановка модели", 2)
    add_numbers(doc, [
        "Откройте настройки кнопкой с шестеренкой.",
        "Проверьте путь к llama-server.exe в NativeLlama.",
        "Проверьте путь к GGUF-модели в Translator.",
        "Выберите профиль настроек или задайте продвинутые параметры.",
        "Нажмите «Запустить модель» и дождитесь индикатора загрузки.",
        "После работы нажмите «Остановить модель» или просто закройте программу: процесс llama-server должен завершиться.",
    ])
    add_heading(doc, "Режимы ответа", 2)
    add_key_value_table(doc, [
        ("Быстро", "короткий ответ с минимальной задержкой; подходит для первичной проверки"),
        ("Подробно", "более развернутый ответ с пояснениями и рекомендациями"),
        ("Экспертно", "максимально структурированный анализ с контекстом, ограничениями и приоритетами"),
    ], header="Выбор режима ответа")
    add_heading(doc, "Режим RAG", 2)
    add_key_value_table(doc, [
        ("Авто", "программа сама подбирает релевантные фрагменты базы знаний"),
        ("Без базы", "модель отвечает только по своему контексту и вопросу"),
        ("Только по базе", "ответ должен опираться на найденные материалы; удобно для нормативных терминов"),
    ], header="Использование базы знаний")
    add_heading(doc, "Продвинутые настройки", 2)
    add_figure(doc, illustrations["chat_settings"], "Иллюстрация 14. Основные настройки модели в фактическом интерфейсе.", width=6.2)
    add_figure(doc, illustrations["chat_advanced"], "Иллюстрация 15. Верхняя часть продвинутых числовых настроек.", width=6.2)
    doc.add_page_break()
    add_matrix_table(
        doc,
        ["Параметр", "Что регулирует"],
        [
            ("Контекст", "объем текста, который модель может учитывать за один запрос"),
            ("Max tokens", "максимальная длина ответа"),
            ("Temperature", "вариативность ответа; ниже — строже, выше — творческое"),
            ("Top P", "ограничение вероятностного выбора токенов"),
            ("Repeat penalty", "штраф за повторы"),
            ("GPU layers", "сколько слоев переносить на видеокарту"),
            ("Threads", "число потоков процессора"),
            ("Batch/Micro batch", "пакетная обработка токенов и нагрузка на память"),
            ("История", "сколько прошлых сообщений учитывать"),
            ("System prompt", "базовая инструкция модели"),
        ],
        [2300, 7060],
    )
    add_heading(doc, "Режим арены", 2)
    add_para(doc, "В режиме арены один и тот же вопрос последовательно отправляется нескольким выбранным моделям. Одновременно модели не загружаются: программа запускает первую, получает ответ, останавливает ее, затем запускает следующую. После этого модель-судья сравнивает ответы и формирует общий итог.")
    add_figure(doc, illustrations["arena"], "Иллюстрация 16. Выбор участников и модели-судьи в режиме арены.", width=6.2)
    add_figure(doc, illustrations["arena_models"], "Иллюстрация 17. Нижняя часть настроек арены и системная инструкция.", width=6.2)
    add_numbers(doc, [
        "Нажмите «Режим» и выберите «Режим арены».",
        "Добавьте модели-участники из папки Translator.",
        "Выберите модель-судью.",
        "Введите один вопрос для всех моделей.",
        "Дождитесь ответов участников и итогового отчета судьи.",
    ])
    add_note(doc, "Почему арена работает долго.", "Время складывается из загрузки каждой модели, генерации ответа каждой моделью и отдельной генерации отчета судьей. Это сделано осознанно, чтобы не перегружать память компьютера.")

    add_heading(doc, "10. Вкладка «Информация»", 1)
    add_para(doc, "Вкладка содержит справочные материалы по алгоритмам FastText, Word2Vec и Tf-Idf. Кнопки открывают привязанные PDF-файлы из папки Resource.")
    add_figure(doc, illustrations["info"], "Иллюстрация 18. Фактические справочные карточки алгоритмов.", width=6.2)
    add_heading(doc, "Как пользоваться", 2)
    add_numbers(doc, [
        "Выберите нужный алгоритм.",
        "Нажмите кнопку просмотра информации.",
        "Откройте PDF-материал или описание алгоритма.",
        "Если PDF не открылся, проверьте наличие файлов в папке Resource.",
    ])
    add_matrix_table(
        doc,
        ["Алгоритм", "Краткое назначение"],
        [
            ("FastText", "учитывает символьные n-граммы и лучше работает с редкими/морфологически измененными словами"),
            ("Word2Vec", "строит векторные представления слов по контекстам употребления"),
            ("Tf-Idf", "оценивает важность слова в документе относительно набора документов"),
        ],
        [2300, 7060],
    )

    add_heading(doc, "11. Типовые сценарии работы", 1)
    add_heading(doc, "Сценарий 1. Найти термин в базах", 2)
    add_numbers(doc, [
        "Откройте «Работа с БД».",
        "Введите термин в общий поиск.",
        "Выберите режим «Содержит текст» или «Слово целиком».",
        "Откройте карточку результата и посмотрите источник/поля записи.",
    ])
    add_heading(doc, "Сценарий 2. Получить помощь по инциденту", 2)
    add_numbers(doc, [
        "Откройте «Общение с моделью».",
        "Запустите GGUF-модель и дождитесь готовности.",
        "Включите RAG-режим «Авто» или «Только по базе».",
        "Опишите инцидент: что произошло, на каком компьютере, какие процессы/файлы замечены, какие признаки есть в журналах.",
        "Попросите модель дать краткий вывод, вероятный тип инцидента, срочные действия, что проверить и ограничения ответа.",
    ])
    add_heading(doc, "Сценарий 3. Сформировать отчет", 2)
    add_numbers(doc, [
        "Загрузите базы и рабочую модель Word2Vec/FastText.",
        "Откройте «Сформировать отчет».",
        "Введите описание ситуации.",
        "Настройте фильтры.",
        "Сформируйте и сохраните отчет.",
    ])
    add_heading(doc, "Сценарий 4. Добавить собственный источник знаний", 2)
    add_numbers(doc, [
        "Откройте «Работа с ИЗ».",
        "Создайте новую базу с нужными полями.",
        "Добавьте записи с описаниями и ключевыми терминами.",
        "Проверьте поиск по добавленным данным.",
    ])

    add_heading(doc, "12. Типовые проблемы и диагностика", 1)
    add_matrix_table(
        doc,
        ["Проблема", "Что проверить"],
        [
            ("Не открывается PDF во вкладке «Информация»", "папка Resource перенесена вместе с приложением; файл не переименован и не удален"),
            ("Не загружается база", "выбран правильный JSON-файл; структура файла соответствует типу базы; путь доступен"),
            ("В поиске мало результатов", "изменить режим поиска на «Любое слово» или проверить, загружена ли нужная база"),
            ("Ответ модели слишком короткий", "выбрать режим «Подробно» или «Экспертно», увеличить Max tokens, проверить system prompt"),
            ("Модель долго запускается", "GGUF-файл большой; проверьте GPU layers, threads, доступную ОЗУ и профиль «Под железо»"),
            ("llama-server остается в процессах", "использовать кнопку «Остановить модель»; при закрытии программа должна завершать связанный процесс"),
            ("Дообучение выдает ошибку", "проверить, что выбрана Word2Vec/FastText-модель, а не GGUF-модель чата"),
            ("Отчет не формируется", "загружена рабочая модель, matcher доступен, базы подключены, фильтры не слишком узкие"),
        ],
        [3000, 6360],
    )
    add_heading(doc, "Контрольный список перед передачей на другой ПК", 2)
    add_bullets(doc, [
        "Опубликованная папка содержит exe-файл и все DLL-библиотеки.",
        "Рядом находятся папки Database, Config, Resource, NativeLlama, Translator, KnowledgeBase и Models.",
        "GGUF-модель скопирована в Translator или указан актуальный путь к ней.",
        "llama-server.exe находится в NativeLlama.",
        "Если модель не передается через GitHub из-за размера, пользователь получает ее отдельно.",
        "На компьютере достаточно ОЗУ и доступно место для модели и баз данных.",
    ])

    doc.add_page_break()
    add_heading(doc, "Приложение. Краткая памятка пользователя", 1)
    add_matrix_table(
        doc,
        ["Нужно сделать", "Куда идти"],
        [
            ("Подключить или обновить JSON-базы", "Загрузить БД"),
            ("Найти термин или запись", "Работа с БД → общий или локальный поиск"),
            ("Посмотреть схему угроз/нарушителя", "Работа с БД → Угрозы или Нарушитель → Схема"),
            ("Добавить термин в онтологию", "Работа с онтологиями"),
            ("Обучить Word2Vec/FastText", "Работа с моделями"),
            ("Создать пользовательскую базу знаний", "Работа с ИЗ"),
            ("Сделать Word-отчет", "Сформировать отчет"),
            ("Спросить локальную модель", "Общение с моделью"),
            ("Открыть материалы по алгоритмам", "Информация"),
        ],
        [2800, 6560],
    )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    doc.save(OUT)
    return OUT


if __name__ == "__main__":
    out = make_doc()
    print(out)
