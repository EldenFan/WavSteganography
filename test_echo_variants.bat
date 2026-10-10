@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

set "SECONDS=60"
set "TEXT_COUNT=0"

:parseArgs
if "%~1"=="" goto argsDone
if /i "%~1"=="-s" goto argSeconds
if /i "%~1"=="-f" goto argFile
set /a TEXT_COUNT+=1
set "TEXT_!TEXT_COUNT!=%~1"
shift
goto parseArgs

:argSeconds
if "%~2"=="" goto usage
set "SECONDS=%~2"
shift
shift
goto parseArgs

:argFile
if "%~2"=="" goto usage
if not exist "%~2" (
    echo Файл со строками не найден: %~2
    exit /b 1
)
for /f "usebackq eol=# delims=" %%L in ("%~2") do (
    set /a TEXT_COUNT+=1
    set "TEXT_!TEXT_COUNT!=%%L"
)
shift
shift
goto parseArgs

:argsDone
if "%TEXT_COUNT%"=="0" goto usage

set "PROJECT_DIR=WavSteganography"
set "EXE=%PROJECT_DIR%\bin\Debug\net8.0\WavSteganographyConsole.exe"
set "WORK_DIR=Analys\echo_test_run"
set "TEST_AUDIO_DIR=TestAudio"
set "SRC_DIR=%WORK_DIR%\_sources"
set "CSV=%WORK_DIR%\results.csv"
set "TEXTS_CSV=%WORK_DIR%\texts.csv"
set "SUMMARY_PS=%~dp0summarize_echo_results.ps1"

set "ESC="
set "COL_GREEN=%ESC%[32m"
set "COL_RED=%ESC%[31m"
set "COL_DIM=%ESC%[90m"
set "COL_RESET=%ESC%[0m"

where ffmpeg >nul 2>&1
if errorlevel 1 (
    set "FFMPEG_OK=0"
) else (
    set "FFMPEG_OK=1"
)

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

if "%FFMPEG_OK%"=="0" (
    echo.
    echo ВНИМАНИЕ: ffmpeg не найден в PATH — блок постобработки
    echo ^(MP3 / шум / ресэмплинг^) будет пропущен для всех источников.
    echo Файлы из %TEST_AUDIO_DIR% не будут приведены к моно 44100 Гц: стерео-файлы дадут некорректный результат.
)

if exist "%WORK_DIR%" rmdir /s /q "%WORK_DIR%"
mkdir "%WORK_DIR%"

>"%CSV%" echo text_id,source,method,test,match,ber,ber_raw,snr_db,lsd_db
>"%TEXTS_CSV%" echo text_id,text
for /l %%I in (1,1,%TEXT_COUNT%) do >>"%TEXTS_CSV%" echo %%I,"!TEXT_%%I!"

echo.
echo Строк для теста: %TEXT_COUNT%
for /l %%I in (1,1,%TEXT_COUNT%) do echo   [%%I] !TEXT_%%I!
echo.
echo BER     - доля ошибочных информационных бит ^(заголовок + данные^) после декодирования.
echo BER_RAW - доля ошибочных решений детектора по отдельным блокам ^(до голосования и Хэмминга^).
echo Реальные файлы перед тестом приводятся к моно, 44100 Гц, 16 бит ^(source_mono.wav в %SRC_DIR%^).

rem --- Подготовка источников (один раз для всех строк) ---
set "SRC_COUNT=0"
mkdir "%SRC_DIR%"

echo.
echo === Генерация чистого тестового сигнала ^(%SECONDS% сек^) ===
mkdir "%SRC_DIR%\clean"
"%EXE%" generate "%SRC_DIR%\clean\clean.wav" %SECONDS% >"%SRC_DIR%\clean\generate.log" 2>&1
if errorlevel 1 (
    echo Не удалось сгенерировать сигнал, смотрите %SRC_DIR%\clean\generate.log
) else (
    call :addSource "%SRC_DIR%\clean\clean.wav" "clean" "CLEAN (синтетика)"
)

