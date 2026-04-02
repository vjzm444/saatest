@echo off
title AWS Quiz CSV Parser
echo [START] CSV 추출 작업을 시작합니다.

:: datas.txt 파일 존재 확인
if not exist "datas.txt" (
    echo [ERROR] 'datas.txt' 파일이 없습니다. 파일을 만들고 데이터를 넣어주세요.
    pause
    exit /b
)

:: 파이썬 실행
python parse_quiz.py

if %errorlevel% neq 0 (
    echo [FAIL] 파이썬 실행 중 오류가 발생했습니다.
) else (
    echo [SUCCESS] 변환 성공! 생성된 questions.csv를 사용하세요.
)

pause