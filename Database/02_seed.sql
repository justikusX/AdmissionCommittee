-- =====================================================================
--  Скрипт 2. Тестовые данные
--  Пароли тестовых пользователей:
--     admin / admin123          (Администратор)
--     ivanov, petrova, ... / 12345   (Абитуриенты)
--  Пароли хранятся только в виде хэша PBKDF2-SHA256 (100 000 итераций).
-- =====================================================================

TRUNCATE audit_log, statement_entries, applicants, faculty_subjects,
         subjects, faculties, users, roles RESTART IDENTITY CASCADE;

INSERT INTO roles(role_name) VALUES ('Администратор'), ('Абитуриент');

INSERT INTO users(login, password_hash, full_name, role_id) VALUES
    ('admin', 'PBKDF2$100000$s2L9bodSybqfm55hrScN3g==$SrUEMx1zn9+sPWxMckKXVgXJ/yP+tth3iteHiQ9sN9A=', 'Администратор приёмной комиссии', 1);

INSERT INTO faculties(faculty_name, plan_count, description) VALUES
    ('Информационные технологии', 3, 'Программирование, базы данных, сети'),
    ('Экономика',                 2, 'Экономика и бухгалтерский учёт'),
    ('Юриспруденция',             2, 'Право и организация социального обеспечения');

INSERT INTO subjects(subject_name, min_score) VALUES
    ('Русский язык',   40),
    ('Математика',     39),
    ('Информатика',    44),
    ('Обществознание', 45),
    ('История',        35),
    ('Физика',         39);

INSERT INTO faculty_subjects(faculty_id, subject_id) VALUES
    (1, 1), (1, 2), (1, 3),
    (2, 1), (2, 2), (2, 4),
    (3, 1), (3, 4), (3, 5);

