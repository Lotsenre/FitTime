#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch

fig, ax = plt.subplots(figsize=(26, 20))
ax.set_xlim(0, 26)
ax.set_ylim(0, 20)
ax.axis('off')
fig.patch.set_facecolor('#F8F9FA')
ax.set_facecolor('#F8F9FA')

ROW_H = 0.26
HEADER_H = 0.38
DIVIDER_H = 0.22  # divider row between sections

COLORS = {
    'models': ('#1A237E', '#E8EAF6', '#C5CAE9'),
    'viewmodels': ('#1B5E20', '#E8F5E9', '#C8E6C9'),
    'services': ('#4A148C', '#F3E5F5', '#E1BEE7'),
    'data': ('#E65100', '#FFF3E0', '#FFCCBC'),
}

def uml_class(ax, x, y, name, stereotype, props, methods, kind='models', width=3.8):
    c = COLORS[kind]
    hdr_color, body_color, div_color = c

    sections = [('attrs', props), ('methods', methods)]
    total_rows = len(props) + (1 if methods else 0) + len(methods)
    total_h = HEADER_H + total_rows * ROW_H + (DIVIDER_H if methods else 0)

    # Header
    hbox = FancyBboxPatch((x, y - HEADER_H), width, HEADER_H,
        boxstyle="round,pad=0.04", lw=2,
        edgecolor=hdr_color, facecolor=hdr_color, zorder=2)
    ax.add_patch(hbox)
    if stereotype:
        ax.text(x + width/2, y - 0.12, f'«{stereotype}»', ha='center', va='center',
            fontsize=6.5, color='#FFEB3B', zorder=3, style='italic')
        ax.text(x + width/2, y - 0.28, name, ha='center', va='center',
            fontsize=8.5, fontweight='bold', color='white', zorder=3)
    else:
        ax.text(x + width/2, y - HEADER_H/2, name, ha='center', va='center',
            fontsize=8.5, fontweight='bold', color='white', zorder=3)

    cur_y = y - HEADER_H
    # Attrs
    for i, attr in enumerate(props):
        fy = cur_y - (i+1) * ROW_H
        fc = body_color if i % 2 == 0 else '#FAFAFA'
        rbox = FancyBboxPatch((x, fy), width, ROW_H,
            boxstyle="square,pad=0", lw=0.5,
            edgecolor='#BDBDBD', facecolor=fc, zorder=2)
        ax.add_patch(rbox)
        ax.text(x + 0.1, fy + ROW_H/2, attr, ha='left', va='center',
            fontsize=6.5, color='#1A237E', zorder=3)

    cur_y -= len(props) * ROW_H

    if methods:
        # divider
        div = FancyBboxPatch((x, cur_y - DIVIDER_H), width, DIVIDER_H,
            boxstyle="square,pad=0", lw=0.5,
            edgecolor='#BDBDBD', facecolor=div_color, zorder=2)
        ax.add_patch(div)
        cur_y -= DIVIDER_H
        for i, m in enumerate(methods):
            fy = cur_y - (i+1) * ROW_H
            fc = '#FAFAFA' if i % 2 == 0 else body_color
            rbox = FancyBboxPatch((x, fy), width, ROW_H,
                boxstyle="square,pad=0", lw=0.5,
                edgecolor='#BDBDBD', facecolor=fc, zorder=2)
            ax.add_patch(rbox)
            ax.text(x + 0.1, fy + ROW_H/2, m, ha='left', va='center',
                fontsize=6.5, color='#1A237E', zorder=3)

    # Outer border
    border = FancyBboxPatch((x, y - total_h), width, total_h,
        boxstyle="square,pad=0", lw=1.5,
        edgecolor=hdr_color, facecolor='none', zorder=4)
    ax.add_patch(border)

    return {'x': x, 'y': y, 'w': width, 'h': total_h,
            'cx': x + width/2, 'cy': y - total_h/2,
            'top': y, 'bottom': y - total_h,
            'left': x, 'right': x + width,
            'mid_y': y - total_h/2}

