@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

:: ═══════════════════════════════════════════════════════════════
::  FitTime — Автоматическое развёртывание
::  Устанавливает: winget, .NET 8 SDK, PostgreSQL 17
:: ═══════════════════════════════════════════════════════════════

:: ─────────────────────────────────────────────
:: Запрос прав администратора (нужны для установки)
:: ─────────────────────────────────────────────
net session >nul 2>&1
if errorlevel 1 (
    echo  Запрашиваю права администратора...
    powershell -NoProfile -Command ^
        "Start-Process cmd.exe -ArgumentList '/c \"%~f0\"' -Verb RunAs -Wait"
    exit /b
)

color 0A
echo.
echo  ╔══════════════════════════════════════════════════════╗
echo  ║          FitTime — Развёртывание приложения          ║
echo  ╚══════════════════════════════════════════════════════╝
echo.

set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%FitTime"
set "SQL_FILE=%SCRIPT_DIR%fittime_db.sql"
set "APPSETTINGS=%PROJECT_DIR%\appsettings.json"
set "PSQL_EXE="

:: ─────────────────────────────────────────────
:: 1. Проверка / установка winget
:: ─────────────────────────────────────────────
echo [1/6] Проверка winget (менеджер пакетов Windows)...

winget --version >nul 2>&1
if not errorlevel 1 (
    for /f "tokens=*" %%v in ('winget --version 2^>nul') do echo  ✓ winget уже установлен: %%v
    goto :winget_ok
)

echo  winget не найден. Скачиваю и устанавливаю...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "try { " ^
    "  $url = 'https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle'; " ^
    "  $tmp = Join-Path $env:TEMP 'winget_installer.msixbundle'; " ^
    "  Write-Host '  Скачиваю winget (~5 МБ)...'; " ^
    "  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " ^
    "  Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing; " ^
    "  Write-Host '  Устанавливаю...'; " ^
    "  Add-AppxPackage -Path $tmp -ForceApplicationShutdown; " ^
    "  Remove-Item $tmp -Force -ErrorAction SilentlyContinue " ^
    "} catch { Write-Host ('ОШИБКА: ' + $_.Exception.Message); exit 1 }"

if errorlevel 1 (
    color 0C
    echo.
    echo  [ОШИБКА] Не удалось установить winget автоматически.
    echo  Установите App Installer из Microsoft Store вручную:
    echo  ms-windows-store://pdp/?ProductId=9NBLGGH4NNS1
    pause
    exit /b 1
)

winget --version >nul 2>&1
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] winget установлен, но недоступен. Перезагрузите компьютер и запустите снова.
    pause
    exit /b 1
)
echo  ✓ winget установлен

:winget_ok

:: ─────────────────────────────────────────────
:: 2. Установка .NET 8 SDK (если нужно)
:: ─────────────────────────────────────────────
echo.
echo [2/6] Проверка .NET 8 SDK...

set "DOTNET_OK=0"
dotnet --version >nul 2>&1
if not errorlevel 1 (
    for /f "tokens=1 delims=." %%m in ('dotnet --version 2^>nul') do (
        if %%m GEQ 8 set "DOTNET_OK=1"
    )
)

if "%DOTNET_OK%"=="1" (
    for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do echo  ✓ .NET SDK уже установлен: v%%v
    goto :dotnet_ok
)

echo  .NET 8 SDK не найден. Устанавливаю через winget...
winget install --id Microsoft.DotNet.SDK.8 ^
    --silent ^
    --accept-package-agreements ^
    --accept-source-agreements

if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Не удалось установить .NET 8 SDK через winget.
    echo  Установите вручную: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

:: Обновляем PATH в текущем сеансе
set "PATH=C:\Program Files\dotnet;%PATH%"
echo  ✓ .NET 8 SDK установлен

:dotnet_ok

:: ─────────────────────────────────────────────
:: 3. Установка PostgreSQL 17 (если нужно)
:: ─────────────────────────────────────────────
echo.
echo [3/6] Проверка PostgreSQL...

call :find_psql
if defined PSQL_EXE (
    echo  ✓ PostgreSQL уже установлен
    echo    psql: %PSQL_EXE%
    goto :pg_ok
)

