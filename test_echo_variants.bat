@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

rem =====================================================================
rem  Сравнение всех вариантов echo hiding на наборе аудиофайлов.
rem
rem  Использование:
rem    test_echo_variants.bat "текст для встраивания" [seconds_для_CLEAN]
rem
rem  Скрипт сам прогоняет все 5 вариантов (echo-naive, echo-window,
rem  echo-repeat, echo-repeat-spread, echo-hamming) для:
rem    - синтетического CLEAN-сигнала (два тона + шум, как в тестах),
rem      длиной [seconds] секунд (по умолчанию 60);
rem    - каждого *.wav файла из папки TestAudio\ рядом со скриптом.
rem
rem  Для каждого источника создаётся отдельная подпапка с логами и
rem  результатами: echo_test_run\<имя_источника>\
rem
rem  Пример:
rem    test_echo_variants.bat "Hello Echo Hiding!" 90
rem =====================================================================

if "%~1"=="" goto usage

set "TEXT=%~1"
set "SECONDS=%~2"
if "%SECONDS%"=="" set "SECONDS=60"

set "PROJECT_DIR=WavStefanography"
set "EXE=%PROJECT_DIR%\bin\Debug\net8.0\WavSteganographyConsole.exe"
set "WORK_DIR=echo_test_run"
set "TEST_AUDIO_DIR=TestAudio"

rem --- ESC для ANSI-цвета вшит в файл как реальный байт ---
set "ESC="
set "COL_GREEN=%ESC%[32m"
set "COL_RED=%ESC%[31m"
set "COL_DIM=%ESC%[90m"
set "COL_RESET=%ESC%[0m"

echo === Сборка проекта ===
dotnet build "%PROJECT_DIR%" -c Debug >"%TEMP%\echo_build.log" 2>&1
if errorlevel 1 (
    echo Сборка не удалась, смотрите %TEMP%\echo_build.log
    exit /b 1
)

if not exist "%EXE%" (
    echo Не найден исполняемый файл: %EXE%
    exit /b 1
)

if exist "%WORK_DIR%" rmdir /s /q "%WORK_DIR%"
mkdir "%WORK_DIR%"

echo.
echo Текст: %TEXT%

rem --- CLEAN: синтетический сигнал ---
set "CUR_NAME=clean"
set "CUR_WORK_DIR=%WORK_DIR%\%CUR_NAME%"
mkdir "%CUR_WORK_DIR%"

echo.
echo === Генерация чистого тестового сигнала ^(%SECONDS% сек^) ===
"%EXE%" generate "%CUR_WORK_DIR%\clean.wav" %SECONDS% >"%CUR_WORK_DIR%\generate.log" 2>&1
if errorlevel 1 (
    echo Не удалось сгенерировать сигнал, смотрите %CUR_WORK_DIR%\generate.log
) else (
    call :runAllVariants "%CUR_WORK_DIR%\clean.wav" "CLEAN (синтетика)"
)

rem --- Все файлы из TestAudio\ ---
if not exist "%TEST_AUDIO_DIR%" (
    echo.
    echo Папка "%TEST_AUDIO_DIR%" не найдена — реальные файлы пропущены.
    echo Создайте её рядом со скриптом и положите туда .wav файлы.
) else (
    for %%F in ("%TEST_AUDIO_DIR%\*.wav") do (
        set "CUR_NAME=%%~nF"
        set "CUR_WORK_DIR=%WORK_DIR%\!CUR_NAME!"
        mkdir "!CUR_WORK_DIR!"
        call :runAllVariants "%%~fF" "%%~nxF"
    )
)

echo.
echo Готово. Результаты — в папке "%WORK_DIR%" (отдельная подпапка на каждый источник).
exit /b 0

:usage
echo Использование: %~nx0 "текст" [seconds_для_CLEAN]
echo   Тестирует все варианты echo hiding на синтетическом CLEAN-сигнале
echo   и на каждом .wav файле из папки TestAudio\.
exit /b 1

