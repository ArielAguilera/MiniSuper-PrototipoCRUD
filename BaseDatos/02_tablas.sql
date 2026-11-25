-- Minisúper El Ahorro - Actividad 5 (Prototipo CRUD)
-- Crear tablas (ejecutar conectado a la BD "minisuper")
-- Integrantes de Grupo: Ariel Aguilera

-- Tabla del catálogo de productos
CREATE TABLE productos (
    id      SERIAL PRIMARY KEY,                          -- PK autoincremental
    codigo  VARCHAR(30)    NOT NULL UNIQUE,              -- código de barras, no se repite
    nombre  VARCHAR(100)   NOT NULL,
    precio  NUMERIC(10, 2) NOT NULL CHECK (precio > 0),  -- precio en Bs.
    stock   INT            NOT NULL DEFAULT 0 CHECK (stock >= 0)
);

-- Datos de prueba para la demostración
INSERT INTO productos (codigo, nombre, precio, stock) VALUES
('7771234500011', 'Leche PIL Entera 1L',      7.50, 40),
('7771234500028', 'Arroz Grano de Oro 1kg',   9.00, 25),
('7771234500035', 'Aceite Fino 900ml',       16.50, 18),
('7771234500042', 'Azúcar Guabirá 1kg',       7.00, 30),
('7771234500059', 'Fideo Lazzaroni 400g',     5.50, 50);

-- Consulta para verificar
SELECT * FROM productos ORDER BY id;