if not exist "%TEST_AUDIO_DIR%" (
    echo.
    echo Папка "%TEST_AUDIO_DIR%" не найдена — реальные файлы пропущены.
    echo Создайте её рядом со скриптом и положите туда .wav файлы.
) else (
    echo === Подготовка файлов из %TEST_AUDIO_DIR% ===
    for %%F in ("%TEST_AUDIO_DIR%\*.wav") do (
        set "PREP_DIR=%SRC_DIR%\%%~nF"
        mkdir "!PREP_DIR!"
        call :prepareSource "%%~fF" "!PREP_DIR!\source_mono.wav"
        if exist "!PREP_DIR!\source_mono.wav" (
            call :addSource "!PREP_DIR!\source_mono.wav" "%%~nF" "%%~nxF"
        ) else (
            echo Не удалось подготовить %%~nxF, смотрите prepare.log в !PREP_DIR!
        )
    )
)

if "%SRC_COUNT%"=="0" (
    echo Нет ни одного источника для теста.
    exit /b 1
)

rem --- Прогон всех строк по всем источникам ---
for /l %%T in (1,1,%TEXT_COUNT%) do call :runText %%T

echo.
echo Готово. Результаты — в папке "%WORK_DIR%" (подпапка tN на каждую строку, внутри — на каждый источник).
echo Сводная таблица: %CSV%
echo Список строк:    %TEXTS_CSV%

if exist "%SUMMARY_PS%" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%SUMMARY_PS%" -CsvPath "%CSV%" -OutDir "%WORK_DIR%"
) else (
    echo Не найден %SUMMARY_PS% — средние значения не посчитаны.
)
exit /b 0

:usage
echo Использование: %~nx0 [-s seconds_для_CLEAN] [-f файл_со_строками] "текст1" ["текст2" ...]
echo   Тестирует все варианты echo hiding на синтетическом CLEAN-сигнале
echo   и на каждом .wav файле из папки TestAudio\ — для каждой переданной строки.
echo   В конце выводятся средние значения по всем строкам ^(и сохраняются в summary_*.csv^).
echo   -s N     длительность синтетического сигнала в секундах ^(по умолчанию 60^)
echo   -f FILE  файл со строками: одна строка на линию, пустые и начинающиеся с # пропускаются
echo            ^(сохраняйте в UTF-8 без BOM^)
echo   Строки из -f и из аргументов объединяются.
echo   В тексте избегайте восклицательного знака, процента, каретки, амперсанда и кавычек.
echo Примеры:
echo   %~nx0 "Hello World" "Привет мир" "1234567890"
echo   %~nx0 -s 30 -f texts.txt
exit /b 1

:addSource
set /a SRC_COUNT+=1
set "SRC_PATH_%SRC_COUNT%=%~1"
set "SRC_NAME_%SRC_COUNT%=%~2"
set "SRC_DISPLAY_%SRC_COUNT%=%~3"
exit /b 0

:runText
set "TEXT_ID=%~1"
set "TEXT=!TEXT_%~1!"

echo.
echo ################################################################################
echo   Строка %TEXT_ID% из %TEXT_COUNT%: !TEXT!
echo ################################################################################

for /l %%S in (1,1,%SRC_COUNT%) do (
    set "CUR_WORK_DIR=%WORK_DIR%\t%TEXT_ID%\!SRC_NAME_%%S!"
    mkdir "!CUR_WORK_DIR!"
    call :runAllVariants "!SRC_PATH_%%S!" "!SRC_DISPLAY_%%S!"
)
exit /b 0

:prepareSource
rem Метод рассчитан на один канал: приводим файл к моно, 44100 Гц, 16 бит.
rem Иначе стерео-поток читается как один плоский массив, а атаки с -ac 1 ломают сетку блоков.
if "%FFMPEG_OK%"=="0" (
    copy /y "%~1" "%~2" >nul
    exit /b 0
)
ffmpeg -hide_banner -loglevel error -y -i "%~1" -ac 1 -ar 44100 -c:a pcm_s16le "%~2" >"%~dp2prepare.log" 2>&1
exit /b 0

