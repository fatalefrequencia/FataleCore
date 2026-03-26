
import sys
import re

def check_balance(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    stack = []
    
    for i, line in enumerate(lines):
        ln = i + 1
        # Strip comments
        line = re.sub(r'{\s*/\*.*?\*/\s*}', '', line)
        line = re.sub(r'//.*', '', line)
        
        # Self-closing tags - ignore them
        line = re.sub(r'<([a-zA-Z0-9.]+)[^>]*/>', '', line)
        
        # Find opens and closes
        tokens = re.findall(r'(<[a-zA-Z0-9.]+[ >]|</[a-zA-Z0-9.]+>)', line)
        for token in tokens:
            if token.startswith('</'):
                tag = token[2:-1]
                if not stack:
                    print(f"L{ln}: Error! Extra closing tag {token}")
                else:
                    last_open = stack.pop()
                    if last_open['tag'] != tag:
                        print(f"L{ln}: Mismatch! Closing {token} but last open was <{last_open['tag']}> at L{last_open['ln']}")
            else:
                tag = token[1:].strip().split()[0].replace('>', '')
                stack.append({'tag': tag, 'ln': ln})

    print(f"\nFinal Stack ({len(stack)} tags):")
    for item in stack:
        print(f"  Unclosed <{item['tag']}> at L{item['ln']}")

if __name__ == "__main__":
    check_balance(sys.argv[1])
