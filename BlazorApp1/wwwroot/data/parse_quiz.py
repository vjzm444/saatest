import re
import csv
import os

def parse_aws_quiz(input_file, output_file):
    print(f"--- [CLEAN START] Generating CSV without quotes ---")
    
    if not os.path.exists(input_file):
        print(f"[ERROR] '{input_file}' not found.")
        return

    content = ""
    for enc in ['utf-8-sig', 'utf-8', 'cp949']:
        try:
            with open(input_file, 'r', encoding=enc) as f:
                content = f.read()
            break
        except UnicodeDecodeError:
            continue
    
    # QUESTION NO: 기준으로 분할
    raw_blocks = re.split(r'QUESTION NO:', content)
    parsed_data = []
    q_count = 0

    for idx, block in enumerate(raw_blocks):
        if not block.strip() or idx == 0:
            continue
            
        try:
            # 1. Title (A. 전까지)
            title_match = re.search(r'^\s*\d+?\s*(.*?)(?=A\.)', block, re.DOTALL)
            title = title_match.group(1).strip().replace('\n', ' ').replace(',', ' ') if title_match else "N/A"

            # 2. Options (A, B, C, D) - 콤마는 세미콜론이나 공백으로 치환해서 CSV 깨짐 방지
            def clean(text):
                return text.strip().replace('\n', ' ').replace(',', ';')

            opt_a = clean(re.search(r'A\.(.*?)(?=B\.)', block, re.DOTALL).group(1))
            opt_b = clean(re.search(r'B\.(.*?)(?=C\.)', block, re.DOTALL).group(1))
            opt_c = clean(re.search(r'C\.(.*?)(?=D\.)', block, re.DOTALL).group(1))
            opt_d = clean(re.search(r'D\.(.*?)(?=Answer:)', block, re.DOTALL).group(1))

            # 3. CorrectKey (Answer: 뒤의 알파벳 1글자만)
            ans_match = re.search(r'Answer:\s*([A-D])', block)
            correct_key = ans_match.group(1) if ans_match else "N/A"

            # 4. Explanation
            exp_match = re.search(r'Explanation:(.*)', block, re.DOTALL)
            explanation = clean(exp_match.group(1)) if exp_match else "N/A"

            q_count += 1
            # 정확한 순서: Id,Title,OptionA,OptionB,OptionC,OptionD,CorrectKey,Explanation
            parsed_data.append([q_count, title, opt_a, opt_b, opt_c, opt_d, correct_key, explanation])
            
        except Exception as e:
            print(f"[WARN] Block {idx} parsing failed: {e}")

    # CSV 저장 (따옴표 없이 저장)
    header = ['Id', 'Title', 'OptionA', 'OptionB', 'OptionC', 'OptionD', 'CorrectKey', 'Explanation']
    with open(output_file, 'w', encoding='utf-8-sig', newline='') as f:
        # quoting=csv.QUOTE_NONE를 쓰고 escapechar를 설정해서 따옴표를 제거함
        writer = csv.writer(f, quoting=csv.QUOTE_NONE, escapechar='\\')
        writer.writerow(header)
        writer.writerows(parsed_data)
    
    print(f"[SUCCESS] {q_count} questions saved to '{output_file}'")

if __name__ == "__main__":
    parse_aws_quiz('datas.txt', 'questions.csv')