:runAllVariants
set "SOURCE=%~1"
set "DISPLAY_NAME=%~2"

echo.
echo ================================================================================
echo   Источник: %DISPLAY_NAME%
echo   Путь:     %SOURCE%
echo   Логи:     %CUR_WORK_DIR%
echo ================================================================================
echo   Качество встраивания и базовое извлечение ^(без постобработки^):
call :padTo "Метод" 20 H1
call :padTo "Embed" 8 H2
call :padTo "Extract" 8 H3
call :padTo "Match" 7 H4
call :padTo "BER" 9 H5
call :padTo "BER_RAW" 9 H6
call :padTo "SNR,dB" 9 H7
call :padTo "LSD,dB" 9 H8
echo   !H1! !H2! !H3! !H4! !H5! !H6! !H7! !H8! Результат
echo   ------------------------------------------------------------------------------------------------------------

call :test echo-naive
call :test echo-window
call :test echo-repeat
call :test echo-repeat-spread
call :test echo-hamming

echo   ------------------------------------------------------------------------------------------------------------

if "%FFMPEG_OK%"=="1" (
    echo.
    echo   Устойчивость к постобработке ^(тот же встроенный out_*.wav^):
    call :padTo "Метод" 20 R1
    call :padTo "Обработка" 16 R2
    call :padTo "Статус" 14 R3
    call :padTo "Match" 7 R4
    call :padTo "BER" 9 R5
    call :padTo "BER_RAW" 9 R6
    echo   !R1! !R2! !R3! !R4! !R5! !R6!
    echo   ------------------------------------------------------------------------------------------

    for %%M in (echo-naive echo-window echo-repeat echo-repeat-spread echo-hamming) do (
        call :postproc %%M mp3-320 mp3 320
        call :postproc %%M mp3-192 mp3 192
        call :postproc %%M mp3-128 mp3 128
        call :postproc %%M mp3-64 mp3 64
        call :postproc %%M noise-0.01 noise 0.01
        call :postproc %%M noise-0.05 noise 0.05
        call :postproc %%M resample-22050 resample 22050
        call :postproc %%M resample-16000 resample 16000
    )

    echo   ------------------------------------------------------------------------------------------
)

exit /b 0

:test
set "M=%~1"
set "OUT=%CUR_WORK_DIR%\out_%M%.wav"
set "EMBED_STATUS=OK"
set "EXTRACT_STATUS=OK"
set "RESULT="
set "MATCH=-"
set "SNR=-"
set "LSD=-"
set "BER=-"
set "BERRAW=-"

call :padTo "%M%" 20 NAME_COL
<nul set /p ="  !NAME_COL! "

if exist "%OUT%" del "%OUT%"

"%EXE%" embed "%SOURCE%" "%OUT%" %M% "%TEXT%" >"%CUR_WORK_DIR%\embed_%M%.log" 2>&1
if errorlevel 1 set "EMBED_STATUS=ОШИБКА"

