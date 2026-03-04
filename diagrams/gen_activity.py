#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import numpy as np

fig, ax = plt.subplots(figsize=(10, 20))
ax.set_xlim(0, 10)
ax.set_ylim(0, 20)
ax.axis('off')
fig.patch.set_facecolor('#FAFAFA')
ax.set_facecolor('#FAFAFA')

cx = 5.0   # center x

def action(ax, x, y, text, w=4.8, h=0.55, color='#E3F2FD', border='#1565C0'):
    box = FancyBboxPatch((x - w/2, y - h/2), w, h,
        boxstyle="round,pad=0.12", linewidth=1.5,
        edgecolor=border, facecolor=color, zorder=2)
    ax.add_patch(box)
    ax.text(x, y, text, ha='center', va='center', fontsize=8.2,
        color='#1A237E', zorder=3, multialignment='center', wrap=True)
    return y - h/2  # bottom y

def arrow_down(ax, x, y_from, y_to, color='#555', label=None, offset_x=0):
    ax.annotate('', xy=(x + offset_x, y_to + 0.02), xytext=(x + offset_x, y_from - 0.02),
        arrowprops=dict(arrowstyle='->', color=color, lw=1.3))
    if label:
        ax.text(x + offset_x + 0.1, (y_from + y_to) / 2, label,
            ha='left', va='center', fontsize=7.5, color=color)

def diamond(ax, x, y, text, size=0.45, color='#FFF9C4', border='#F57F17'):
    # Draw rotated square
    pts = np.array([[x, y + size], [x + size, y], [x, y - size], [x - size, y]])
    diamond_shape = plt.Polygon(pts, closed=True, facecolor=color, edgecolor=border, lw=1.8, zorder=2)
    ax.add_patch(diamond_shape)
    ax.text(x, y, text, ha='center', va='center', fontsize=7.5,
        color='#5D4037', zorder=3, multialignment='center')

def start_node(ax, x, y, r=0.2):
    c = plt.Circle((x, y), r, color='#1A237E', zorder=3)
    ax.add_patch(c)

def end_node(ax, x, y, r=0.22):
    outer = plt.Circle((x, y), r, facecolor='white', edgecolor='#1A237E', lw=2, zorder=3)
    inner = plt.Circle((x, y), r * 0.6, color='#1A237E', zorder=4)
    ax.add_patch(outer)
    ax.add_patch(inner)

# --- Build diagram top-to-bottom ---
gap = 0.25  # gap between elements

y = 19.5
start_node(ax, cx, y)
y -= 0.2

arrow_down(ax, cx, y, y - gap)
y -= gap

# 1
y_top = y
y_bot = action(ax, cx, y_top, 'Менеджер открывает раздел «Клиенты»')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 2
y_bot = action(ax, cx, y, 'Выбор клиента из списка')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 3
y_bot = action(ax, cx, y, 'Нажатие кнопки «Оформить абонемент»')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 4
y_bot = action(ax, cx, y, 'Открытие диалога оформления абонемента')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 5
y_bot = action(ax, cx, y, 'Выбор типа абонемента из выпадающего списка')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 6
y_bot = action(ax, cx, y, 'Система автозаполняет: цена, длительность,\nкол-во посещений, дата окончания', h=0.7)
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# Decision: Безлимит?
d_y = y - 0.45
diamond(ax, cx, d_y, 'Тип абонемента —\nБезлимит?', size=0.55)
y_after_diamond = d_y

# YES branch (left)
left_x = cx - 2.8
ax.annotate('', xy=(left_x, d_y), xytext=(cx - 0.55, d_y),
    arrowprops=dict(arrowstyle='->', color='#2E7D32', lw=1.2))
ax.text(cx - 1.6, d_y + 0.1, '[Да]', ha='center', va='bottom', fontsize=8, color='#2E7D32', fontweight='bold')
# box on left
yes_y = d_y
box = FancyBboxPatch((left_x - 2.0, yes_y - 0.28), 4.0, 0.55,
    boxstyle="round,pad=0.1", linewidth=1.2,
    edgecolor='#2E7D32', facecolor='#E8F5E9', zorder=2)
ax.add_patch(box)
ax.text(left_x, yes_y, 'Поле «кол-во посещений»\nскрыто, IsUnlimited = true', ha='center', va='center',
    fontsize=7.2, color='#1B5E20', zorder=3, multialignment='center')

# NO branch (right)
right_x = cx + 2.8
ax.annotate('', xy=(right_x, d_y), xytext=(cx + 0.55, d_y),
    arrowprops=dict(arrowstyle='->', color='#C62828', lw=1.2))
