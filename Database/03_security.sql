-- =====================================================================
--  Скрипт 3. Разграничение доступа на уровне СУБД
--
--  Приложение подключается не суперпользователем postgres,
--  а отдельной ролью admission_app с минимально нужными правами
--  (принцип минимальных привилегий):
--    * журнал audit_log можно только дополнять и читать —
--      изменить или удалить записи журнала через приложение нельзя;
--    * создавать/удалять таблицы роль не может.
--
--  Выполняйте под пользователем postgres, подключившись к базе
--  admission_committee. Пароль роли можно поменять — тогда
--  поменяйте его и в appsettings.json приложения.
-- =====================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'admission_app') THEN
        CREATE ROLE admission_app LOGIN PASSWORD 'Adm1ss10n_App!';
    END IF;
END
$$;

-- Нельзя создавать объекты в схеме public
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT  USAGE  ON SCHEMA public TO admission_app;

-- Справочники и данные: обычная работа
GRANT SELECT                         ON roles             TO admission_app;
GRANT SELECT, INSERT, UPDATE         ON users             TO admission_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON faculties         TO admission_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON subjects          TO admission_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON faculty_subjects  TO admission_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON applicants        TO admission_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON statement_entries TO admission_app;

-- Журнал: только добавление (через триггеры) и чтение
GRANT SELECT, INSERT ON audit_log TO admission_app;
REVOKE UPDATE, DELETE, TRUNCATE ON audit_log FROM admission_app;

-- Представления
GRANT SELECT ON v_statement, v_applicant_totals, v_rating TO admission_app;

-- Последовательности для SERIAL-ключей
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO admission_app;

-- Функции зачисления
GRANT EXECUTE ON FUNCTION fn_run_enrollment()   TO admission_app;
GRANT EXECUTE ON FUNCTION fn_reset_enrollment() TO admission_app;
