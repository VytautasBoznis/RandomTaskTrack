-- gen_random_uuid() lives in pgcrypto on older PostgreSQL. Harmless on 13+
-- where it is built in.
CREATE EXTENSION IF NOT EXISTS pgcrypto;