-- Иванов Иван Сергеевич
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('ivanov', 'PBKDF2$100000$ZVD2/YUnquvtI1OP72qLqg==$JdQ6qroTT2gjxtARSl6onwzBaE1YL+8Rz7Xr9CCgbDg=', 'Иванов Иван Сергеевич', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (2, 'Иванов Иван Сергеевич', '2008-03-14', '+79001110001', 'ivanov@mail.ru', 1, 4.75, TIMESTAMP '2026-06-20 10:00' + INTERVAL '37 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (1, 1, 82, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (1, 2, 78, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (1, 3, 85, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Петрова Анна Викторовна
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('petrova', 'PBKDF2$100000$M9VT5fG5i6ZCSi1fFifC2g==$hkC9R8ffJREjKeFNOt8AfmD6GaSZSd6hs9Fa302ffIU=', 'Петрова Анна Викторовна', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (3, 'Петрова Анна Викторовна', '2008-07-02', '+79001110002', 'petrova@mail.ru', 1, 4.90, TIMESTAMP '2026-06-20 10:00' + INTERVAL '74 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (2, 1, 91, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (2, 2, 88, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (2, 3, 90, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Сидоров Павел Андреевич
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('sidorov', 'PBKDF2$100000$MpMm5mKruPzI9PZdvYyR0g==$LK++HeJh9d3LfvDHHKJvOjZY+p7U/yn/FsC7C22z/rw=', 'Сидоров Павел Андреевич', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (4, 'Сидоров Павел Андреевич', '2007-11-23', '+79001110003', 'sidorov@mail.ru', 1, 3.80, TIMESTAMP '2026-06-20 10:00' + INTERVAL '111 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (3, 1, 65, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (3, 2, 60, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (3, 3, 58, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Кузнецова Мария Олеговна
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('kuznetsova', 'PBKDF2$100000$mC3UcMhUIeBV/O9ZvR3Mdg==$vybWROCfR0xWhlSXeh1j6WqwSZfHXYsrTgLCHmdIUg8=', 'Кузнецова Мария Олеговна', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (5, 'Кузнецова Мария Олеговна', '2008-01-30', '+79001110004', 'kuz@mail.ru', 1, 4.20, TIMESTAMP '2026-06-20 10:00' + INTERVAL '148 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (4, 1, 74, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (4, 2, 70, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (4, 3, 72, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Смирнов Дмитрий Игоревич
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('smirnov', 'PBKDF2$100000$Re0q3+2kpXsPDjxdd3N2dA==$M78LnN/IPx6llJTlDpWep/yP3Dcu8LetHP5kMJMOBpo=', 'Смирнов Дмитрий Игоревич', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (6, 'Смирнов Дмитрий Игоревич', '2008-05-09', '+79001110005', 'smirnov@mail.ru', 1, 4.50, TIMESTAMP '2026-06-20 10:00' + INTERVAL '185 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (5, 1, 80, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (5, 2, 35, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (5, 3, 77, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Волкова Елена Дмитриевна
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('volkova', 'PBKDF2$100000$0UeknY9RnsSwkppCGR16VQ==$D8El0jRErBYpvovaYq/EphQ786aZnsrguWbcc3IuDzQ=', 'Волкова Елена Дмитриевна', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (7, 'Волкова Елена Дмитриевна', '2008-09-17', '+79001110006', 'volkova@mail.ru', 2, 4.60, TIMESTAMP '2026-06-20 10:00' + INTERVAL '222 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (6, 1, 84, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (6, 2, 71, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (6, 4, 79, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Морозов Алексей Петрович
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('morozov', 'PBKDF2$100000$Dp3SkasPGOE3gXQ/151h4Q==$ICSfgFyKtKeAGs/wFnUtkAp3Q4pIp22kUqCi6rqluP8=', 'Морозов Алексей Петрович', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (8, 'Морозов Алексей Петрович', '2007-12-05', '+79001110007', 'morozov@mail.ru', 2, 3.95, TIMESTAMP '2026-06-20 10:00' + INTERVAL '259 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (7, 1, 68, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (7, 2, 62, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (7, 4, 66, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Лебедева Ольга Сергеевна
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('lebedeva', 'PBKDF2$100000$UZttGFEYl5+PWDO61faP0g==$MeQKH6AxOhf2Z1BPNz1Tn9X6eRqNYC3KPV7UFq8A/GU=', 'Лебедева Ольга Сергеевна', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (9, 'Лебедева Ольга Сергеевна', '2008-04-21', '+79001110008', 'lebedeva@mail.ru', 2, 4.35, TIMESTAMP '2026-06-20 10:00' + INTERVAL '296 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (8, 1, 77, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (8, 2, 69, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (8, 4, 73, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Новиков Артём Владимирович
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('novikov', 'PBKDF2$100000$HJ5u9i+SMtdXHThX2ATeew==$1laYtnU+BXhGp3pZ4PVK9Ukk3TbVcRFaKjTNux1Yc5g=', 'Новиков Артём Владимирович', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (10, 'Новиков Артём Владимирович', '2008-02-11', '+79001110009', 'novikov@mail.ru', 2, 4.10, TIMESTAMP '2026-06-20 10:00' + INTERVAL '333 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (9, 1, 70, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (9, 2, 66, FALSE, NULL, NULL),
    (9, 4, 75, FALSE, NULL, NULL);

-- Фёдорова Дарья Николаевна
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('fedorova', 'PBKDF2$100000$ayT1tD/3mrU5liNtKWkKig==$YEotdk2Ypv62QIb2RpSLLOWgdQNSbmkGbyKsNwP8Xzc=', 'Фёдорова Дарья Николаевна', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (11, 'Фёдорова Дарья Николаевна', '2008-06-28', '+79001110010', 'fedorova@mail.ru', 3, 4.80, TIMESTAMP '2026-06-20 10:00' + INTERVAL '370 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (10, 1, 88, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (10, 4, 83, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (10, 5, 79, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Соколов Никита Романович
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('sokolov', 'PBKDF2$100000$w15Y3Lieve6qeOtR8GL6LA==$MQ6BFEj/RrRjfyyyoIWdgvWmQhep8hAEocJA4kp+7E8=', 'Соколов Никита Романович', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (12, 'Соколов Никита Романович', '2007-10-03', '+79001110011', 'sokolov@mail.ru', 3, 4.00, TIMESTAMP '2026-06-20 10:00' + INTERVAL '407 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (11, 1, 72, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (11, 4, 68, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (11, 5, 64, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Попова Ксения Андреевна
INSERT INTO users(login, password_hash, full_name, role_id) VALUES ('popova', 'PBKDF2$100000$WERsp0YXrdtAUuyAHMuNPA==$/QNrs3SA2rNcBhg0FLTVpStj6uKPs1OBsi+JJXegW2M=', 'Попова Ксения Андреевна', 2);
INSERT INTO applicants(user_id, full_name, birth_date, phone, email, faculty_id, certificate_avg, registered_at) VALUES (13, 'Попова Ксения Андреевна', '2008-08-15', '+79001110012', 'popova@mail.ru', 3, 4.45, TIMESTAMP '2026-06-20 10:00' + INTERVAL '444 minutes');
INSERT INTO statement_entries(applicant_id, subject_id, score, is_confirmed, confirmed_by, confirmed_at) VALUES
    (12, 1, 79, TRUE, 1, TIMESTAMP '2026-06-25 12:00'),
    (12, 4, 76, TRUE, 1, TIMESTAMP '2026-06-25 12:00');

-- Записи, появившиеся в журнале при заполнении тестовыми данными, не нужны
TRUNCATE audit_log RESTART IDENTITY;
