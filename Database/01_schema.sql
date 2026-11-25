-- =====================================================================
--  Информационная система «Приёмная комиссия»
--  Скрипт 1. Структура базы данных (PostgreSQL 13+)
--
--  Как запустить в DBeaver:
--    1) Создайте базу:  CREATE DATABASE admission_committee;
--    2) Подключитесь к admission_committee, откройте этот файл
--       в SQL-редакторе и выполните его целиком: Alt+X
--       («Выполнить SQL-скрипт»).
-- =====================================================================

-- Пересоздание с нуля (удобно при повторном запуске скрипта)
DROP VIEW     IF EXISTS v_rating            CASCADE;
DROP VIEW     IF EXISTS v_applicant_totals  CASCADE;
DROP VIEW     IF EXISTS v_statement         CASCADE;
DROP FUNCTION IF EXISTS fn_run_enrollment()   CASCADE;
DROP FUNCTION IF EXISTS fn_reset_enrollment() CASCADE;
DROP FUNCTION IF EXISTS fn_audit()            CASCADE;
DROP FUNCTION IF EXISTS fn_audit_users()      CASCADE;
DROP TABLE    IF EXISTS audit_log           CASCADE;
DROP TABLE    IF EXISTS statement_entries   CASCADE;
DROP TABLE    IF EXISTS applicants          CASCADE;
DROP TABLE    IF EXISTS faculty_subjects    CASCADE;
DROP TABLE    IF EXISTS subjects            CASCADE;
DROP TABLE    IF EXISTS faculties           CASCADE;
DROP TABLE    IF EXISTS users               CASCADE;
DROP TABLE    IF EXISTS roles               CASCADE;

-- ---------------------------------------------------------------------
--  Справочники
-- ---------------------------------------------------------------------

-- Роли пользователей системы
CREATE TABLE roles (
    role_id   SERIAL       PRIMARY KEY,
    role_name VARCHAR(30)  NOT NULL UNIQUE
);

-- Учётные записи. Пароль хранится только в виде хэша PBKDF2-SHA256.
CREATE TABLE users (
    user_id       SERIAL        PRIMARY KEY,
    login         VARCHAR(50)   NOT NULL UNIQUE CHECK (length(login) >= 3),
    password_hash VARCHAR(200)  NOT NULL,
    full_name     VARCHAR(150)  NOT NULL,
    role_id       INT           NOT NULL REFERENCES roles(role_id),
    is_active     BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMP     NOT NULL DEFAULT now()
);

-- Факультеты с фиксированным планом набора
CREATE TABLE faculties (
    faculty_id   SERIAL        PRIMARY KEY,
    faculty_name VARCHAR(150)  NOT NULL UNIQUE,
    plan_count   INT           NOT NULL CHECK (plan_count > 0),
    description  TEXT
);

-- Предметы (вступительные испытания) с минимальным проходным баллом
CREATE TABLE subjects (
    subject_id   SERIAL        PRIMARY KEY,
    subject_name VARCHAR(100)  NOT NULL UNIQUE,
    min_score    INT           NOT NULL DEFAULT 0 CHECK (min_score BETWEEN 0 AND 100)
);

-- Какие предметы нужно сдать для поступления на факультет (M:N)
CREATE TABLE faculty_subjects (
    faculty_id INT NOT NULL REFERENCES faculties(faculty_id) ON DELETE CASCADE,
    subject_id INT NOT NULL REFERENCES subjects(subject_id)  ON DELETE RESTRICT,
    PRIMARY KEY (faculty_id, subject_id)
);

-- ---------------------------------------------------------------------
--  Основные данные
-- ---------------------------------------------------------------------

-- Абитуриенты. Каждый абитуриент — это пользователь с ролью «Абитуриент»,
-- зарегистрированный на один факультет.
CREATE TABLE applicants (
    applicant_id    SERIAL        PRIMARY KEY,
    user_id         INT           NOT NULL UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
    full_name       VARCHAR(150)  NOT NULL,
    birth_date      DATE          NOT NULL CHECK (birth_date > DATE '1950-01-01'),
    phone           VARCHAR(20),
    email           VARCHAR(100),
    faculty_id      INT           NOT NULL REFERENCES faculties(faculty_id) ON DELETE RESTRICT,
    certificate_avg NUMERIC(3,2)  NOT NULL CHECK (certificate_avg BETWEEN 3.00 AND 5.00),
    status          VARCHAR(20)   NOT NULL DEFAULT 'Подано'
                    CHECK (status IN ('Подано', 'Зачислен', 'Не зачислен')),
    registered_at   TIMESTAMP     NOT NULL DEFAULT now()
);