rem ---------------------------------------------------------------------
rem :runAllVariants <путь_к_источнику> <отображаемое_имя>
rem  Использует уже установленную переменную CUR_WORK_DIR как папку для
rem  логов/результатов текущего источника.
rem ---------------------------------------------------------------------
:runAllVariants
set "SOURCE=%~1"
set "DISPLAY_NAME=%~2"

echo.
echo ================================================================================
echo   Источник: %DISPLAY_NAME%
echo   Путь:     %SOURCE%
echo   Логи:     %CUR_WORK_DIR%
echo ================================================================================
call :padTo "Метод" 20 H1
call :padTo "Embed" 8 H2
call :padTo "Extract" 8 H3
call :padTo "Match" 7 H4
echo   !H1! !H2! !H3! !H4! Результат
echo   ------------------------------------------------------------------------------

call :test echo-naive
call :test echo-window
call :test echo-repeat
call :test echo-repeat-spread
call :test echo-hamming

echo   ------------------------------------------------------------------------------
exit /b 0

rem ---------------------------------------------------------------------
rem :test <method> — прогоняет один вариант для текущего SOURCE, кладёт
rem out_*.wav и *.log в %CUR_WORK_DIR%, печатает строку результата.
rem ---------------------------------------------------------------------
:test
set "M=%~1"
set "OUT=%CUR_WORK_DIR%\out_%M%.wav"
set "EMBED_STATUS=OK"
set "EXTRACT_STATUS=OK"
set "RESULT="
set "MATCH=-"

call :padTo "%M%" 20 NAME_COL
<nul set /p ="  !NAME_COL! "

"%EXE%" embed "%SOURCE%" "%OUT%" %M% "%TEXT%" >"%CUR_WORK_DIR%\embed_%M%.log" 2>&1
if errorlevel 1 set "EMBED_STATUS=ОШИБКА"

if "%EMBED_STATUS%"=="OK" (
    "%EXE%" extract "%OUT%" %M% >"%CUR_WORK_DIR%\extract_%M%.log" 2>&1
    if errorlevel 1 (
        set "EXTRACT_STATUS=ОШИБКА"
    ) else (
        for /f "usebackq delims=" %%L in ("%CUR_WORK_DIR%\extract_%M%.log") do (
            set "LINE=%%L"
            if "!LINE:~0,8!"=="Result: " set "RESULT=!LINE:~8!"
        )
        if "!RESULT!"=="%TEXT%" (set "MATCH=ДА") else (set "MATCH=НЕТ")
    )
) else (
    set "EXTRACT_STATUS=-"
)

call :padTo "%EMBED_STATUS%" 8 E_COL
call :padTo "%EXTRACT_STATUS%" 8 X_COL
call :padTo "%MATCH%" 7 M_COL

if "%MATCH%"=="ДА" (
    set "M_COLORED=%COL_GREEN%!M_COL!%COL_RESET%"
) else if "%MATCH%"=="НЕТ" (
    set "M_COLORED=%COL_RED%!M_COL!%COL_RESET%"
) else (
    set "M_COLORED=%COL_DIM%!M_COL!%COL_RESET%"
)

set "SHOWN_RESULT=!RESULT!"
if not "!SHOWN_RESULT!"=="" (
    set "TRIMMED=!SHOWN_RESULT:~0,40!"
    if not "!TRIMMED!"=="!SHOWN_RESULT!" set "SHOWN_RESULT=!TRIMMED!..."
)

echo !E_COL! !X_COL! !M_COLORED! !SHOWN_RESULT!
exit /b 0

rem ---------------------------------------------------------------------
rem :padTo <text> <width> <outVar> — дополняет текст пробелами до width
rem ---------------------------------------------------------------------
:padTo
setlocal EnableDelayedExpansion
set "v=%~1                                                                "
set "w=%~2"
call set "v=%%v:~0,%w%%%"
endlocal & set "%~3=%v%"
exit /b 0