if "%EMBED_STATUS%"=="OK" (
    "%EXE%" compare "%SOURCE%" "%OUT%" >"%CUR_WORK_DIR%\compare_%M%.log" 2>&1
    if not errorlevel 1 (
        for /f "usebackq delims=" %%L in ("%CUR_WORK_DIR%\compare_%M%.log") do (
            set "CLINE=%%L"
            if "!CLINE:~0,5!"=="SNR: " set "SNR=!CLINE:~5!"
            if "!CLINE:~0,5!"=="LSD: " set "LSD=!CLINE:~5!"
        )
        set "SNR=!SNR: dB=!"
        set "LSD=!LSD: dB=!"
    )

    "%EXE%" extract "%OUT%" %M% >"%CUR_WORK_DIR%\extract_%M%.log" 2>&1
    if errorlevel 1 (
        set "EXTRACT_STATUS=ОШИБКА"
        set "MATCH=НЕТ"
    ) else (
        for /f "usebackq delims=" %%L in ("%CUR_WORK_DIR%\extract_%M%.log") do (
            set "LINE=%%L"
            if "!LINE:~0,8!"=="Result: " set "RESULT=!LINE:~8!"
        )
        if "!RESULT!"=="%TEXT%" (set "MATCH=ДА") else (set "MATCH=НЕТ")
    )

    "%EXE%" ber "%OUT%" %M% "%TEXT%" >"%CUR_WORK_DIR%\ber_%M%.log" 2>&1
    if not errorlevel 1 (
        for /f "usebackq delims=" %%L in ("%CUR_WORK_DIR%\ber_%M%.log") do (
            set "BLINE=%%L"
            if "!BLINE:~0,5!"=="BER: " set "BER=!BLINE:~5!"
            if "!BLINE:~0,9!"=="BER_RAW: " set "BERRAW=!BLINE:~9!"
        )
    )
) else (
    set "EXTRACT_STATUS=-"
)

call :padTo "%EMBED_STATUS%" 8 E_COL
call :padTo "%EXTRACT_STATUS%" 8 X_COL
call :padTo "%MATCH%" 7 M_COL
call :padTo "%BER%" 9 BER_COL
call :padTo "%BERRAW%" 9 BERRAW_COL
call :padTo "%SNR%" 9 SNR_COL
call :padTo "%LSD%" 9 LSD_COL

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

echo !E_COL! !X_COL! !M_COLORED! !BER_COL! !BERRAW_COL! !SNR_COL! !LSD_COL! !SHOWN_RESULT!

call :writeCsv "%M%" "embed" "%MATCH%" "%BER%" "%BERRAW%" "%SNR%" "%LSD%"
exit /b 0

:postproc
set "PM=%~1"
set "PPROFILE=%~2"
set "PKIND=%~3"
set "PPARAM=%~4"
set "PSRC=%CUR_WORK_DIR%\out_%PM%.wav"
set "PROC=%CUR_WORK_DIR%\proc_%PM%_%PPROFILE%.wav"
set "PTMP=%CUR_WORK_DIR%\tmp_%PM%_%PPROFILE%"
set "PLOG=%CUR_WORK_DIR%\proc_%PM%_%PPROFILE%.log"
set "P_STATUS=OK"
set "P_MATCH=-"
set "P_BER=-"
set "P_BERRAW=-"

if not exist "%PSRC%" (
    set "P_STATUS=НЕТ ФАЙЛА"
    goto :postprocPrint
)

if exist "%PROC%" del "%PROC%"

if "%PKIND%"=="mp3" (
    ffmpeg -hide_banner -loglevel error -y -i "%PSRC%" -b:a %PPARAM%k "%PTMP%.mp3" >"%PLOG%" 2>&1
    ffmpeg -hide_banner -loglevel error -y -i "%PTMP%.mp3" -ar 44100 -ac 1 -c:a pcm_s16le "%PROC%" >>"%PLOG%" 2>&1
) else if "%PKIND%"=="noise" (
    ffmpeg -hide_banner -loglevel error -y -i "%PSRC%" -filter_complex "anoisesrc=color=white:amplitude=%PPARAM%:sample_rate=44100:seed=42[n];[0:a][n]amix=inputs=2:duration=first:dropout_transition=0:normalize=0[out]" -map "[out]" -ar 44100 -ac 1 -c:a pcm_s16le "%PROC%" >"%PLOG%" 2>&1
) else if "%PKIND%"=="resample" (
    ffmpeg -hide_banner -loglevel error -y -i "%PSRC%" -ar %PPARAM% -ac 1 -c:a pcm_s16le "%PTMP%.wav" >"%PLOG%" 2>&1
    ffmpeg -hide_banner -loglevel error -y -i "%PTMP%.wav" -ar 44100 -ac 1 -c:a pcm_s16le "%PROC%" >>"%PLOG%" 2>&1
)

