# Naive checkout

Базовый read → check → `Stock -= qty` в одной EF-транзакции **без** row lock и без условного UPDATE.

Нужен как:

1. Исходная «комфортная» реализация (baseline).
2. Регрессия на полигоне: при конкуренции должен ловить oversell (`Completed > initial_stock`).

Не использовать как default Dev после фикса остатка — см. `../Fixes/`.