def arrow_line(ax, x1, y1, x2, y2, style='->', label='', color='#333', dashed=False):
    lstyle = 'dashed' if dashed else 'solid'
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
        arrowprops=dict(arrowstyle=style, color=color, lw=1.2, linestyle=lstyle))
    if label:
        mx, my = (x1+x2)/2, (y1+y2)/2
        ax.text(mx+0.05, my+0.08, label, ha='left', va='bottom',
            fontsize=6.5, color=color, style='italic')

# ========== PACKAGE LABELS ==========
def pkg_label(ax, x, y, text, color):
    ax.text(x, y, f'« {text} »', ha='left', va='bottom',
        fontsize=10, fontweight='bold', color=color,
        bbox=dict(boxstyle='round,pad=0.3', facecolor=color+'22', edgecolor=color, lw=1.5))

pkg_label(ax, 0.3, 19.8, 'Models', '#1A237E')
pkg_label(ax, 14.0, 19.8, 'ViewModels', '#1B5E20')
pkg_label(ax, 20.5, 19.8, 'Services', '#4A148C')
pkg_label(ax, 14.0, 10.0, 'Data', '#E65100')

W_M = 3.6
W_VM = 4.5

# ===== MODELS =====
role = uml_class(ax, 0.2, 19.5, 'Role', None,
    ['+ Id: int', '+ Name: string', '+ Description: string?',
     '+ Users: ICollection<User>'], [], 'models', W_M)

user = uml_class(ax, 0.2, 16.8, 'User', None,
    ['+ Id: int', '+ Login: string', '+ PasswordHash: string',
     '+ RoleId: int', '+ FirstName: string', '+ LastName: string',
     '+ IsActive: bool', '+ FailedAttempts: short', '+ LastLoginAt: DateTime?',
     '+ Role: Role'],
    ['+ FullName: string «get»', '+ ShortName: string «get»'],
    'models', W_M)

client = uml_class(ax, 0.2, 12.0, 'Client', None,
    ['+ Id: int', '+ FirstName: string', '+ LastName: string',
     '+ BirthDate: DateOnly?', '+ Phone: string?', '+ IsActive: bool',
     '+ Memberships: ICollection<Membership>',
     '+ Enrollments: ICollection<ClassEnrollment>',
     '+ Attendances: ICollection<Attendance>'],
    ['+ FullName: string «get»', '+ Initials: string «get»'],
    'models', W_M)

mem_type = uml_class(ax, 0.2, 7.5, 'MembershipType', None,
    ['+ Id: int', '+ Name: string', '+ DurationDays: int',
     '+ IsUnlimited: bool', '+ VisitCount: int', '+ Price: decimal',
     '+ IsArchived: bool',
     '+ Memberships: ICollection<Membership>'],
    [], 'models', W_M)

membership = uml_class(ax, 4.2, 12.0, 'Membership', None,
    ['+ Id: int', '+ ClientId: int', '+ MembershipTypeId: int',
     '+ StartDate: DateOnly', '+ EndDate: DateOnly',
     '+ IsUnlimited: bool', '+ VisitsRemaining: int',
     '+ IsActive: bool', '+ SoldByUserId: int?', '+ Price: decimal',
     '+ Client: Client', '+ MembershipType: MembershipType'],
    [], 'models', W_M)

class_type = uml_class(ax, 4.2, 7.5, 'ClassType', None,
    ['+ Id: int', '+ Name: string', '+ Color: string', '+ IsActive: bool',
     '+ Classes: ICollection<Class>'],
    [], 'models', W_M)

cls = uml_class(ax, 8.0, 12.5, 'Class', None,
    ['+ Id: int', '+ ClassTypeId: int', '+ TrainerId: int',
     '+ Room: string', '+ StartTime: DateTime', '+ EndTime: DateTime',
     '+ MaxParticipants: int', '+ Status: string',
     '+ Trainer: User', '+ ClassType: ClassType',
     '+ Enrollments: ICollection<ClassEnrollment>',
     '+ Attendances: ICollection<Attendance>'],
    [], 'models', W_M)

