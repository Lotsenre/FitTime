#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch

fig, ax = plt.subplots(figsize=(24, 18))
ax.set_xlim(0, 24)
ax.set_ylim(0, 18)
ax.axis('off')
fig.patch.set_facecolor('#F8F9FA')
ax.set_facecolor('#F8F9FA')

HEADER_COLOR = '#1565C0'
HEADER_TEXT = 'white'
CELL_COLOR = '#E3F2FD'
BORDER_COLOR = '#1565C0'
PK_COLOR = '#F9A825'
FK_COLOR = '#EF6C00'
ROW_ALT = '#DCEDC8'

def table(ax, x, y, name, fields, width=3.5):
    """Draw an ER table. fields: list of (name, type, note, is_pk, is_fk)"""
    row_h = 0.30
    header_h = 0.38
    total_h = header_h + len(fields) * row_h
    # Header
    hbox = FancyBboxPatch((x, y - header_h), width, header_h,
        boxstyle="round,pad=0.04", lw=2,
        edgecolor=BORDER_COLOR, facecolor=HEADER_COLOR, zorder=2)
    ax.add_patch(hbox)
    ax.text(x + width / 2, y - header_h / 2, name, ha='center', va='center',
        fontsize=9.5, fontweight='bold', color=HEADER_TEXT, zorder=3)

    # Fields
    for i, (fname, ftype, note, is_pk, is_fk) in enumerate(fields):
        fy = y - header_h - (i + 1) * row_h
        alt = i % 2 == 1
        fc = '#EEF2FF' if alt else CELL_COLOR
        if is_pk:
            fc = '#FFF9C4'
        elif is_fk:
            fc = '#FFF3E0'
        rbox = FancyBboxPatch((x, fy), width, row_h,
            boxstyle="square,pad=0", lw=0.8,
            edgecolor='#90A4AE', facecolor=fc, zorder=2)
        ax.add_patch(rbox)
        # icon
        icon = '🔑 ' if is_pk else ('→ ' if is_fk else '   ')
        icon_color = PK_COLOR if is_pk else (FK_COLOR if is_fk else '#555')
        ax.text(x + 0.07, fy + row_h / 2, icon, ha='left', va='center',
            fontsize=7.5, color=icon_color, zorder=3)
        ax.text(x + 0.35, fy + row_h / 2, fname, ha='left', va='center',
            fontsize=7.5, color='#1A237E', zorder=3)
        ax.text(x + width - 0.07, fy + row_h / 2, ftype, ha='right', va='center',
            fontsize=6.5, color='#546E7A', zorder=3)

    # outer border
    full = FancyBboxPatch((x, y - total_h), width, total_h,
        boxstyle="square,pad=0", lw=1.5,
        edgecolor=BORDER_COLOR, facecolor='none', zorder=4)
    ax.add_patch(full)

    return {'x': x, 'y': y, 'w': width, 'h': total_h,
            'cx': x + width / 2, 'cy': y - total_h / 2,
            'top': y, 'bottom': y - total_h,
            'left': x, 'right': x + width,
            'mid_y': y - total_h / 2}

def conn(ax, t1, side1, t2, side2, label='1:N', color='#333'):
    """Draw relationship line between two tables."""
    # pick attachment points
    def pt(t, side):
        if side == 'right':
            return t['right'], t['mid_y']
        elif side == 'left':
            return t['left'], t['mid_y']
        elif side == 'top':
            return t['cx'], t['top']
        elif side == 'bottom':
            return t['cx'], t['bottom']
    x1, y1 = pt(t1, side1)
    x2, y2 = pt(t2, side2)
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
        arrowprops=dict(arrowstyle='->', color=color, lw=1.3,
            connectionstyle='arc3,rad=0.0'))
    mx, my = (x1 + x2) / 2, (y1 + y2) / 2
    ax.text(mx, my + 0.12, label, ha='center', va='bottom',
        fontsize=7, color=color, fontweight='bold')

# ===================== TABLE DEFINITIONS =====================
# Layout positions (x, y = top-left top)
#   roles        @ (0.5, 17.5)
#   users        @ (4.5, 17.5)
#   clients      @ (0.5, 12.0)
#   membership_types @ (8.5, 17.5)
#   memberships  @ (4.5, 11.0)
#   class_types  @ (12.5, 17.5)
#   classes      @ (12.5, 12.0)
#   class_enrollments @ (8.5, 7.5)
#   attendance   @ (16.5, 9.5)

