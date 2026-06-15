-- Création des rôles applicatifs PostgreSQL.
--
-- pokekool_app      : utilisé par l'API à runtime
-- pokekool_migrator : utilisé par EF Core pour les migrations (privilèges élevés)
--
-- En dev, l'API utilise le superuser pour faciliter les tests.
-- Ces rôles sont préparés pour la mise en production avec RLS activé.

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'pokekool_app') THEN
        CREATE ROLE pokekool_app LOGIN PASSWORD 'pokekool_app_dev';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'pokekool_migrator') THEN
        CREATE ROLE pokekool_migrator LOGIN PASSWORD 'pokekool_migrator_dev';
    END IF;
END$$;

-- Les permissions précises (GRANT sur tables) seront ajoutées par les migrations
-- une fois les tables créées.