enroll = uml_class(ax, 8.0, 7.0, 'ClassEnrollment', None,
    ['+ Id: int', '+ ClassId: int', '+ ClientId: int',
     '+ MembershipId: int?', '+ EnrolledAt: DateTime', '+ Status: string',
     '+ Class: Class', '+ Client: Client', '+ Membership: Membership?'],
    [], 'models', W_M)

attend = uml_class(ax, 8.0, 3.5, 'Attendance', None,
    ['+ Id: int', '+ ClassId: int', '+ ClientId: int',
     '+ MembershipId: int?', '+ CheckedInAt: DateTime',
     '+ CheckedInByUserId: int?', '+ Status: string',
     '+ Class: Class', '+ Client: Client', '+ CheckedInByUser: User?'],
    [], 'models', W_M)

# ===== VIEWMODELS =====
base_vm = uml_class(ax, 14.2, 19.5, 'BaseViewModel', 'abstract',
    ['(ObservableObject — CommunityToolkit.Mvvm)'], [], 'viewmodels', W_VM)

login_vm = uml_class(ax, 14.2, 18.2, 'LoginViewModel', None,
    ['+ Login: string', '+ Password: string', '+ ErrorMessage: string'],
    ['+ LoginCommand: IRelayCommand'],
    'viewmodels', W_VM)

main_vm = uml_class(ax, 14.2, 16.2, 'MainViewModel', None,
    ['+ CurrentView: object', '+ PageTitle: string',
     '+ CurrentUser: User', '+ IsAdmin: bool'],
    ['+ NavigateCommand: IRelayCommand<string>',
     '+ LogoutCommand: IRelayCommand'],
    'viewmodels', W_VM)

dash_vm = uml_class(ax, 14.2, 14.0, 'DashboardViewModel', None,
    ['+ ActiveClientsCount: int', '+ TodayClassesCount: int',
     '+ MonthSalesCount: int', '+ ExpiringCount: int',
     '+ WeeklyAttendance: List', '+ TodayClasses: List'],
    [], 'viewmodels', W_VM)

clients_vm = uml_class(ax, 14.2, 11.8, 'ClientsViewModel', None,
    ['+ Clients: ObservableCollection<ClientDisplay>',
     '+ SearchText: string', '+ SelectedFilter: string',
     '+ TotalCount: int'],
    ['+ AddCommand, EditCommand, DeactivateCommand'],
    'viewmodels', W_VM)

schedule_vm = uml_class(ax, 14.2, 9.5, 'ScheduleViewModel', None,
    ['+ Classes: List', '+ WeekStart/WeekEnd: DateTime',
     '+ SelectedClass: ScheduleClassDisplay'],
    ['+ PrevWeekCommand, NextWeekCommand',
     '+ AddClassCommand, EditClassCommand'],
    'viewmodels', W_VM)

reports_vm = uml_class(ax, 14.2, 7.2, 'ReportsViewModel', None,
    ['+ SelectedTab: int', '+ StartDate/EndDate: DateTime',
     '+ SalesData: List', '+ TopClasses: List'],
    ['+ ApplyFilterCommand: IRelayCommand',
     '+ ExportCsvCommand: IRelayCommand'],
    'viewmodels', W_VM)

# ===== SERVICES =====
iDialog = uml_class(ax, 20.8, 19.5, 'IDialogService', 'interface',
    [],
    ['+ ShowDialog(vm): bool?'],
    'services', W_VM - 0.5)

iNav = uml_class(ax, 20.8, 18.2, 'INavigationService', 'interface',
    [],
    ['+ NavigateTo(viewType): void'],
    'services', W_VM - 0.5)

iUser = uml_class(ax, 20.8, 16.8, 'ICurrentUserService', 'interface',
    ['+ CurrentUser: User', '+ IsAdmin: bool',
     '+ IsManager: bool', '+ IsTrainer: bool'],
    ['+ SetCurrentUser(user): void', '+ Clear(): void'],
    'services', W_VM - 0.5)