W = 3.4  # table width

roles = table(ax, 0.5, 17.8, 'ROLES', [
    ('id', 'SERIAL', 'PK', True, False),
    ('name', 'VARCHAR(50)', 'NOT NULL', False, False),
    ('description', 'TEXT', '', False, False),
], width=W)

users = table(ax, 4.5, 17.8, 'USERS', [
    ('id', 'SERIAL', 'PK', True, False),
    ('login', 'VARCHAR(50)', 'UNIQUE', False, False),
    ('password_hash', 'VARCHAR(255)', '', False, False),
    ('role_id', 'INT', 'FK→roles', False, True),
    ('first_name', 'VARCHAR(100)', '', False, False),
    ('last_name', 'VARCHAR(100)', '', False, False),
    ('is_active', 'BOOLEAN', 'DEFAULT true', False, False),
    ('failed_attempts', 'SMALLINT', 'DEFAULT 0', False, False),
    ('last_login_at', 'TIMESTAMP', '', False, False),
    ('created_at', 'TIMESTAMP', 'DEFAULT NOW()', False, False),
], width=W + 0.4)

clients = table(ax, 0.5, 11.5, 'CLIENTS', [
    ('id', 'SERIAL', 'PK', True, False),
    ('first_name', 'VARCHAR(100)', '', False, False),
    ('last_name', 'VARCHAR(100)', '', False, False),
    ('birth_date', 'DATE', '', False, False),
    ('phone', 'VARCHAR(20)', '', False, False),
    ('email', 'VARCHAR(100)', '', False, False),
    ('is_active', 'BOOLEAN', 'DEFAULT true', False, False),
    ('created_at', 'TIMESTAMP', '', False, False),
], width=W)

mem_types = table(ax, 8.8, 17.8, 'MEMBERSHIP_TYPES', [
    ('id', 'SERIAL', 'PK', True, False),
    ('name', 'VARCHAR(100)', '', False, False),
    ('duration_days', 'INT', 'NOT NULL', False, False),
    ('is_unlimited', 'BOOLEAN', 'DEFAULT false', False, False),
    ('visit_count', 'INT', 'DEFAULT 0', False, False),
    ('price', 'DECIMAL(10,2)', 'NOT NULL', False, False),
    ('is_archived', 'BOOLEAN', 'DEFAULT false', False, False),
], width=W + 0.5)

memberships = table(ax, 4.5, 10.5, 'MEMBERSHIPS', [
    ('id', 'SERIAL', 'PK', True, False),
    ('client_id', 'INT', 'FK→clients', False, True),
    ('membership_type_id', 'INT', 'FK→mem_types', False, True),
    ('start_date', 'DATE', 'NOT NULL', False, False),
    ('end_date', 'DATE', 'NOT NULL', False, False),
    ('is_unlimited', 'BOOLEAN', '', False, False),
    ('visits_remaining', 'INT', 'DEFAULT 0', False, False),
    ('is_active', 'BOOLEAN', 'DEFAULT true', False, False),
    ('sold_by_user_id', 'INT', 'FK→users', False, True),
    ('price', 'DECIMAL(10,2)', 'NOT NULL', False, False),
], width=W + 0.4)

class_types = table(ax, 13.0, 17.8, 'CLASS_TYPES', [
    ('id', 'SERIAL', 'PK', True, False),
    ('name', 'VARCHAR(100)', 'NOT NULL', False, False),
    ('description', 'TEXT', '', False, False),
    ('color', 'VARCHAR(7)', "DEFAULT '#2196F3'", False, False),
    ('is_active', 'BOOLEAN', 'DEFAULT true', False, False),
], width=W + 0.2)

classes = table(ax, 13.0, 11.5, 'CLASSES', [
    ('id', 'SERIAL', 'PK', True, False),
    ('class_type_id', 'INT', 'FK→class_types', False, True),
    ('trainer_id', 'INT', 'FK→users', False, True),
    ('room', 'VARCHAR(100)', '', False, False),
    ('start_time', 'TIMESTAMP', 'NOT NULL', False, False),
    ('end_time', 'TIMESTAMP', 'NOT NULL', False, False),
    ('max_participants', 'INT', 'DEFAULT 20', False, False),
    ('status', 'VARCHAR(20)', '', False, False),
    ('created_by_user_id', 'INT', 'FK→users', False, True),
], width=W + 0.3)

