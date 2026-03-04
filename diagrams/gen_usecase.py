#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, Ellipse, FancyArrowPatch
import numpy as np

fig, ax = plt.subplots(1, 1, figsize=(20, 14))
ax.set_xlim(0, 20)
ax.set_ylim(0, 14)
ax.axis('off')
ax.set_facecolor('#FAFAFA')
fig.patch.set_facecolor('#FAFAFA')

# --- System boundary box ---
sys_box = FancyBboxPatch((2.8, 0.5), 13.4, 13.0,
    boxstyle="round,pad=0.1", linewidth=2, edgecolor='#1565C0', facecolor='#E3F2FD', zorder=0)
ax.add_patch(sys_box)
ax.text(9.5, 13.25, 'ИС FitTime', ha='center', va='center',
    fontsize=15, fontweight='bold', color='#1565C0',
    fontfamily='DejaVu Sans')

# ---- Helper: draw actor (stick figure) ----
def draw_actor(ax, cx, cy, label, color='#1A237E'):
    # Head
    head = plt.Circle((cx, cy + 0.55), 0.22, color=color, zorder=3)
    ax.add_patch(head)
    # Body
    ax.plot([cx, cx], [cy + 0.33, cy - 0.1], color=color, lw=2, zorder=3)
    # Arms
    ax.plot([cx - 0.35, cx + 0.35], [cy + 0.15, cy + 0.15], color=color, lw=2, zorder=3)
    # Legs
    ax.plot([cx, cx - 0.28], [cy - 0.1, cy - 0.55], color=color, lw=2, zorder=3)
    ax.plot([cx, cx + 0.28], [cy - 0.1, cy - 0.55], color=color, lw=2, zorder=3)
    ax.text(cx, cy - 0.82, label, ha='center', va='top', fontsize=9,
        fontweight='bold', color=color, wrap=True,
        multialignment='center')

# ---- Helper: draw use case ellipse ----
def draw_usecase(ax, cx, cy, text, w=2.5, h=0.55, fcolor='#BBDEFB', ecolor='#1565C0'):
    ell = Ellipse((cx, cy), w, h, facecolor=fcolor, edgecolor=ecolor, linewidth=1.5, zorder=2)
    ax.add_patch(ell)
    # wrap long text
    lines = text.split('\n')
    if len(lines) == 1 and len(text) > 30:
        words = text.split(' ')
        mid = len(words)//2
        text = ' '.join(words[:mid]) + '\n' + ' '.join(words[mid:])
    ax.text(cx, cy, text, ha='center', va='center', fontsize=7.5,
        color='#0D1B4E', zorder=3, multialignment='center')

# ---- Actors ----
draw_actor(ax, 1.5, 10.0, 'Администратор', color='#1A237E')
draw_actor(ax, 1.5, 5.5,  'Менеджер',      color='#004D40')
draw_actor(ax, 18.5, 7.5, 'Тренер',        color='#4A148C')

# ---- Use cases: Common (all three actors) ----
draw_usecase(ax, 9.5, 12.3, 'Авторизация в системе')
draw_usecase(ax, 9.5, 11.3, 'Просмотр панели управления (Dashboard)')

# ---- Use cases: Admin ----
uc_admin = [
    (5.5, 10.0, 'Управление пользователями'),
    (5.5,  9.0, 'Сброс пароля пользователя'),
    (5.5,  8.0, 'Управление тренерами'),
    (5.5,  7.0, 'Управление типами абонементов'),
    (5.5,  6.0, 'Просмотр всех отчётов'),
    (5.5,  5.0, 'Экспорт отчётов в CSV'),
]
for (cx, cy, txt) in uc_admin:
    draw_usecase(ax, cx, cy, txt, fcolor='#E8EAF6', ecolor='#3949AB')

# ---- Use cases: Manager ----
uc_manager = [
    (11.5, 10.2, 'Управление клиентами'),
    (11.5,  9.2, 'Оформление абонемента клиенту'),
    (11.5,  8.2, 'Управление расписанием'),
    (11.5,  7.2, 'Отметка посещаемости'),
    (11.5,  6.2, 'Просмотр отчётов'),
    (11.5,  5.2, 'Экспорт отчётов в CSV '),
]
for (cx, cy, txt) in uc_manager:
    draw_usecase(ax, cx, cy, txt, fcolor='#E8F5E9', ecolor='#2E7D32')

# ---- Use cases: Trainer ----
uc_trainer = [
    (14.5, 8.5, 'Просмотр своего расписания'),
    (14.5, 7.3, 'Просмотр списка\nучастников занятия'),
]
for (cx, cy, txt) in uc_trainer:
    draw_usecase(ax, cx, cy, txt, fcolor='#F3E5F5', ecolor='#7B1FA2')

# ---- Lines: actors to use cases ----
def line(ax, x1, y1, x2, y2, color='#555', style='-'):
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
        arrowprops=dict(arrowstyle='-', color=color, lw=1.2,
            linestyle=style), zorder=1)

# Admin → common
line(ax, 1.5, 10.0, 8.2, 12.3, '#3949AB')
line(ax, 1.5, 10.0, 8.2, 11.3, '#3949AB')
# Admin → own UCs
for (cx, cy, _) in uc_admin:
    line(ax, 1.5, 10.0, cx - 1.25, cy, '#3949AB')

# Manager → common
line(ax, 1.5, 5.5, 8.2, 12.3, '#2E7D32')
line(ax, 1.5, 5.5, 8.2, 11.3, '#2E7D32')
# Manager → own UCs
for (cx, cy, _) in uc_manager:
    line(ax, 1.5, 5.5, cx - 1.25, cy, '#2E7D32')

# Trainer → common
line(ax, 18.5, 7.5, 10.8, 12.3, '#7B1FA2')
line(ax, 18.5, 7.5, 10.8, 11.3, '#7B1FA2')
# Trainer → own UCs
for (cx, cy, _) in uc_trainer:
    line(ax, 18.5, 7.5, cx + 1.25, cy, '#7B1FA2')

# ---- Include / Extend relations ----
def dashed_arrow(ax, x1, y1, x2, y2, label, color='#555'):
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
        arrowprops=dict(arrowstyle='->', color=color, lw=1.2,
            linestyle='dashed'), zorder=4)
    mx, my = (x1+x2)/2, (y1+y2)/2
    ax.text(mx, my+0.12, label, ha='center', va='bottom', fontsize=7,
        color=color, style='italic')

# «Управление клиентами» --<<include>>--> «Оформление абонемента клиенту»
dashed_arrow(ax, 11.5, 10.2, 11.5, 9.47, '<<include>>', '#C62828')
# «Управление расписанием» --<<include>>--> «Просмотр списка участников»
dashed_arrow(ax, 12.75, 8.2, 13.2, 7.9, '<<include>>', '#C62828')
# «Просмотр отчётов» --<<extend>>--> «Экспорт отчётов в CSV»
dashed_arrow(ax, 11.5, 6.2, 11.5, 5.47, '<<extend>>', '#E65100')

plt.title('Use Case Diagram — ИС FitTime', fontsize=14, fontweight='bold', pad=8)
plt.tight_layout()
plt.savefig('use_case.png', dpi=150, bbox_inches='tight', facecolor='#FAFAFA')
print("use_case.png saved")
