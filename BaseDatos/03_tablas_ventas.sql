DROP TABLE IF EXISTS detalle_ventas CASCADE;
DROP TABLE IF EXISTS ventas CASCADE;

-- 1. Tabla Maestro (Cabecera de la venta)
CREATE TABLE ventas (
    id SERIAL PRIMARY KEY,
    fecha TIMESTAMP NOT NULL DEFAULT NOW(),
    cliente VARCHAR(100) NOT NULL DEFAULT 'Cliente General',
    total NUMERIC(10, 2) NOT NULL DEFAULT 0 CHECK (total >= 0)
);

-- 2. Tabla Detalle (Productos vendidos en cada venta)
CREATE TABLE detalle_ventas (
    id SERIAL PRIMARY KEY,
    id_venta INT NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
    id_producto INT NOT NULL REFERENCES productos(id),
    cantidad INT NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10, 2) NOT NULL CHECK (precio_unitario >= 0),
    subtotal NUMERIC(10, 2) NOT NULL CHECK (subtotal >= 0)
);