enrollments = table(ax, 8.8, 7.5, 'CLASS_ENROLLMENTS', [
    ('id', 'SERIAL', 'PK', True, False),
    ('class_id', 'INT', 'FK→classes', False, True),
    ('client_id', 'INT', 'FK→clients', False, True),
    ('membership_id', 'INT', 'FK→memberships', False, True),
    ('enrolled_at', 'TIMESTAMP', 'DEFAULT NOW()', False, False),
    ('status', 'VARCHAR(20)', '', False, False),
], width=W + 0.5)

attendance = table(ax, 17.5, 9.5, 'ATTENDANCE', [
    ('id', 'SERIAL', 'PK', True, False),
    ('class_id', 'INT', 'FK→classes', False, True),
    ('client_id', 'INT', 'FK→clients', False, True),
    ('membership_id', 'INT', 'FK→memberships', False, True),
    ('checked_in_at', 'TIMESTAMP', 'DEFAULT NOW()', False, False),
    ('checked_in_by_user_id', 'INT', 'FK→users', False, True),
    ('status', 'VARCHAR(20)', '', False, False),
], width=W + 0.5)

# ===================== CONNECTIONS =====================
# roles → users
conn(ax, roles, 'right', users, 'left', '1:N', '#1565C0')
# users → classes (trainer)
conn(ax, users, 'right', classes, 'left', '1:N', '#6A1B9A')
# users → memberships (sold_by)
conn(ax, users, 'bottom', memberships, 'top', '1:N', '#00695C')
# users → attendance (checked_in_by)
ax.annotate('', xy=(attendance['left'], attendance['mid_y'] - 0.3),
    xytext=(users['right'], users['mid_y'] - 1.0),
    arrowprops=dict(arrowstyle='->', color='#795548', lw=1.1,
        connectionstyle='arc3,rad=-0.3'))
ax.text(13.0, 14.5, '1:N\n(checked_in)', ha='center', fontsize=6.5, color='#795548')

# clients → memberships
conn(ax, clients, 'right', memberships, 'left', '1:N', '#2E7D32')
# clients → enrollments
ax.annotate('', xy=(enrollments['left'], enrollments['mid_y']),
    xytext=(clients['right'], clients['mid_y']),
    arrowprops=dict(arrowstyle='->', color='#2E7D32', lw=1.1,
        connectionstyle='arc3,rad=0.2'))
ax.text(5.5, 8.5, '1:N', ha='center', fontsize=7, color='#2E7D32', fontweight='bold')
# clients → attendance
ax.annotate('', xy=(attendance['left'], attendance['mid_y'] + 0.3),
    xytext=(clients['right'], clients['bottom'] + 0.2),
    arrowprops=dict(arrowstyle='->', color='#2E7D32', lw=1.1,
        connectionstyle='arc3,rad=-0.15'))
ax.text(9.0, 6.5, '1:N', ha='center', fontsize=7, color='#2E7D32', fontweight='bold')

# membership_types → memberships
conn(ax, mem_types, 'bottom', memberships, 'top', '1:N', '#E65100')
# memberships → enrollments
conn(ax, memberships, 'bottom', enrollments, 'top', '1:N', '#5C6BC0')
# memberships → attendance
ax.annotate('', xy=(attendance['left'], attendance['mid_y']),
    xytext=(memberships['right'], memberships['mid_y']),
    arrowprops=dict(arrowstyle='->', color='#5C6BC0', lw=1.1,
        connectionstyle='arc3,rad=0.25'))
ax.text(12.5, 9.8, '1:N', ha='center', fontsize=7, color='#5C6BC0', fontweight='bold')

# class_types → classes
conn(ax, class_types, 'bottom', classes, 'top', '1:N', '#AD1457')
# classes → enrollments
conn(ax, classes, 'bottom', enrollments, 'right', '1:N', '#1565C0')
# classes → attendance
conn(ax, classes, 'right', attendance, 'left', '1:N', '#1565C0')

plt.title('ER-Диаграмма — База данных ИС FitTime',
    fontsize=14, fontweight='bold', pad=10)
plt.tight_layout()
plt.savefig('erd.png', dpi=150, bbox_inches='tight', facecolor='#F8F9FA')
print("erd.png saved")
