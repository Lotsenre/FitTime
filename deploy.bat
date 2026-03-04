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
echo [1/6] Проверка winget...

winget --version >nul 2>&1
if not errorlevel 1 (
    for /f "tokens=*" %%v in ('winget --version 2^>nul') do echo  ✓ winget: %%v
    set "WINGET_EXE=winget"
    goto :winget_found
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
    echo  [ОШИБКА] Не удалось установить winget.
    echo  Установите App Installer вручную: ms-windows-store://pdp/?ProductId=9NBLGGH4NNS1
    pause & exit /b 1
)

:: После Add-AppxPackage PATH не обновился — ищем напрямую
set "WINGET_EXE="
if exist "%LOCALAPPDATA%\Microsoft\WindowsApps\winget.exe" (
    set "WINGET_EXE=%LOCALAPPDATA%\Microsoft\WindowsApps\winget.exe"
)
if not defined WINGET_EXE (
    for /f "delims=" %%i in ('powershell -NoProfile -Command "[Environment]::GetEnvironmentVariable(\"PATH\",\"Machine\")+\";\"+ [Environment]::GetEnvironmentVariable(\"PATH\",\"User\")"') do set "PATH=%%i"
    where winget >nul 2>&1
    if not errorlevel 1 set "WINGET_EXE=winget"
)
if not defined WINGET_EXE (
    echo  winget требует новой сессии. Перезапускаю скрипт...
    powershell -NoProfile -Command "Start-Process cmd.exe -ArgumentList '/c \"%~f0\"' -Verb RunAs -Wait"
    exit /b 0
)
echo  ✓ winget установлен: !WINGET_EXE!

:winget_found

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
"!WINGET_EXE!" install --id Microsoft.DotNet.SDK.8 --silent --accept-package-agreements --accept-source-agreements
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Не удалось установить .NET 8 SDK.
    echo  Установите вручную: https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)
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
    echo  ✓ PostgreSQL уже установлен: !PSQL_EXE!
    goto :pg_ok
)

echo  PostgreSQL не найден. Устанавливаю через winget (версия 17)...
echo  Это может занять 3-10 минут. Пожалуйста, подождите...
echo.
"!WINGET_EXE!" install --id PostgreSQL.PostgreSQL.17 --silent --accept-package-agreements --accept-source-agreements --override "--mode unattended --superpassword root --serverport 5432"
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Не удалось установить PostgreSQL.
    echo  Установите вручную: https://www.postgresql.org/download/windows/
    echo  При установке задайте пароль суперпользователя: root
    pause & exit /b 1
)
echo.
echo  Жду запуска сервиса PostgreSQL...
timeout /t 8 /nobreak >nul
sc start postgresql-x64-17 >nul 2>&1
timeout /t 3 /nobreak >nul
call :find_psql
if not defined PSQL_EXE (
    color 0C
    echo  [ОШИБКА] psql.exe не найден после установки.
    echo  Перезапустите компьютер и запустите скрипт снова.
    pause & exit /b 1
)
echo  ✓ PostgreSQL 17 установлен: !PSQL_EXE!

:pg_ok

:: ─────────────────────────────────────────────
:: 4. Параметры подключения
:: ─────────────────────────────────────────────
echo.
echo [4/6] Настройка подключения к PostgreSQL...
echo.
echo  Нажмите Enter чтобы использовать значение по умолчанию.
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
    echo  [ОШИБКА] Не удалось подключиться к PostgreSQL.
    echo  Проверьте: запущен ли сервис, верны ли хост/порт/пароль.
    echo  Запустить сервис: sc start postgresql-x64-17
    pause & exit /b 1
)
echo  ✓ Подключение успешно

:: ─────────────────────────────────────────────
:: 5. Создание базы данных и накат схемы
:: ─────────────────────────────────────────────
echo.
echo [5/6] Подготовка базы данных...

"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -lqt 2>nul | findstr /i "fittime" >nul 2>&1
if not errorlevel 1 (
    echo.
    echo  База данных 'fittime' уже существует.
    set /p "RECREATE=  Пересоздать (все данные будут удалены)? [Y/N]: "
    if /i "!RECREATE!"=="Y" (
        "!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='fittime';" postgres >nul 2>&1
        "!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -c "DROP DATABASE IF EXISTS fittime;" postgres >nul 2>&1
        echo  ✓ Старая база удалена
        goto :create_db
    ) else (
        echo  Используем существующую базу.
        goto :update_appsettings
    )
)

:create_db
echo  Создаю базу данных 'fittime'...
"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -c "CREATE DATABASE fittime WITH ENCODING='UTF8' LC_COLLATE='C' LC_CTYPE='C' TEMPLATE=template0;" postgres >nul 2>&1
if errorlevel 1 (
    color 0C & echo  [ОШИБКА] Не удалось создать базу данных. & pause & exit /b 1
)
echo  ✓ База данных создана

if not exist "!SQL_FILE!" (
    color 0C & echo  [ОШИБКА] SQL-файл не найден: !SQL_FILE! & pause & exit /b 1
)

echo  Накатываю схему и данные...
"!PSQL_EXE!" -h !PG_HOST! -p !PG_PORT! -U !PG_USER! -d fittime -f "!SQL_FILE!" >"!SCRIPT_DIR!deploy_db.log" 2>&1
if errorlevel 1 (
    color 0C
    echo  [ОШИБКА] Ошибка SQL-скрипта. Подробности: !SCRIPT_DIR!deploy_db.log
    pause & exit /b 1
)
echo  ✓ Схема и данные загружены

:update_appsettings
echo  Обновляю appsettings.json...
set "NEW_CONN=Host=!PG_HOST!;Port=!PG_PORT!;Database=fittime;Username=!PG_USER!;Password=!PG_PASS!"
powershell -NoProfile -Command ^
    "$s = '!NEW_CONN!'; $j = Get-Content -Raw -Encoding UTF8 '!APPSETTINGS!' | ConvertFrom-Json; $j.ConnectionStrings.DefaultConnection = $s; $j | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 '!APPSETTINGS!'" >nul 2>&1
echo  ✓ appsettings.json обновлён

:: ─────────────────────────────────────────────
:: 6. Сборка и запуск
:: ─────────────────────────────────────────────
echo.
echo [6/6] Сборка проекта...
echo.

cd /d "!PROJECT_DIR!"

dotnet restore --verbosity quiet
if errorlevel 1 (
    color 0C & echo  [ОШИБКА] Не удалось восстановить NuGet-пакеты. & pause & exit /b 1
)

dotnet build --configuration Release --verbosity quiet
if errorlevel 1 (
    color 0C & echo  [ОШИБКА] Ошибка компиляции. & pause & exit /b 1
)

echo  ✓ Сборка успешна
echo.
echo  ════════════════════════════════════════════════════
echo   Развёртывание завершено! Запускаю FitTime...
echo  ════════════════════════════════════════════════════
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

:: ─────────────────────────────────────────────
:: Подпрограмма: поиск psql.exe
:: ─────────────────────────────────────────────
:find_psql
set "PSQL_EXE="
for %%v in (18 17 16 15 14) do (
    if exist "C:\Program Files\PostgreSQL\%%v\bin\psql.exe" (
        if not defined PSQL_EXE set "PSQL_EXE=C:\Program Files\PostgreSQL\%%v\bin\psql.exe"
    )
)
if not defined PSQL_EXE (
    where psql >nul 2>&1
    if not errorlevel 1 (
        for /f "tokens=*" %%p in ('where psql 2^>nul') do if not defined PSQL_EXE set "PSQL_EXE=%%p"
    )
)
exit /b 0
