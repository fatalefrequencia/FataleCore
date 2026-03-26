import sys
import re

def audit_balance(filename):
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    stack = []
    # Match tags, accounting for multi-line
    # This regex matches <tag, </tag, and <tag ... />
    tag_pattern = re.compile(r'<(/?)([a-zA-Z0-9\._-]+)([^>]*?)(/?)>')

    for match in tag_pattern.finditer(content):
        is_closing = match.group(1) == '/'
        tag_name = match.group(2)
        is_self_closing = match.group(4) == '/'
        
        # Get line number
        ln = content.count('\n', 0, match.start()) + 1

        if is_self_closing or tag_name in ['img', 'input', 'br', 'hr', 'link']:
            continue

        if is_closing:
            if not stack:
                print(f"L{ln}: ERROR - Extra closing </{tag_name}>")
            else:
                top_tag, top_ln = stack.pop()
                if top_tag != tag_name:
                    print(f"L{ln}: MISMATCH - Closing </{tag_name}> but last open was <{top_tag}> at L{top_ln}")
        else:
            stack.append((tag_name, ln))

    print(f"\nFinal Stack Trace ({len(stack)} unclosed tags):")
    for t, l in stack:
        print(f"  <{t}> at L{l}")

if __name__ == "__main__":
    audit_balance(sys.argv[1])
