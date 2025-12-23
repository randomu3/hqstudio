-- Миграция: добавление поля Icon в таблицу Services
-- Дата: 2025-12-23

-- SQLite
ALTER TABLE Services ADD COLUMN Icon TEXT NOT NULL DEFAULT '🔧';

-- PostgreSQL (если используется)
-- ALTER TABLE "Services" ADD COLUMN "Icon" TEXT NOT NULL DEFAULT '🔧';

-- Обновление существующих услуг с подходящими иконками
UPDATE Services SET Icon = '🚪' WHERE Title LIKE '%Доводчик%' OR Title LIKE '%дверь%';
UPDATE Services SET Icon = '🔇' WHERE Title LIKE '%Шумоизоляция%' OR Title LIKE '%шумка%';
UPDATE Services SET Icon = '⚫' WHERE Title LIKE '%Антихром%' OR Title LIKE '%хром%';
UPDATE Services SET Icon = '💡' WHERE Title LIKE '%подсветка%' OR Title LIKE '%Ambient%' OR Title LIKE '%свет%';
UPDATE Services SET Icon = '📦' WHERE Title LIKE '%Комплект%' OR Title LIKE '%набор%';