-- Ведомость: баллы абитуриента по предметам.
-- Абитуриент вводит балл (is_confirmed = FALSE),
-- Администратор проверяет и регистрирует его в ведомости (is_confirmed = TRUE).
CREATE TABLE statement_entries (
    entry_id      SERIAL     PRIMARY KEY,
    applicant_id  INT        NOT NULL REFERENCES applicants(applicant_id) ON DELETE CASCADE,
    subject_id    INT        NOT NULL REFERENCES subjects(subject_id)     ON DELETE RESTRICT,
    score         INT        NOT NULL CHECK (score BETWEEN 0 AND 100),
    entered_at    TIMESTAMP  NOT NULL DEFAULT now(),
    is_confirmed  BOOLEAN    NOT NULL DEFAULT FALSE,
    confirmed_by  INT        REFERENCES users(user_id),
    confirmed_at  TIMESTAMP,
    UNIQUE (applicant_id, subject_id),
    -- подтверждённая запись обязана хранить, кто и когда её подтвердил
    CHECK (NOT is_confirmed OR (confirmed_by IS NOT NULL AND confirmed_at IS NOT NULL))
);

-- Журнал важных действий (заполняется триггерами)
CREATE TABLE audit_log (
    log_id     BIGSERIAL     PRIMARY KEY,
    event_time TIMESTAMP     NOT NULL DEFAULT now(),
    app_user   VARCHAR(50),             -- логин пользователя приложения
    db_user    VARCHAR(63)   NOT NULL DEFAULT current_user,
    table_name VARCHAR(63)   NOT NULL,
    operation  VARCHAR(10)   NOT NULL,
    row_data   JSONB
);

CREATE INDEX ix_applicants_faculty ON applicants(faculty_id);
CREATE INDEX ix_statement_applicant ON statement_entries(applicant_id);
CREATE INDEX ix_audit_time ON audit_log(event_time);

-- ---------------------------------------------------------------------
--  Журналирование
--  Приложение при каждом подключении выполняет
--     SELECT set_config('app.user_login', '<логин>', false);
--  поэтому в журнале видно, какой пользователь приложения сделал изменение.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_audit() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO audit_log(app_user, table_name, operation, row_data)
    VALUES (
        NULLIF(current_setting('app.user_login', true), ''),
        TG_TABLE_NAME,
        TG_OP,
        CASE WHEN TG_OP = 'DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END
    );
    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_audit_statement
    AFTER INSERT OR UPDATE OR DELETE ON statement_entries
    FOR EACH ROW EXECUTE FUNCTION fn_audit();

CREATE TRIGGER trg_audit_applicants
    AFTER INSERT OR UPDATE OR DELETE ON applicants
    FOR EACH ROW EXECUTE FUNCTION fn_audit();

CREATE TRIGGER trg_audit_faculties
    AFTER INSERT OR UPDATE OR DELETE ON faculties
    FOR EACH ROW EXECUTE FUNCTION fn_audit();

-- Хэш пароля в журнал не пишем: журналируем только факт создания/изменения
CREATE OR REPLACE FUNCTION fn_audit_users() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO audit_log(app_user, table_name, operation, row_data)
    VALUES (
        NULLIF(current_setting('app.user_login', true), ''),
        TG_TABLE_NAME,
        TG_OP,
        CASE WHEN TG_OP = 'DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END - 'password_hash'
    );
    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_audit_users
    AFTER INSERT OR UPDATE OR DELETE ON users
    FOR EACH ROW EXECUTE FUNCTION fn_audit_users();

-- ---------------------------------------------------------------------
--  Представления
-- ---------------------------------------------------------------------

