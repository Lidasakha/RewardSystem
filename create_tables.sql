-- =============================================
-- Eksik tabloları oluşturma scripti
-- Şema: belek_reward_system
-- Bu scripti DBeaver'da çalıştırın
-- =============================================

-- 1. articles (Makaleler)
CREATE TABLE IF NOT EXISTS belek_reward_system.articles (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES belek_reward_system.users(id),
    title VARCHAR(500) NOT NULL DEFAULT '',
    journal VARCHAR(500),
    doi VARCHAR(200),
    year INTEGER,
    score INTEGER,
    status VARCHAR(50),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 2. article_teacher_assignments (Makale-Hoca Atamaları)
CREATE TABLE IF NOT EXISTS belek_reward_system.article_teacher_assignments (
    id BIGSERIAL PRIMARY KEY,
    article_id BIGINT NOT NULL REFERENCES belek_reward_system.articles(id),
    teacher_id BIGINT NOT NULL REFERENCES belek_reward_system.users(id),
    weight_percentage INTEGER NOT NULL DEFAULT 0,
    given_score INTEGER NOT NULL DEFAULT 0,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE
);

-- 3. projects (Projeler)
CREATE TABLE IF NOT EXISTS belek_reward_system.projects (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES belek_reward_system.users(id),
    title VARCHAR(500) NOT NULL DEFAULT '',
    description TEXT,
    start_date TIMESTAMP,
    end_date TIMESTAMP,
    status VARCHAR(50),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 4. presentations (Bildiriler)
CREATE TABLE IF NOT EXISTS belek_reward_system.presentations (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES belek_reward_system.users(id),
    title VARCHAR(500) NOT NULL DEFAULT '',
    conference VARCHAR(500),
    year INTEGER,
    status VARCHAR(50),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 5. patents (Patentler)
CREATE TABLE IF NOT EXISTS belek_reward_system.patents (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES belek_reward_system.users(id),
    title VARCHAR(500) NOT NULL DEFAULT '',
    patent_number VARCHAR(200),
    year INTEGER,
    status VARCHAR(50),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