echo  PostgreSQL не найден. Устанавливаю через winget (версия 17)...
echo  Это может занять 3–10 минут. Пожалуйста, подождите...
echo.

:: --override передаёт параметры напрямую в инсталлятор EDB
:: --mode unattended   — тихая установка
:: --superpassword     — пароль пользователя postgres (root)
:: --serverport        — порт (5432 стандарт)
winget install --id PostgreSQL.PostgreSQL.17 ^
    --silent ^
    --accept-package-agreements ^
    --accept-source-agreements ^
    --override "--mode unattended --superpassword root --serverport 5432"

if errorlevel 1 (
    color 0C
    echo.
    echo  [ОШИБКА] Не удалось установить PostgreSQL через winget.
    echo  Установите вручную: https://www.postgresql.org/download/windows/
    echo  При установке задайте пароль суперпользователя: root
    pause
    exit /b 1
)

echo.
echo  Жду запуска сервиса PostgreSQL...
timeout /t 8 /nobreak >nul

:: Явно запускаем сервис на случай если не стартовал автоматически
sc start postgresql-x64-17 >nul 2>&1
timeout /t 3 /nobreak >nul

call :find_psql
if not defined PSQL_EXE (
    color 0C
    echo  [ОШИБКА] psql.exe не найден после установки.
    echo  Перезапустите компьютер и запустите скрипт снова.
    pause
    exit /b 1
)

echo  ✓ PostgreSQL 17 установлен и запущен
echo    psql: %PSQL_EXE%
echo    Пароль postgres: root

:pg_ok

:: ─────────────────────────────────────────────
:: 4. Параметры подключения к PostgreSQL
:: ─────────────────────────────────────────────
echo.
echo [4/6] Настройка подключения к PostgreSQL...
echo.
echo  Параметры по умолчанию соответствуют только что установленному PostgreSQL.
echo  (нажмите Enter, чтобы использовать значение по умолчанию)
echo.

set /p "PG_HOST=  PostgreSQL host [localhost]: "
if "!PG_HOST!"=="" set "PG_HOST=localhost"

set /p "PG_PORT=  PostgreSQL port [5432]: "
if "!PG_PORT!"=="" set "PG_PORT=5432"

set /p "PG_USER=  Пользователь [postgres]: "
if "!PG_USER!"=="" set "PG_USER=postgres"

set /p "PG_PASS=  Пароль [root]: "
if "!PG_PASS!"=="" set "PG_PASS=root"

set "PG_DB=fittime"
set "PGPASSWORD=!PG_PASS!"

echo.
echo  Проверяю подключение...
"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -c "SELECT 1;" postgres >nul 2>&1
if errorlevel 1 (
    color 0C
    echo.
    echo  [ОШИБКА] Не удалось подключиться к PostgreSQL.
    echo  Проверьте:
    echo    - Сервис PostgreSQL запущен  ^(sc start postgresql-x64-17^)
    echo    - Правильность хоста, порта и пароля
    pause
    exit /b 1
)
echo  ✓ Подключение к PostgreSQL успешно

:: ─────────────────────────────────────────────
:: 5. Создание базы данных и накат схемы
:: ─────────────────────────────────────────────
echo.
echo [5/6] Подготовка базы данных...

:: Проверяем, существует ли уже БД
"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -lqt 2>nul | findstr /i "\b!PG_DB!\b" >nul 2>&1
if not errorlevel 1 (
    echo.
    echo  База данных '!PG_DB!' уже существует.
    set /p "RECREATE=  Пересоздать базу данных (все данные будут удалены)? [Y/N]: "
    if /i "!RECREATE!"=="Y" (
        echo  Удаляю старую базу...
        "!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! ^
            -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='!PG_DB!';" postgres >nul 2>&1
        "!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! ^
            -c "DROP DATABASE IF EXISTS !PG_DB!;" postgres >nul 2>&1
        if errorlevel 1 (
            echo  [ОШИБКА] Не удалось удалить базу. Закройте pgAdmin/приложение и повторите.
            pause
            exit /b 1
        )
        echo  ✓ Старая база удалена
        goto :do_create_db
    ) else (
        echo  Используем существующую базу.
        goto :update_appsettings
    )
)