-- Ведомость в читаемом виде (ФИО, факультет, предмет, балл, кем подтверждено)
CREATE VIEW v_statement AS
SELECT se.entry_id,
       se.applicant_id,
       a.full_name                       AS applicant_name,
       f.faculty_name,
       se.subject_id,
       s.subject_name,
       s.min_score,
       se.score,
       se.entered_at,
       se.is_confirmed,
       u.login                           AS confirmed_by_login,
       se.confirmed_at
FROM statement_entries se
JOIN applicants a ON a.applicant_id = se.applicant_id
JOIN faculties  f ON f.faculty_id   = a.faculty_id
JOIN subjects   s ON s.subject_id   = se.subject_id
LEFT JOIN users u ON u.user_id      = se.confirmed_by;

-- Сумма баллов абитуриента.
--   Сумма = баллы по подтверждённым предметам факультета
--         + средний балл аттестата × 10   (аттестат 5.00 даёт 50 баллов)
--   Абитуриент участвует в конкурсе, если по ВСЕМ предметам факультета
--   есть подтверждённый балл не ниже минимального.
CREATE VIEW v_applicant_totals AS
SELECT a.applicant_id,
       a.full_name,
       a.faculty_id,
       f.faculty_name,
       f.plan_count,
       a.certificate_avg,
       a.status,
       a.registered_at,
       COALESCE(SUM(se.score) FILTER (WHERE se.is_confirmed), 0)              AS exam_sum,
       ROUND(a.certificate_avg * 10, 1)                                        AS certificate_points,
       COALESCE(SUM(se.score) FILTER (WHERE se.is_confirmed), 0)
           + ROUND(a.certificate_avg * 10, 1)                                  AS total_score,
       (SELECT COUNT(*) FROM faculty_subjects fs
         WHERE fs.faculty_id = a.faculty_id)                                   AS required_count,
       COUNT(se.entry_id) FILTER (WHERE se.is_confirmed AND se.score >= s.min_score) AS passed_count
FROM applicants a
JOIN faculties f ON f.faculty_id = a.faculty_id
LEFT JOIN statement_entries se
       ON se.applicant_id = a.applicant_id
      AND se.subject_id IN (SELECT fs.subject_id FROM faculty_subjects fs
                             WHERE fs.faculty_id = a.faculty_id)
LEFT JOIN subjects s ON s.subject_id = se.subject_id
GROUP BY a.applicant_id, f.faculty_id;

-- Конкурсный рейтинг по факультетам.
-- Порядок: сумма баллов ↓, средний балл аттестата ↓, дата подачи ↑.
-- passes = TRUE, если абитуриент проходит в план набора.
CREATE VIEW v_rating AS
WITH t AS (
    SELECT v.*,
           (v.required_count > 0 AND v.passed_count = v.required_count) AS is_eligible
    FROM v_applicant_totals v
), r AS (
    SELECT t.*,
           CASE WHEN t.is_eligible THEN
               ROW_NUMBER() OVER (PARTITION BY t.faculty_id, t.is_eligible
                                  ORDER BY t.total_score DESC,
                                           t.certificate_avg DESC,
                                           t.registered_at)
           END AS rank_position
    FROM t
)
SELECT r.*,
       COALESCE(r.rank_position <= r.plan_count, FALSE) AS passes
FROM r;

-- ---------------------------------------------------------------------
--  Зачисление
-- ---------------------------------------------------------------------

-- Подводит итоги конкурса: прошедшим в план — «Зачислен», остальным — «Не зачислен».
-- Возвращает количество зачисленных.
CREATE OR REPLACE FUNCTION fn_run_enrollment() RETURNS INT
LANGUAGE plpgsql AS $$
DECLARE
    enrolled INT;
BEGIN
    UPDATE applicants a
       SET status = CASE WHEN r.passes THEN 'Зачислен' ELSE 'Не зачислен' END
      FROM v_rating r
     WHERE r.applicant_id = a.applicant_id;

    SELECT COUNT(*) INTO enrolled FROM applicants WHERE status = 'Зачислен';
    RETURN enrolled;
END;
$$;

-- Отменяет итоги (все снова «Подано»)
CREATE OR REPLACE FUNCTION fn_reset_enrollment() RETURNS VOID
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE applicants SET status = 'Подано' WHERE status <> 'Подано';
END;
$$;
