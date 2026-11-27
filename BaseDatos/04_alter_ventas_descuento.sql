-- Actividad 8 - CR-01: Control de Descuentos y Trazabilidad
-- Script de migración sobre la tabla "ventas" (ejecutar en la BD "minisuper")
-- Es idempotente: se puede ejecutar más de una vez sin error.
-- Las ventas antiguas se conservan (se rellenan con descuento 0%).
-- Ariel Aguilera

BEGIN;

-- 1. Nuevas columnas (con DEFAULT para no romper las filas ya existentes)
ALTER TABLE ventas
    ADD COLUMN IF NOT EXISTS vendedor             VARCHAR(100)   NOT NULL DEFAULT 'Sin registrar',
    ADD COLUMN IF NOT EXISTS subtotal_bruto       NUMERIC(10, 2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS porcentaje_descuento NUMERIC(5, 2)  NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS monto_descuento      NUMERIC(10, 2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_final          NUMERIC(10, 2) NOT NULL DEFAULT 0;

-- 2. Rellenar ventas históricas: sin descuento, el bruto y el final son el "total" que ya existía
UPDATE ventas
   SET subtotal_bruto = total,
       total_final    = total
 WHERE subtotal_bruto = 0
   AND total_final    = 0
   AND total > 0;

-- 3. Restricciones (segunda línea de defensa: la BD también rechaza descuentos inválidos)
ALTER TABLE ventas DROP CONSTRAINT IF EXISTS chk_ventas_porcentaje_descuento;
ALTER TABLE ventas ADD CONSTRAINT chk_ventas_porcentaje_descuento
    CHECK (porcentaje_descuento >= 0 AND porcentaje_descuento <= 20);

ALTER TABLE ventas DROP CONSTRAINT IF EXISTS chk_ventas_monto_descuento;
ALTER TABLE ventas ADD CONSTRAINT chk_ventas_monto_descuento
    CHECK (monto_descuento >= 0 AND monto_descuento <= subtotal_bruto);

ALTER TABLE ventas DROP CONSTRAINT IF EXISTS chk_ventas_total_final;
ALTER TABLE ventas ADD CONSTRAINT chk_ventas_total_final
    CHECK (total_final >= 0 AND total_final = subtotal_bruto - monto_descuento);

COMMIT;

-- 4. Verificación de la estructura
SELECT column_name, data_type, column_default, is_nullable
  FROM information_schema.columns
 WHERE table_name = 'ventas'
 ORDER BY ordinal_position;

-- 5. CONSULTA DE AUDITORÍA PARA LA GERENCIA
--    ¿Qué vendedores han otorgado descuentos y por cuánto dinero?

SELECT vendedor,
       COUNT(*)             AS ventas_con_descuento,
       SUM(monto_descuento) AS total_descontado
  FROM ventas
 WHERE porcentaje_descuento > 0
 GROUP BY vendedor
 ORDER BY total_descontado DESC;

-- (Opcional) Prueba de que el CHECK de la BD funciona: debe fallar con error 23514
-- INSERT INTO ventas (cliente, total, vendedor, subtotal_bruto, porcentaje_descuento, monto_descuento, total_final)
-- VALUES ('Prueba', 75, 'Test', 100, 25, 25, 75);
