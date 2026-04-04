import re
import csv
import os

def parse_aws_quiz_final(input_file, output_file):
    print(f"--- [FINAL V7] Parsing {input_file} 시작 ---")
    
    if not os.path.exists(input_file):
        print(f"[ERROR] '{input_file}' 파일이 없습니다. 경로를 확인해줘!")
        return

    # 1. 파일 읽기 (인코딩 대응)
    content = ""
    for enc in ['utf-8-sig', 'utf-8', 'cp949']:
        try:
            with open(input_file, 'r', encoding=enc) as f:
                content = f.read()
            break
        except UnicodeDecodeError:
            continue
    
    # 2. Q[숫자] 패턴으로 문제 블록 분할
    # (괄호를 써야 split 결과에 q_id가 포함됨)
    raw_blocks = re.split(r'Q(\d+)\s*\n', content)
    parsed_data = []
    
    # i=1부터 2씩 증가하며 (ID, 내용) 쌍으로 처리
    for i in range(1, len(raw_blocks), 2):
        q_id = raw_blocks[i]
        block = raw_blocks[i+1]
        
        try:
            # 헬퍼 함수: 연속 공백/줄바꿈 정리 및 CSV 깨짐 방지용 쉼표 치환
            def clean_text(text):
                if not text: return ""
                # 줄바꿈과 탭을 일반 공백 하나로 통합
                text = re.sub(r'\s+', ' ', text)
                return text.strip().replace(',', ';')

            # [1] Title 추출 (시작부터 A. 전까지)
            title_match = re.search(r'^(.*?)(?=A\.)', block, re.DOTALL)
            title = clean_text(title_match.group(1)) if title_match else "N/A"

            # [2] Options 추출 (정규식으로 각 알파벳 사이 구간 획득)
            opt_a = clean_text(re.search(r'A\.(.*?)(?=B\.)', block, re.DOTALL).group(1))
            opt_b = clean_text(re.search(r'B\.(.*?)(?=C\.)', block, re.DOTALL).group(1))
            opt_c = clean_text(re.search(r'C\.(.*?)(?=D\.)', block, re.DOTALL).group(1))
            opt_d = clean_text(re.search(r'D\.(.*?)(?=Answer:)', block, re.DOTALL).group(1))

            # [3] CorrectKey 추출 (Answer: [알파벳])
            ans_match = re.search(r'Answer:\s*([A-D])', block)
            correct_key = ans_match.group(1) if ans_match else "N/A"

            # [4] Explanation 추출 (핵심!)
            # Answer: [알파벳] "이후의 모든 텍스트"를 있는 그대로 긁어옴
            # 임의로 '해설:' 이라는 글자를 절대 붙이지 않음.
            exp_match = re.search(r'Answer:\s*[A-D]\s*(.*)', block, re.DOTALL)
            
            if exp_match:
                # 데이터 원문 그대로 (설명1, 설명2, URL 등이 텍스트에 있는 대로 나옴)
                explanation = clean_text(exp_match.group(1))
            else:
                explanation = "N/A"

            parsed_data.append([q_id, title, opt_a, opt_b, opt_c, opt_d, correct_key, explanation])
            
        except Exception as e:
            print(f"[WARN] Q{q_id} 파싱 실패: {e}")

    # 3. CSV 저장 (따옴표 처리를 통해 데이터 유실 방지)
    header = ['Id', 'Title', 'OptionA', 'OptionB', 'OptionC', 'OptionD', 'CorrectKey', 'Explanation']
    with open(output_file, 'w', encoding='utf-8-sig', newline='') as f:
        # QUOTE_MINIMAL: 데이터에 세미콜론이나 특수문자가 있어도 안전하게 묶어줌
        writer = csv.writer(f, quoting=csv.QUOTE_MINIMAL)
        writer.writerow(header)
        writer.writerows(parsed_data)
    
    print(f"--- [SUCCESS] {len(parsed_data)}개의 문제를 '{output_file}'로 저장 완료! ---")

if __name__ == "__main__":
    parse_aws_quiz_final('datas2.txt', 'questions2.csv')