if not exist "%PROC%" (
    set "P_STATUS=ОШИБКА FFMPEG"
    goto :postprocPrint
)

set "P_RESULT="
"%EXE%" extract "%PROC%" %PM% >"%CUR_WORK_DIR%\extract_%PM%_%PPROFILE%.log" 2>&1
if errorlevel 1 (
    set "P_STATUS=ОШИБКА ИЗВЛ"
    set "P_MATCH=НЕТ"
) else (
    for /f "usebackq delims=" %%L in ("%CUR_WORK_DIR%\extract_%PM%_%PPROFILE%.log") do (
        set "PLINE=%%L"
        if "!PLINE:~0,8!"=="Result: " set "P_RESULT=!PLINE:~8!"
    )
    if "!P_RESULT!"=="%TEXT%" (set "P_MATCH=ДА") else (set "P_MATCH=НЕТ")
)

"%EXE%" ber "%PROC%" %PM% "%TEXT%" >"%CUR_WORK_DIR%\ber_%PM%_%PPROFILE%.log" 2>&1
if not errorlevel 1 (
    for /f "usebackq delims=" %%L in ("%CUR_WORK_DIR%\ber_%PM%_%PPROFILE%.log") do (
        set "BLINE=%%L"
        if "!BLINE:~0,5!"=="BER: " set "P_BER=!BLINE:~5!"
        if "!BLINE:~0,9!"=="BER_RAW: " set "P_BERRAW=!BLINE:~9!"
    )
)

:postprocPrint
if exist "%PTMP%.mp3" del "%PTMP%.mp3"
if exist "%PTMP%.wav" del "%PTMP%.wav"

call :padTo "%PM%" 20 PC1
call :padTo "%PPROFILE%" 16 PC2
call :padTo "%P_STATUS%" 14 PC3
call :padTo "%P_MATCH%" 7 PC4
call :padTo "%P_BER%" 9 PC5
call :padTo "%P_BERRAW%" 9 PC6

if "%P_MATCH%"=="ДА" (
    set "PC4C=%COL_GREEN%!PC4!%COL_RESET%"
) else if "%P_MATCH%"=="НЕТ" (
    set "PC4C=%COL_RED%!PC4!%COL_RESET%"
) else (
    set "PC4C=%COL_DIM%!PC4!%COL_RESET%"
)

echo   !PC1! !PC2! !PC3! !PC4C! !PC5! !PC6!

call :writeCsv "%PM%" "%PPROFILE%" "%P_MATCH%" "%P_BER%" "%P_BERRAW%" "-" "-"
exit /b 0

:writeCsv
rem строка: TEXT_ID; %1=метод %2=тест %3=match(ДА/НЕТ/-) %4=ber %5=ber_raw %6=snr %7=lsd
setlocal EnableDelayedExpansion
set "c_match=0"
if "%~3"=="ДА" set "c_match=1"
if "%~3"=="-" set "c_match="
set "c_ber=%~4"
set "c_raw=%~5"
set "c_snr=%~6"
set "c_lsd=%~7"
if "!c_ber!"=="-" set "c_ber="
if "!c_raw!"=="-" set "c_raw="
if "!c_snr!"=="-" set "c_snr="
if "!c_lsd!"=="-" set "c_lsd="
>>"%CSV%" echo %TEXT_ID%,"%DISPLAY_NAME%",%~1,%~2,!c_match!,!c_ber!,!c_raw!,!c_snr!,!c_lsd!
endlocal
exit /b 0

:padTo
setlocal EnableDelayedExpansion
set "v=%~1                                                                "
set "w=%~2"
call set "v=%%v:~0,%w%%%"
endlocal & set "%~3=%v%"
exit /b 0
