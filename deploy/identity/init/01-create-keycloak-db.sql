\set ON_ERROR_STOP on

\getenv keycloak_db_name KEYCLOAK_DB_NAME
\getenv keycloak_db_user KEYCLOAK_DB_USER
\getenv keycloak_db_password KEYCLOAK_DB_PASSWORD

CREATE ROLE :"keycloak_db_user"
    WITH LOGIN
    PASSWORD :'keycloak_db_password'
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION;

CREATE DATABASE :"keycloak_db_name"
    OWNER :"keycloak_db_user";