# ===== DATA =====
db_ctx = uml_class(ax, 14.2, 9.4, 'FitTimeDbContext', 'DbContext',
    ['+ Roles: DbSet<Role>', '+ Users: DbSet<User>',
     '+ Clients: DbSet<Client>', '+ MembershipTypes: DbSet<MembershipType>',
     '+ Memberships: DbSet<Membership>', '+ ClassTypes: DbSet<ClassType>',
     '+ Classes: DbSet<Class>', '+ ClassEnrollments: DbSet<ClassEnrollment>',
     '+ Attendances: DbSet<Attendance>'],
    ['+ OnModelCreating(ModelBuilder): void'],
    'data', W_VM)

# Wait - db_ctx is overlapping with schedule_vm. Let's move it lower:
# Re-draw db context at y=5.5
db_ctx = uml_class(ax, 14.2, 5.5, 'FitTimeDbContext', 'DbContext',
    ['+ Roles: DbSet<Role>', '+ Users: DbSet<User>',
     '+ Clients: DbSet<Client>', '+ MembershipTypes: DbSet<MembershipType>',
     '+ Memberships: DbSet<Membership>', '+ ClassTypes: DbSet<ClassType>',
     '+ Classes: DbSet<Class>', '+ ClassEnrollments: DbSet<ClassEnrollment>',
     '+ Attendances: DbSet<Attendance>'],
    ['+ OnModelCreating(ModelBuilder): void'],
    'data', W_VM)
# Fix label
pkg_label(ax, 14.0, 6.0, 'Data', '#E65100')

# ===== RELATIONS =====
# Models: Role → User
arrow_line(ax, role['cx'], role['bottom'], user['cx'], user['top'], '-|>', '1:N', '#1A237E')
# Models: Client → Membership
arrow_line(ax, client['right'], client['mid_y'], membership['left'], membership['mid_y'], '->', '1:N', '#1A237E')
# Models: MembershipType → Membership
arrow_line(ax, mem_type['right'], mem_type['mid_y'], membership['bottom'] - 0.0, membership['bottom'] + 0.0, '->', '1:N', '#1A237E')
# Models: ClassType → Class
arrow_line(ax, class_type['right'], class_type['mid_y'], cls['bottom'] + 0.1, cls['bottom'] + 0.1, '->', '', '#1A237E')
# Membership → Enrollment
arrow_line(ax, membership['bottom'] - 0.0, membership['bottom'] + 0.0, enroll['top'], enroll['top'] - 0.0, '->', '1:N', '#5C6BC0')
# Class → Enrollment
arrow_line(ax, cls['bottom'], cls['bottom'] - 0.0, enroll['top'] + 0.1, enroll['top'] + 0.0, '->', '1:N', '#5C6BC0')
# Class → Attendance
arrow_line(ax, cls['bottom'] - 0.0, cls['bottom'] - 0.0, attend['top'] + 0.0, attend['top'] + 0.0, '->', '1:N', '#5C6BC0')

# ViewModels: Inheritance from BaseViewModel
for vm in [login_vm, main_vm, dash_vm, clients_vm, schedule_vm, reports_vm]:
    arrow_line(ax, vm['cx'], vm['top'], base_vm['cx'], base_vm['bottom'], '->', 'extends', '#1B5E20', dashed=False)

# ViewModels → FitTimeDbContext
arrow_line(ax, main_vm['left'], main_vm['mid_y'],
    db_ctx['cx'], db_ctx['top'], '-|>', 'uses', '#E65100', dashed=True)

# FitTimeDbContext → models (one representative arrow)
arrow_line(ax, db_ctx['left'], db_ctx['mid_y'],
    cls['right'], cls['mid_y'], '-|>', 'DbSet<>', '#E65100', dashed=True)

# ViewModels → ICurrentUserService
arrow_line(ax, main_vm['right'], main_vm['mid_y'],
    iUser['left'], iUser['mid_y'], '-|>', 'uses', '#4A148C', dashed=True)

plt.title('Диаграмма классов — ИС FitTime', fontsize=14, fontweight='bold', pad=10)
plt.tight_layout()
plt.savefig('class_diagram.png', dpi=150, bbox_inches='tight', facecolor='#F8F9FA')
print("class_diagram.png saved")
