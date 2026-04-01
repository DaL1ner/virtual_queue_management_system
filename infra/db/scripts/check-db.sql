\dt

SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'roles' AND column_name = 'id';

SELECT id, code, name
FROM roles
ORDER BY id;
