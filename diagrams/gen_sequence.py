#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch

fig, ax = plt.subplots(figsize=(22, 18))
ax.set_xlim(0, 22)
ax.set_ylim(0, 18)
ax.axis('off')
fig.patch.set_facecolor('#FAFAFA')
ax.set_facecolor('#FAFAFA')

# Participants
participants = [
    ':Пользователь',
    ':LoginWindow',
    ':LoginViewModel',
    ':FitTimeDbContext',
    ':BCrypt',
    ':ICurrentUserService',
    ':MainWindow',
]
n = len(participants)
xs = [1.2 + i * 2.9 for i in range(n)]
top_y = 17.2
lifeline_bottom = 0.5

colors = ['#E3F2FD', '#E8F5E9', '#FFF3E0', '#FCE4EC', '#F3E5F5', '#E0F2F1', '#FFF8E1']
border_colors = ['#1565C0', '#2E7D32', '#E65100', '#C62828', '#6A1B9A', '#00695C', '#F57F17']

# Draw participant boxes
for i, (p, x) in enumerate(zip(participants, xs)):
    box = FancyBboxPatch((x - 1.1, top_y - 0.4), 2.2, 0.7,
        boxstyle="round,pad=0.05", linewidth=1.5,
        edgecolor=border_colors[i], facecolor=colors[i], zorder=3)
    ax.add_patch(box)
    ax.text(x, top_y, p, ha='center', va='center', fontsize=7.5,
        fontweight='bold', color='#1A237E', zorder=4, wrap=True,
        multialignment='center')
    # Lifeline
    ax.plot([x, x], [top_y - 0.4, lifeline_bottom], '--', color='#9E9E9E', lw=1.0, zorder=1)

# Helper to draw arrow
def arrow(ax, x1, y, x2, label, dashed=False, color='#212121', return_arrow=False):
    style = 'dashed' if dashed else 'solid'
    aw = '->' if not return_arrow else '->'
    ax.annotate('', xy=(x2, y), xytext=(x1, y),
        arrowprops=dict(arrowstyle='->', color=color, lw=1.3, linestyle=style))
    mx = (x1 + x2) / 2
    ax.text(mx, y + 0.1, label, ha='center', va='bottom', fontsize=7.2,
        color=color, style='italic' if dashed else 'normal',
        multialignment='center')

# Self arrow
def self_arrow(ax, x, y, label, color='#212121'):
    ax.annotate('', xy=(x, y - 0.3), xytext=(x, y),
        arrowprops=dict(arrowstyle='->', color=color, lw=1.3,
            connectionstyle='arc3,rad=-0.8'))
    ax.text(x + 0.3, y - 0.15, label, ha='left', va='center', fontsize=7.2, color=color)

# Alt block
def alt_block(ax, y_top, y_bottom, condition, x_left=0.3, x_right=21.7, color='#FF6F00'):
    rect = FancyBboxPatch((x_left, y_bottom), x_right - x_left, y_top - y_bottom,
        boxstyle="square,pad=0", linewidth=1.5,
        edgecolor=color, facecolor='#FFF8E1', alpha=0.35, zorder=0)
    ax.add_patch(rect)
    # alt label
    label_box = FancyBboxPatch((x_left, y_top - 0.32), 0.9, 0.32,
        boxstyle="square,pad=0", linewidth=1,
        edgecolor=color, facecolor=color, alpha=0.8, zorder=2)
    ax.add_patch(label_box)
    ax.text(x_left + 0.45, y_top - 0.16, 'alt', ha='center', va='center',
        fontsize=7.5, fontweight='bold', color='white', zorder=3)
    ax.text(x_left + 1.1, y_top - 0.16, f'[{condition}]', ha='left', va='center',
        fontsize=7.2, color=color, zorder=3, style='italic')
    # dashed divider
    ax.plot([x_left, x_right], [y_top - 0.32, y_top - 0.32], '--', color=color, lw=0.8, zorder=1)

# ---- Messages sequence ----
y = 16.3

# 1. Пользователь → LoginWindow
arrow(ax, xs[0], y, xs[1], 'Ввод логина и пароля', color='#1A237E')
y -= 0.55

# 2. LoginWindow → LoginViewModel
arrow(ax, xs[1], y, xs[2], 'LoginCommand(login, password)', color='#1A237E')
y -= 0.55

# 3. Self check
self_arrow(ax, xs[2], y, 'Проверка: поля не пустые', color='#E65100')
y -= 0.55

# Alt: поля пустые
alt_block(ax, y + 0.1, y - 0.5, 'Поля пустые')
arrow(ax, xs[2], y - 0.15, xs[1], 'Показать ошибку: "Заполните все поля"', color='#C62828')
y -= 0.7

# 4. LoginViewModel → FitTimeDbContext
arrow(ax, xs[2], y, xs[3], 'Users.FirstOrDefault(u => u.Login == login)', color='#1A237E')
y -= 0.55

# 5. FitTimeDbContext → LoginViewModel (return)
arrow(ax, xs[3], y, xs[2], 'return user / null', dashed=True, color='#388E3C')
y -= 0.55

# Alt: пользователь не найден
alt_block(ax, y + 0.1, y - 0.5, 'Пользователь не найден')
arrow(ax, xs[2], y - 0.15, xs[1], 'Показать ошибку: "Неверный логин или пароль"', color='#C62828')
y -= 0.7

# 6. Self check: IsActive, FailedAttempts
self_arrow(ax, xs[2], y, 'user.IsActive && user.FailedAttempts < 5', color='#E65100')
y -= 0.55

# Alt: аккаунт заблокирован
alt_block(ax, y + 0.1, y - 0.5, 'Аккаунт заблокирован')
arrow(ax, xs[2], y - 0.15, xs[1], 'Показать ошибку: "Аккаунт заблокирован"', color='#C62828')
y -= 0.7

# 7. LoginViewModel → BCrypt
arrow(ax, xs[2], y, xs[4], 'BCrypt.Verify(password, user.PasswordHash)', color='#1A237E')
y -= 0.55

# 8. BCrypt → LoginViewModel (return)
arrow(ax, xs[4], y, xs[2], 'return true / false', dashed=True, color='#388E3C')
y -= 0.55

# Alt: пароль неверный
alt_block(ax, y + 0.1, y - 0.75, 'Пароль неверный')
arrow(ax, xs[2], y - 0.15, xs[3], 'user.FailedAttempts++; SaveChanges()', color='#C62828')
y -= 0.38
arrow(ax, xs[2], y - 0.15, xs[1], 'Показать ошибку + счётчик попыток', color='#C62828')
y -= 0.75

# 9. Reset attempts
arrow(ax, xs[2], y, xs[3],
    'FailedAttempts=0; LastLoginAt=Now(); SaveChanges()', color='#1A237E')
y -= 0.55

# 10. SetCurrentUser
arrow(ax, xs[2], y, xs[5], 'SetCurrentUser(user)', color='#1A237E')
y -= 0.55

# 11. Show MainWindow
arrow(ax, xs[2], y, xs[6], 'new MainWindow(); Show()', color='#1A237E')
y -= 0.55

# 12. Close LoginWindow
arrow(ax, xs[2], y, xs[1], 'Close()', color='#1A237E')

plt.title('Sequence Diagram — Авторизация в системе FitTime',
    fontsize=13, fontweight='bold', pad=10)
plt.tight_layout()
plt.savefig('sequence_auth.png', dpi=150, bbox_inches='tight', facecolor='#FAFAFA')
print("sequence_auth.png saved")
