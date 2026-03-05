-- ============================================================
-- Схема базы данных: FitTime
-- СУБД: PostgreSQL 15+
-- Кодировка: UTF-8
-- Инициализация: выполнить скрипт вручную через psql или pgAdmin
-- ============================================================

-- ------------------------------------------------------------
-- 1. Роли пользователей
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS roles (
    id          SERIAL      PRIMARY KEY,
    name        VARCHAR(50) NOT NULL UNIQUE,    -- 'Admin', 'Manager', 'Trainer'
    description TEXT
);

INSERT INTO roles (name, description) VALUES
    ('Admin',   'Полный доступ ко всем разделам'),
    ('Manager', 'Управление клиентами, абонементами, расписанием'),
    ('Trainer', 'Просмотр расписания, отметка посещаемости')
ON CONFLICT (name) DO NOTHING;

-- ------------------------------------------------------------
-- 2. Пользователи системы (включая тренеров)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id               SERIAL       PRIMARY KEY,
    login            VARCHAR(100) NOT NULL UNIQUE,
    password_hash    VARCHAR(255) NOT NULL,          -- BCrypt hash
    role_id          INTEGER      NOT NULL REFERENCES roles(id),
    first_name       VARCHAR(100) NOT NULL,
    last_name        VARCHAR(100) NOT NULL,
    patronymic       VARCHAR(100),
    phone            VARCHAR(20),
    email            VARCHAR(150),
    specialization   VARCHAR(200),                   -- Заполняется только для тренеров
    is_active        BOOLEAN      NOT NULL DEFAULT TRUE,
    failed_attempts  SMALLINT     NOT NULL DEFAULT 0,
    last_login_at    TIMESTAMP,
    created_at       TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Начальный администратор (пароль задаётся при первом запуске)
INSERT INTO users (login, password_hash, role_id, first_name, last_name)
VALUES ('admin', '$2a$11$Fhw8nICUDpjSJJW0SF1TSegpnJ6PAh.rk5Ow7U1h6Mv3V3AAHFj56', 1, 'Администратор', 'Системный')
ON CONFLICT (login) DO NOTHING;