ax.text(cx + 1.6, d_y + 0.1, '[Нет]', ha='center', va='bottom', fontsize=8, color='#C62828', fontweight='bold')
box2 = FancyBboxPatch((right_x - 2.0, yes_y - 0.28), 4.0, 0.55,
    boxstyle="round,pad=0.1", linewidth=1.2,
    edgecolor='#C62828', facecolor='#FFEBEE', zorder=2)
ax.add_patch(box2)
ax.text(right_x, yes_y, 'Отображается поле\n«кол-во посещений»', ha='center', va='center',
    fontsize=7.2, color='#B71C1C', zorder=3, multialignment='center')

# Merge paths back to center
merge_y = d_y - 0.65
ax.plot([left_x, left_x], [d_y - 0.28, merge_y], color='#2E7D32', lw=1.2)
ax.plot([right_x, right_x], [d_y - 0.28, merge_y], color='#C62828', lw=1.2)
ax.plot([left_x, right_x], [merge_y, merge_y], color='#555', lw=1.2)
arrow_down(ax, cx, merge_y, merge_y - gap)

y = merge_y - gap

# 8 cont.
y_bot = action(ax, cx, y, 'Менеджер указывает дату начала')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 9
y_bot = action(ax, cx, y, 'Система рассчитывает дату окончания\n= дата начала + duration_days', h=0.7)
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 10
y_bot = action(ax, cx, y, 'Менеджер нажимает «Сохранить»')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# Decision: Валидация
d2_y = y - 0.45
diamond(ax, cx, d2_y, 'Все поля заполнены\nкорректно?', size=0.55)

# NO branch: back loop
loop_x = cx + 3.5
ax.annotate('', xy=(loop_x, d2_y), xytext=(cx + 0.55, d2_y),
    arrowprops=dict(arrowstyle='->', color='#C62828', lw=1.2))
ax.text(cx + 1.6, d2_y + 0.1, '[Нет]', ha='center', va='bottom', fontsize=8, color='#C62828', fontweight='bold')
box_err = FancyBboxPatch((loop_x - 1.5, d2_y - 0.28), 3.0, 0.55,
    boxstyle="round,pad=0.1", linewidth=1.2,
    edgecolor='#C62828', facecolor='#FFEBEE', zorder=2)
ax.add_patch(box_err)
ax.text(loop_x, d2_y, 'Показать ошибку\nвалидации', ha='center', va='center',
    fontsize=7.5, color='#B71C1C', zorder=3, multialignment='center')
# loop back arrow
loop_back_y = d2_y - 0.28  # bottom of error box
target_y = y + gap  # goes back to "Менеджер указывает дату начала"
ax.plot([loop_x, loop_x + 0.9], [loop_back_y, loop_back_y], color='#C62828', lw=1.2)
ax.plot([loop_x + 0.9, loop_x + 0.9], [loop_back_y, target_y + 0.27], color='#C62828', lw=1.2)
ax.annotate('', xy=(cx + 2.4, target_y + 0.27), xytext=(loop_x + 0.9, target_y + 0.27),
    arrowprops=dict(arrowstyle='->', color='#C62828', lw=1.2))
ax.text(loop_x + 0.95, (loop_back_y + target_y) / 2, 'возврат', ha='left', va='center',
    fontsize=6.5, color='#C62828', style='italic')

# YES branch (down)
ax.text(cx - 1.0, d2_y - 0.7, '[Да]', ha='center', va='top', fontsize=8, color='#2E7D32', fontweight='bold')
arrow_down(ax, cx, d2_y - 0.55, d2_y - 0.55 - gap)
y = d2_y - 0.55 - gap

# 11
y_bot = action(ax, cx, y,
    'Создание записи Membership в БД\n(client_id, membership_type_id, start/end_date, price, sold_by_user_id)', h=0.7)
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 12
y_bot = action(ax, cx, y, 'Обновление списка абонементов клиента')
y = y_bot

arrow_down(ax, cx, y, y - gap)
y -= gap

# 13
y_bot = action(ax, cx, y, 'Закрытие диалогового окна')
y = y_bot

arrow_down(ax, cx, y, y - gap * 0.8)
y -= gap * 0.8

# End
end_node(ax, cx, y - 0.22)

plt.title('Activity Diagram — Оформление абонемента', fontsize=13, fontweight='bold', pad=10)
plt.tight_layout()
plt.savefig('activity_membership.png', dpi=150, bbox_inches='tight', facecolor='#FAFAFA')
print("activity_membership.png saved")