:do_create_db
echo  Создаю базу данных '!PG_DB!'...
"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! ^
    -c "CREATE DATABASE !PG_DB! WITH ENCODING='UTF8' LC_COLLATE='C' LC_CTYPE='C' TEMPLATE=template0;" ^
    postgres >nul 2>&1
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Не удалось создать базу данных.
    pause
    exit /b 1
)
echo  ✓ База данных создана

if not exist "!SQL_FILE!" (
    color 0C
    echo  [ОШИБКА] SQL-файл не найден: !SQL_FILE!
    pause
    exit /b 1
)

echo  Накатываю схему и тестовые данные...
"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! ^
    -d !PG_DB! -f "!SQL_FILE!" >"!SCRIPT_DIR!deploy_db.log" 2>&1
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Ошибка при выполнении SQL-скрипта.
    echo  Подробности: !SCRIPT_DIR!deploy_db.log
    pause
    exit /b 1
)
echo  ✓ Схема и тестовые данные загружены

:update_appsettings
echo  Обновляю appsettings.json...
set "NEW_CONN=Host=!PG_HOST!;Port=!PG_PORT!;Database=!PG_DB!;Username=!PG_USER!;Password=!PG_PASS!"

powershell -NoProfile -Command ^
    "$s = '!NEW_CONN!'; " ^
    "$j = Get-Content -Raw -Encoding UTF8 '!APPSETTINGS!' | ConvertFrom-Json; " ^
    "$j.ConnectionStrings.DefaultConnection = $s; " ^
    "$j | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 '!APPSETTINGS!'"

if errorlevel 1 (
    echo  [ПРЕДУПРЕЖДЕНИЕ] Не удалось обновить appsettings.json.
    echo  Вручную задайте строку подключения в: !APPSETTINGS!
    echo  DefaultConnection: !NEW_CONN!
) else (
    echo  ✓ appsettings.json обновлён
)

:: ─────────────────────────────────────────────
:: 6. Сборка и запуск приложения
:: ─────────────────────────────────────────────
echo.
echo [6/6] Сборка проекта...
echo.

cd /d "!PROJECT_DIR!"

dotnet restore --verbosity quiet
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Не удалось восстановить NuGet-пакеты. Проверьте интернет-соединение.
    pause
    exit /b 1
)

dotnet build --configuration Release --verbosity quiet
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Ошибка компиляции. Запустите для деталей:
    echo    dotnet build "!PROJECT_DIR!"
    pause
    exit /b 1
)

echo  ✓ Сборка успешна
echo.
echo  ════════════════════════════════════════════════════════
echo   Развёртывание завершено! Запускаю FitTime...
echo  ════════════════════════════════════════════════════════
echo.
echo   Данные для входа:
echo     Администратор  :  admin    /  admin123
echo     Менеджер       :  manager1 /  manager123
echo     Тренер         :  trainer1 /  trainer123
echo.

set "PGPASSWORD="
dotnet run --configuration Release --project "!PROJECT_DIR!\FitTime.csproj"

echo.
echo  Приложение завершило работу.
pause
endlocal
exit /b 0

:: ═══════════════════════════════════════════════════════════════
:: Подпрограмма: поиск psql.exe в стандартных путях PostgreSQL
:: ═══════════════════════════════════════════════════════════════
:find_psql
set "PSQL_EXE="
for %%v in (18 17 16 15 14) do (
    if exist "C:\Program Files\PostgreSQL\%%v\bin\psql.exe" (
        if not defined PSQL_EXE (
            set "PSQL_EXE=C:\Program Files\PostgreSQL\%%v\bin\psql.exe"
        )
    )
)
if not defined PSQL_EXE (
    where psql >nul 2>&1
    if not errorlevel 1 (
        for /f "tokens=*" %%p in ('where psql 2^>nul') do (
            if not defined PSQL_EXE set "PSQL_EXE=%%p"
        )
    )
)
exit /b 0