-- ------------------------------------------------------------
-- 3. Клиенты
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS clients (
    id          SERIAL       PRIMARY KEY,
    first_name  VARCHAR(100) NOT NULL,
    last_name   VARCHAR(100) NOT NULL,
    patronymic  VARCHAR(100),
    birth_date  DATE,
    phone       VARCHAR(20),
    email       VARCHAR(150),
    notes       TEXT,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMP    NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMP    NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_clients_last_name ON clients(last_name);
CREATE INDEX IF NOT EXISTS idx_clients_phone     ON clients(phone);
CREATE INDEX IF NOT EXISTS idx_clients_email     ON clients(email);

-- ------------------------------------------------------------
-- 4. Типы абонементов (справочник)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS membership_types (
    id             SERIAL          PRIMARY KEY,
    name           VARCHAR(150)    NOT NULL,
    duration_days  INTEGER         NOT NULL,           -- Срок действия в днях
    is_unlimited   BOOLEAN         NOT NULL DEFAULT FALSE,
    visit_count    INTEGER         NOT NULL DEFAULT 0, -- Игнорируется если is_unlimited = TRUE
    price          NUMERIC(10, 2)  NOT NULL,
    description    TEXT,
    is_archived    BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at     TIMESTAMP       NOT NULL DEFAULT NOW()
);

INSERT INTO membership_types (name, duration_days, is_unlimited, visit_count, price, description) VALUES
    ('Разовое занятие',     30,  FALSE,  1,  500.00, 'Одно посещение любого занятия'),
    ('8 занятий',           60,  FALSE,  8, 3200.00, '8 посещений в течение 2 месяцев'),
    ('Месяц безлимит',      30,  TRUE,   0, 4500.00, 'Неограниченное кол-во занятий в течение месяца'),
    ('3 месяца безлимит',   90,  TRUE,   0,11000.00, 'Неограниченное кол-во занятий в течение 3 месяцев')
ON CONFLICT DO NOTHING;

-- ------------------------------------------------------------
-- 5. Абонементы клиентов
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS memberships (
    id                 SERIAL          PRIMARY KEY,
    client_id          INTEGER         NOT NULL REFERENCES clients(id),
    membership_type_id INTEGER         NOT NULL REFERENCES membership_types(id),
    start_date         DATE            NOT NULL,
    end_date           DATE            NOT NULL,       -- Вычисляется: start_date + duration_days
    is_unlimited       BOOLEAN         NOT NULL DEFAULT FALSE,
    visits_remaining   INTEGER         NOT NULL DEFAULT 0, -- Игнорируется если is_unlimited = TRUE
    is_active          BOOLEAN         NOT NULL DEFAULT TRUE,
    sold_by_user_id    INTEGER         REFERENCES users(id),
    price              NUMERIC(10, 2)  NOT NULL,       -- Цена на момент продажи
    notes              TEXT,
    created_at         TIMESTAMP       NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_memberships_client   ON memberships(client_id);
CREATE INDEX IF NOT EXISTS idx_memberships_end_date ON memberships(end_date);
CREATE INDEX IF NOT EXISTS idx_memberships_active   ON memberships(is_active);

-- ------------------------------------------------------------
-- 6. Типы занятий (справочник)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS class_types (
    id          SERIAL       PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    color       VARCHAR(7)   NOT NULL DEFAULT '#2196F3', -- HEX-цвет для календаря
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE
);

INSERT INTO class_types (name, color, description) VALUES
    ('Йога',        '#9C27B0', 'Йога для начинающих и продолжающих'),
    ('Пилатес',     '#E91E63', 'Пилатес на коврике'),
    ('Кроссфит',    '#F44336', 'Функциональный тренинг высокой интенсивности'),
    ('Аквааэробика','#03A9F4', 'Занятия в бассейне'),
    ('Стретчинг',   '#4CAF50', 'Растяжка и гибкость'),
    ('Зумба',       '#FF9800', 'Танцевальная аэробика')
ON CONFLICT (name) DO NOTHING;

-- ------------------------------------------------------------
-- 7. Занятия
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS classes (
    id                SERIAL       PRIMARY KEY,
    class_type_id     INTEGER      NOT NULL REFERENCES class_types(id),
    trainer_id        INTEGER      NOT NULL REFERENCES users(id),
    room              VARCHAR(100) NOT NULL DEFAULT 'Основной зал',
    start_time        TIMESTAMP    NOT NULL,
    end_time          TIMESTAMP    NOT NULL,
    max_participants  INTEGER      NOT NULL DEFAULT 20,
    status            VARCHAR(20)  NOT NULL DEFAULT 'Scheduled'
                          CHECK (status IN ('Scheduled', 'Cancelled', 'Completed')),
    notes             TEXT,
    created_by_user_id INTEGER     REFERENCES users(id),
    created_at        TIMESTAMP    NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_classes_start_time ON classes(start_time);
CREATE INDEX IF NOT EXISTS idx_classes_trainer    ON classes(trainer_id);
CREATE INDEX IF NOT EXISTS idx_classes_status     ON classes(status);

-- ------------------------------------------------------------
-- 8. Записи клиентов на занятия
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS class_enrollments (
    id             SERIAL    PRIMARY KEY,
    class_id       INTEGER   NOT NULL REFERENCES classes(id),
    client_id      INTEGER   NOT NULL REFERENCES clients(id),
    membership_id  INTEGER   REFERENCES memberships(id),
    enrolled_at    TIMESTAMP NOT NULL DEFAULT NOW(),
    status         VARCHAR(20) NOT NULL DEFAULT 'Enrolled'
                       CHECK (status IN ('Enrolled', 'Cancelled')),
    UNIQUE (class_id, client_id)
);

CREATE INDEX IF NOT EXISTS idx_enrollments_class  ON class_enrollments(class_id);
CREATE INDEX IF NOT EXISTS idx_enrollments_client ON class_enrollments(client_id);

-- ------------------------------------------------------------
-- 9. Посещения (журнал)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS attendance (
    id                   SERIAL    PRIMARY KEY,
    class_id             INTEGER   NOT NULL REFERENCES classes(id),
    client_id            INTEGER   NOT NULL REFERENCES clients(id),
    membership_id        INTEGER   REFERENCES memberships(id),
    checked_in_at        TIMESTAMP NOT NULL DEFAULT NOW(),
    checked_in_by_user_id INTEGER  REFERENCES users(id),
    status               VARCHAR(20) NOT NULL DEFAULT 'Present'
                             CHECK (status IN ('Present', 'Absent', 'Cancelled')),
    notes                TEXT
);

CREATE INDEX IF NOT EXISTS idx_attendance_class  ON attendance(class_id);
CREATE INDEX IF NOT EXISTS idx_attendance_client ON attendance(client_id);
CREATE INDEX IF NOT EXISTS idx_attendance_date   ON attendance(checked_in_at);

-- ============================================================
-- Тестовые данные
-- ============================================================

-- Дополнительные пользователи (тренеры и менеджеры)
INSERT INTO users (login, password_hash, role_id, first_name, last_name, patronymic, phone, email, specialization) VALUES
    ('manager1',  '$2a$11$grj.S3gnR0Yl8U9bYRRgwOM83L0FY3VXRXrNbb1q7NwcLELNE.u7u', 2, 'Ольга',    'Смирнова',   'Викторовна',  '+79101234501', 'smirnova@fittime.ru',   NULL),
    ('manager2',  '$2a$11$DPZQHvb5EKXBdBLOC8TDBOWz7II.e5XvcroM29O5LYBhjYMFExQh.', 2, 'Дмитрий',  'Козлов',     'Андреевич',   '+79101234502', 'kozlov@fittime.ru',     NULL),
    ('trainer1',  '$2a$11$wIqYlB.Zwx3npHfpsVZdUeSapHXcADqkQ7haCBdaSovg0bt/Zb8xi', 3, 'Алексей',  'Волков',     'Сергеевич',   '+79101234503', 'volkov@fittime.ru',     'Йога, Стретчинг'),
    ('trainer2',  '$2a$11$jw.qLGYnuJFBDCZHeLNeROOoCulk0HcExXYb6xG7L1D8g5GhXJ12S', 3, 'Мария',    'Новикова',   'Александровна','+79101234504', 'novikova@fittime.ru',  'Пилатес, Зумба'),
    ('trainer3',  '$2a$11$AJnAhHzAqJe/XEMKFDELJeqWHy3iyPpj5/iqBQLoigbqqPdpi1kgG', 3, 'Иван',     'Соколов',    'Дмитриевич',  '+79101234505', 'sokolov@fittime.ru',    'Кроссфит'),
    ('trainer4',  '$2a$11$Iv0QfayN0NN/I14BQRneJudtIFvZnM7HA7LWMvZEKCL6XwOuXeVy.', 3, 'Екатерина','Морозова',   'Игоревна',    '+79101234506', 'morozova@fittime.ru',   'Аквааэробика'),
    ('trainer5',  '$2a$11$ItRDAnEpFxALDfjyjm3i4ekSvkqTw.8w0csHvu2g4/XoXYxLKO4Ka', 3, 'Андрей',   'Лебедев',    'Павлович',    '+79101234507', 'lebedev@fittime.ru',    'Кроссфит, Стретчинг'),
    ('trainer6',  '$2a$11$RRZJ8NnrAS8SnR3YyDw6ceynHJjBmbTsvGlhAgvwA.KOEZ1lYVShe', 3, 'Наталья',  'Кузнецова',  'Олеговна',    '+79101234508', 'kuznetsova@fittime.ru', 'Йога, Пилатес'),
    ('trainer7',  '$2a$11$6y6LM8rvowGhXQY3ViWwNu2x1kY8tyQqs6ujF8CFy1nHsCar0mnVa', 3, 'Сергей',   'Попов',      'Валерьевич',  '+79101234509', 'popov@fittime.ru',      'Зумба'),
    ('trainer8',  '$2a$11$Wgy3MMwmxfPKjLhgArVE1.ZJffmtUt6cU/rl6Y9ntZbiGn9oCL2Fm', 3, 'Анна',     'Федорова',   'Михайловна',  '+79101234510', 'fedorova@fittime.ru',   'Стретчинг, Пилатес'),
    ('manager3',  '$2a$11$a3zZOEnk6Vj4cFaTeTFhkOyNQotV9TwaC3FU6JC.jX56VE/yWSgN2', 2, 'Виктор',   'Егоров',     'Николаевич',  '+79101234511', 'egorov@fittime.ru',     NULL),
    ('trainer9',  '$2a$11$CUdtmibgeTRTJSqjsKA4yucCWcD2Z8AvqrWSVCuffOB/.H1glSNrO', 3, 'Елена',    'Павлова',    'Артёмовна',   '+79101234512', 'pavlova@fittime.ru',    'Йога'),
    ('trainer10', '$2a$11$63tlkGuUJ0aiG7t1frnHT.uCA0SzUxyrAoF14PZ2Fr3JJeCGWRMV.', 3, 'Максим',   'Семёнов',    'Русланович',  '+79101234513', 'semenov@fittime.ru',    'Кроссфит, Зумба'),
    ('manager4',  '$2a$11$dGAyw4l.mYtVBDRMnXzzs.ub5U8XlbZMRiZ9TXULql9Nh6vLtbS5a', 2, 'Татьяна',  'Белова',     'Сергеевна',   '+79101234514', 'belova@fittime.ru',     NULL),
    ('trainer11', '$2a$11$yKEhlUvXHAJGT8C/Cz6BP.1JWtfevo6KO8NaVwsu2LFRKS1vx7vAq', 3, 'Роман',    'Комаров',    'Евгеньевич',  '+79101234515', 'komarov@fittime.ru',    'Аквааэробика, Стретчинг')
ON CONFLICT (login) DO NOTHING;

-- Клиенты
INSERT INTO clients (first_name, last_name, patronymic, birth_date, phone, email, notes) VALUES
    ('Александр', 'Иванов',     'Петрович',    '1990-05-14', '+79201111101', 'ivanov@mail.ru',      'Постоянный клиент'),
    ('Светлана',  'Петрова',    'Анатольевна', '1985-11-22', '+79201111102', 'petrova@mail.ru',     NULL),
    ('Михаил',    'Сидоров',    'Юрьевич',     '1992-03-08', '+79201111103', 'sidorov@mail.ru',     'Травма колена — осторожно'),
    ('Анна',      'Кузнецова',  'Дмитриевна',  '1998-07-30', '+79201111104', 'kuznetsova_a@mail.ru','Новичок'),
    ('Дмитрий',   'Орлов',      'Викторович',  '1988-01-17', '+79201111105', 'orlov@mail.ru',       NULL),
    ('Елена',     'Васильева',  'Сергеевна',   '1995-09-03', '+79201111106', 'vasilieva@mail.ru',   NULL),
    ('Артём',     'Николаев',   'Олегович',    '2000-12-25', '+79201111107', 'nikolaev@mail.ru',    'Студент, скидка 10%'),
    ('Ирина',     'Захарова',   'Павловна',    '1993-06-11', '+79201111108', 'zakharova@mail.ru',   NULL),
    ('Павел',     'Морозов',    'Алексеевич',  '1987-04-29', '+79201111109', 'morozov_p@mail.ru',   'VIP клиент'),
    ('Ольга',     'Романова',   'Ивановна',    '1991-08-15', '+79201111110', 'romanova@mail.ru',    NULL),
    ('Кирилл',    'Волков',     'Романович',   '1996-02-20', '+79201111111', 'volkov_k@mail.ru',    NULL),
    ('Марина',    'Алексеева',  'Фёдоровна',   '1989-10-07', '+79201111112', 'alekseeva@mail.ru',   'Аллергия на хлорку — без бассейна'),
    ('Григорий',  'Лебедев',    'Тимофеевич',  '1994-11-30', '+79201111113', 'lebedev_g@mail.ru',   NULL),
    ('Юлия',      'Козлова',    'Максимовна',  '1997-01-05', '+79201111114', 'kozlova_y@mail.ru',   NULL),
    ('Вадим',     'Новиков',    'Степанович',  '1986-07-18', '+79201111115', 'novikov_v@mail.ru',   'Ходит на кроссфит 3 раза в неделю'),
    ('Наталья',   'Соловьёва',  'Геннадьевна', '1999-03-21', '+79201111116', 'solovieva@mail.ru',   NULL),
    ('Денис',     'Виноградов', 'Артёмович',   '1991-05-09', '+79201111117', 'vinogradov@mail.ru',  NULL),
    ('Алина',     'Богданова',  'Владимировна','2001-08-13', '+79201111118', 'bogdanova@mail.ru',   'Студентка')
ON CONFLICT DO NOTHING;

-- Абонементы клиентов (sold_by_user_id ссылается на менеджеров: 2, 3, 12, 15)
INSERT INTO memberships (client_id, membership_type_id, start_date, end_date, is_unlimited, visits_remaining, is_active, sold_by_user_id, price, notes) VALUES
    (1,  3, '2026-02-01', '2026-03-03', TRUE,  0, TRUE,  2, 4500.00, NULL),
    (2,  4, '2026-01-15', '2026-04-15', TRUE,  0, TRUE,  2, 11000.00, NULL),
    (3,  2, '2026-02-10', '2026-04-10', FALSE, 5, TRUE,  3, 3200.00, NULL),
    (4,  1, '2026-02-20', '2026-03-22', FALSE, 0, FALSE, 2, 500.00,  'Использовано'),
    (5,  3, '2026-02-01', '2026-03-03', TRUE,  0, TRUE,  3, 4500.00, NULL),
    (6,  2, '2026-01-20', '2026-03-20', FALSE, 3, TRUE,  2, 3200.00, NULL),
    (7,  4, '2026-02-01', '2026-05-01', TRUE,  0, TRUE,  12, 11000.00, 'Скидка студенту'),
    (8,  3, '2026-02-15', '2026-03-17', TRUE,  0, TRUE,  3, 4500.00, NULL),
    (9,  4, '2026-01-01', '2026-04-01', TRUE,  0, TRUE,  2, 11000.00, 'VIP'),
    (10, 2, '2026-02-05', '2026-04-05', FALSE, 6, TRUE,  3, 3200.00, NULL),
    (11, 1, '2026-02-25', '2026-03-27', FALSE, 1, TRUE,  12, 500.00, NULL),
    (12, 3, '2026-02-10', '2026-03-12', TRUE,  0, TRUE,  2, 4500.00, NULL),
    (13, 2, '2026-02-01', '2026-04-01', FALSE, 4, TRUE,  15, 3200.00, NULL),
    (14, 4, '2026-01-10', '2026-04-10', TRUE,  0, TRUE,  3, 11000.00, NULL),
    (15, 3, '2026-02-15', '2026-03-17', TRUE,  0, TRUE,  15, 4500.00, NULL),
    (16, 2, '2026-02-01', '2026-04-01', FALSE, 7, TRUE,  2, 3200.00, NULL),
    (17, 1, '2026-02-28', '2026-03-30', FALSE, 1, TRUE,  3, 500.00,  NULL),
    (18, 3, '2026-02-20', '2026-03-22', TRUE,  0, TRUE,  12, 4500.00, 'Студентка')
ON CONFLICT DO NOTHING;

-- Занятия (trainer_id: тренеры 4–11, created_by: менеджеры 2,3)
INSERT INTO classes (class_type_id, trainer_id, room, start_time, end_time, max_participants, status, created_by_user_id) VALUES
    (1, 4,  'Зал йоги',       '2026-02-24 09:00', '2026-02-24 10:00', 15, 'Completed',  2),
    (2, 5,  'Зал пилатеса',   '2026-02-24 10:00', '2026-02-24 11:00', 12, 'Completed',  2),
    (3, 6,  'Кроссфит-зона',  '2026-02-24 11:00', '2026-02-24 12:00', 20, 'Completed',  3),
    (4, 7,  'Бассейн',        '2026-02-24 14:00', '2026-02-24 15:00', 10, 'Completed',  2),
    (5, 4,  'Зал стретчинга', '2026-02-25 09:00', '2026-02-25 10:00', 15, 'Completed',  3),
    (6, 5,  'Основной зал',   '2026-02-25 18:00', '2026-02-25 19:00', 25, 'Completed',  2),
    (1, 9,  'Зал йоги',       '2026-02-26 09:00', '2026-02-26 10:00', 15, 'Completed',  2),
    (3, 8,  'Кроссфит-зона',  '2026-02-26 17:00', '2026-02-26 18:00', 20, 'Completed',  3),
    (2, 11, 'Зал пилатеса',   '2026-02-27 10:00', '2026-02-27 11:00', 12, 'Completed',  2),
    (5, 4,  'Зал стретчинга', '2026-02-27 12:00', '2026-02-27 13:00', 15, 'Completed',  3),
    (6, 10, 'Основной зал',   '2026-02-28 18:00', '2026-02-28 19:00', 25, 'Completed',  2),
    (4, 7,  'Бассейн',        '2026-02-28 14:00', '2026-02-28 15:00', 10, 'Completed',  2),
    (1, 4,  'Зал йоги',       '2026-03-01 09:00', '2026-03-01 10:00', 15, 'Scheduled',  3),
    (3, 6,  'Кроссфит-зона',  '2026-03-01 11:00', '2026-03-01 12:00', 20, 'Scheduled',  2),
    (2, 5,  'Зал пилатеса',   '2026-03-01 10:00', '2026-03-01 11:00', 12, 'Scheduled',  3),
    (6, 10, 'Основной зал',   '2026-03-02 18:00', '2026-03-02 19:00', 25, 'Scheduled',  2),
    (5, 11, 'Зал стретчинга', '2026-03-02 09:00', '2026-03-02 10:00', 15, 'Scheduled',  3),
    (4, 7,  'Бассейн',        '2026-03-03 14:00', '2026-03-03 15:00', 10, 'Scheduled',  2),
    (1, 9,  'Зал йоги',       '2026-03-03 09:00', '2026-03-03 10:00', 15, 'Scheduled',  2),
    (3, 8,  'Кроссфит-зона',  '2026-03-04 17:00', '2026-03-04 18:00', 20, 'Scheduled',  3)
ON CONFLICT DO NOTHING;

-- Записи клиентов на занятия
INSERT INTO class_enrollments (class_id, client_id, membership_id, status) VALUES
    (1,  1,  1,  'Enrolled'),
    (1,  2,  2,  'Enrolled'),
    (1,  8,  8,  'Enrolled'),
    (2,  6,  6,  'Enrolled'),
    (2,  10, 10, 'Enrolled'),
    (3,  5,  5,  'Enrolled'),
    (3,  15, 15, 'Enrolled'),
    (3,  3,  3,  'Enrolled'),
    (4,  9,  9,  'Enrolled'),
    (4,  14, 14, 'Enrolled'),
    (5,  1,  1,  'Enrolled'),
    (5,  12, 12, 'Enrolled'),
    (6,  7,  7,  'Enrolled'),
    (6,  2,  2,  'Enrolled'),
    (6,  16, 16, 'Enrolled'),
    (7,  1,  1,  'Enrolled'),
    (7,  8,  8,  'Enrolled'),
    (8,  5,  5,  'Enrolled'),
    (8,  15, 15, 'Enrolled'),
    (8,  17, 17, 'Enrolled'),
    (9,  6,  6,  'Enrolled'),
    (9,  14, 14, 'Enrolled'),
    (10, 12, 12, 'Enrolled'),
    (10, 1,  1,  'Enrolled'),
    (11, 7,  7,  'Enrolled'),
    (11, 16, 16, 'Enrolled'),
    (11, 2,  2,  'Enrolled'),
    (12, 9,  9,  'Enrolled'),
    (13, 1,  1,  'Enrolled'),
    (13, 8,  8,  'Enrolled'),
    (13, 18, 18, 'Enrolled'),
    (14, 5,  5,  'Enrolled'),
    (14, 15, 15, 'Enrolled'),
    (14, 3,  3,  'Enrolled'),
    (15, 6,  6,  'Enrolled'),
    (15, 10, 10, 'Enrolled'),
    (16, 7,  7,  'Enrolled'),
    (16, 2,  2,  'Enrolled'),
    (17, 12, 12, 'Enrolled'),
    (17, 14, 14, 'Enrolled'),
    (18, 9,  9,  'Enrolled'),
    (19, 1,  1,  'Enrolled'),
    (19, 13, 13, 'Enrolled'),
    (20, 15, 15, 'Enrolled'),
    (20, 5,  5,  'Enrolled')
ON CONFLICT (class_id, client_id) DO NOTHING;

-- Посещения (для завершённых занятий 1–12)
INSERT INTO attendance (class_id, client_id, membership_id, checked_in_at, checked_in_by_user_id, status, notes) VALUES
    (1,  1,  1,  '2026-02-24 08:55', 2, 'Present', NULL),
    (1,  2,  2,  '2026-02-24 08:58', 2, 'Present', NULL),
    (1,  8,  8,  '2026-02-24 09:02', 2, 'Present', 'Опоздала на 2 мин'),
    (2,  6,  6,  '2026-02-24 09:55', 3, 'Present', NULL),
    (2,  10, 10, '2026-02-24 09:50', 3, 'Present', NULL),
    (3,  5,  5,  '2026-02-24 10:55', 3, 'Present', NULL),
    (3,  15, 15, '2026-02-24 10:50', 3, 'Present', NULL),
    (3,  3,  3,  '2026-02-24 11:00', 3, 'Absent',  'Не пришёл, не предупредил'),
    (4,  9,  9,  '2026-02-24 13:50', 2, 'Present', NULL),
    (4,  14, 14, '2026-02-24 13:55', 2, 'Present', NULL),
    (5,  1,  1,  '2026-02-25 08:50', 3, 'Present', NULL),
    (5,  12, 12, '2026-02-25 08:55', 3, 'Present', NULL),
    (6,  7,  7,  '2026-02-25 17:50', 2, 'Present', NULL),
    (6,  2,  2,  '2026-02-25 17:55', 2, 'Present', NULL),
    (6,  16, 16, '2026-02-25 18:00', 2, 'Absent',  'Заболела'),
    (7,  1,  1,  '2026-02-26 08:55', 2, 'Present', NULL),
    (7,  8,  8,  '2026-02-26 08:58', 2, 'Present', NULL),
    (8,  5,  5,  '2026-02-26 16:50', 3, 'Present', NULL),
    (8,  15, 15, '2026-02-26 16:55', 3, 'Present', NULL),
    (8,  17, 17, '2026-02-26 17:00', 3, 'Present', NULL),
    (9,  6,  6,  '2026-02-27 09:55', 2, 'Present', NULL),
    (9,  14, 14, '2026-02-27 09:50', 2, 'Present', NULL),
    (10, 12, 12, '2026-02-27 11:55', 3, 'Present', NULL),
    (10, 1,  1,  '2026-02-27 11:58', 3, 'Present', NULL),
    (11, 7,  7,  '2026-02-28 17:50', 2, 'Present', NULL),
    (11, 16, 16, '2026-02-28 17:55', 2, 'Present', NULL),
    (11, 2,  2,  '2026-02-28 18:05', 2, 'Present', 'Опоздала на 5 мин'),
    (12, 9,  9,  '2026-02-28 13:50', 2, 'Present', NULL)
ON CONFLICT DO NOTHING;

-- ============================================================
-- Представления (Views)
-- ============================================================

-- Активные абонементы с расчётом статуса
CREATE OR REPLACE VIEW v_active_memberships AS
SELECT
    m.id,
    m.client_id,
    c.last_name || ' ' || c.first_name || ' ' || COALESCE(c.patronymic, '') AS client_full_name,
    c.phone,
    mt.name                         AS membership_type_name,
    m.start_date,
    m.end_date,
    m.is_unlimited,
    m.visits_remaining,
    CASE
        WHEN m.end_date < CURRENT_DATE              THEN 'Просрочен'
        WHEN m.end_date <= CURRENT_DATE + 7         THEN 'Истекает'
        ELSE                                             'Активен'
    END                             AS membership_status,
    (m.end_date - CURRENT_DATE)     AS days_remaining
FROM memberships m
JOIN clients c          ON m.client_id = c.id
JOIN membership_types mt ON m.membership_type_id = mt.id
WHERE m.is_active = TRUE AND c.is_active = TRUE;

-- Занятия с числом записанных участников
CREATE OR REPLACE VIEW v_classes_with_occupancy AS
SELECT
    cl.id,
    ct.name                                         AS class_type_name,
    ct.color,
    u.last_name || ' ' || u.first_name              AS trainer_name,
    cl.room,
    cl.start_time,
    cl.end_time,
    cl.max_participants,
    cl.status,
    COUNT(ce.id)                                    AS enrolled_count,
    cl.max_participants - COUNT(ce.id)              AS free_places
FROM classes cl
JOIN class_types ct ON cl.class_type_id = ct.id
JOIN users u        ON cl.trainer_id = u.id
LEFT JOIN class_enrollments ce ON ce.class_id = cl.id AND ce.status = 'Enrolled'
GROUP BY cl.id, ct.name, ct.color, u.last_name, u.first_name, cl.room,
         cl.start_time, cl.end_time, cl.max_participants, cl.status;
