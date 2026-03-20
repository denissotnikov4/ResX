-- ResX Platform — PostgreSQL initialization script
-- Creates separate databases for each microservice
-- Executed automatically by docker-entrypoint-initdb.d

CREATE DATABASE resx_identity;
CREATE DATABASE resx_users;
CREATE DATABASE resx_listings;
CREATE DATABASE resx_transactions;
CREATE DATABASE resx_messaging;
CREATE DATABASE resx_notifications;
CREATE DATABASE resx_charity;
CREATE DATABASE resx_disputes;
CREATE DATABASE resx_files;
