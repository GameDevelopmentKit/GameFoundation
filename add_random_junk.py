#!/usr/bin/env python3
"""
Add random junk/dead code to all C# methods in the com.gdk.core package.
Properly handles expression-bodied members and single-line methods.
"""

import os
import re
import random
import string

def generate_random_string(length):
    """Generate a random string of lowercase letters"""
    return ''.join(random.choice(string.ascii_lowercase) for _ in range(length))

def generate_random_junk_code():
    """Generate truly random junk code with random values"""
    var_name = generate_random_string(random.randint(4, 8))

    junk_types = [
        # Simple variable declarations with random values
        f"var {var_name} = {random.randint(-9999, 9999)};",
        f"string {var_name} = \"{generate_random_string(random.randint(5, 15))}\";",
        f"bool {var_name} = {str(random.choice([True, False])).lower()};",
        f"float {var_name} = {random.uniform(-999.9, 999.9):.2f}f;",
        f"int {var_name} = {random.randint(0, 9999)};",
        f"double {var_name} = {random.uniform(-9999.9, 9999.9):.4f};",
        f"char {var_name} = '{random.choice(string.ascii_letters)}';",
        f"byte {var_name} = {random.randint(0, 255)};",
        f"long {var_name} = {random.randint(-999999, 999999)}L;",

        # More complex but safe operations
        f"var {var_name} = {random.randint(1, 100)} * {random.randint(1, 10)};",
        f"var {var_name} = \"{generate_random_string(6)}\" + \"{generate_random_string(4)}\";",
        f"int {var_name} = {random.randint(1, 50)} + {random.randint(1, 50)};",
        f"bool {var_name} = {random.randint(1, 100)} > {random.randint(1, 100)};",
    ]

    return random.choice(junk_types)

def add_junk_to_method_body(file_path):
    """Add random junk code to all method bodies in a C# file"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.splitlines()

        # Skip if it's an interface file or asmdef
        if file_path.endswith('.asmdef') or 'interface ' in content.lower():
            return 0

        modified = False
        method_count = 0
        i = 0

        while i < len(lines):
            line = lines[i]

            # Skip comments and attributes
            if line.strip().startswith('//') or line.strip().startswith('['):
                i += 1
                continue

            # Pattern for method signatures
            method_pattern = r'^\s*(public|private|protected|internal|static|virtual|override|async|sealed|abstract|extern|partial|\s)+.*\s+\w+\s*\([^)]*\)\s*(:?\s*base\([^)]*\))?\s*(?:{|=>)'

            if re.match(method_pattern, line):
                # Check if it's an abstract or extern method (no body)
                if 'abstract' in line or 'extern' in line or ';' in line:
                    i += 1
                    continue

                # Check if it's an expression-bodied member (=>)
                if '=>' in line:
                    # Expression bodied members - convert to block body and add junk
                    if line.strip().endswith(';'):
                        # Single line expression body
                        indent = len(line) - len(line.lstrip())
                        base_indent = ' ' * indent
                        inner_indent = ' ' * (indent + 4)

                        # Extract the expression
                        expr_match = re.search(r'=>\s*(.+);', line)
                        if expr_match:
                            expression = expr_match.group(1).strip()
                            # Get the method signature without the expression body
                            signature = line[:line.index('=>')].rstrip()

                            # Create block body with junk code
                            junk = generate_random_junk_code()
                            new_lines = [
                                signature,
                                base_indent + '{',
                                inner_indent + junk,
                                inner_indent + 'return ' + expression + ';',
                                base_indent + '}'
                            ]

                            lines[i:i+1] = new_lines
                            modified = True
                            method_count += 1
                            i += len(new_lines)
                            continue
                    else:
                        # Multi-line expression body - skip for now
                        i += 1
                        continue

                # Handle regular method with braces
                if '{' in line:
                    brace_on_same_line = True
                    brace_pos = line.index('{')
                else:
                    # Check if opening brace is on next line
                    if i + 1 < len(lines) and lines[i + 1].strip() == '{':
                        brace_on_same_line = False
                        i += 1
                    else:
                        i += 1
                        continue

                # Find the closing brace
                brace_count = 1
                method_start = i
                j = i + 1

                while j < len(lines) and brace_count > 0:
                    for char in lines[j]:
                        if char == '{':
                            brace_count += 1
                        elif char == '}':
                            brace_count -= 1
                            if brace_count == 0:
                                break
                    if brace_count == 0:
                        break
                    j += 1

                if brace_count == 0:
                    # Check if method body is empty or single line
                    if brace_on_same_line and '}' in line:
                        # Single line method like: public void Method() { }
                        if line.strip().endswith('{ }'):
                            # Empty method - add junk inside
                            indent = len(line) - len(line.lstrip())
                            inner_indent = ' ' * (indent + 4)
                            junk = generate_random_junk_code()

                            new_line = line.replace('{ }', '{ ' + junk + ' }')
                            lines[i] = new_line
                            modified = True
                            method_count += 1
                        elif '{' in line and '}' in line:
                            # Single line with content - add junk after opening brace
                            brace_idx = line.index('{')
                            close_idx = line.rindex('}')
                            junk = generate_random_junk_code()

                            new_line = line[:brace_idx+1] + ' ' + junk + ' ' + line[brace_idx+1:]
                            lines[i] = new_line
                            modified = True
                            method_count += 1
                    else:
                        # Multi-line method - add junk after opening brace
                        if brace_on_same_line:
                            # Brace on same line as signature
                            brace_line_idx = i
                        else:
                            # Brace on next line
                            brace_line_idx = method_start + 1

                        # Calculate indentation
                        next_line_idx = brace_line_idx + 1
                        if next_line_idx < len(lines):
                            next_line = lines[next_line_idx]
                            if next_line.strip():
                                indent = len(next_line) - len(next_line.lstrip())
                            else:
                                # Empty line, use method indent + 4
                                method_line = lines[method_start]
                                indent = len(method_line) - len(method_line.lstrip()) + 4
                        else:
                            method_line = lines[method_start]
                            indent = len(method_line) - len(method_line.lstrip()) + 4

                        # Insert junk code
                        junk = generate_random_junk_code()
                        junk_line = ' ' * indent + junk
                        lines.insert(brace_line_idx + 1, junk_line)
                        modified = True
                        method_count += 1
                        j += 1  # Adjust for inserted line

                i = j + 1
            else:
                i += 1

        if modified:
            # Write the modified content back
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write('\n'.join(lines))

        return method_count

    except Exception as e:
        print(f"[ERROR] processing {file_path}: {str(e)}")
        return 0

def process_directory(directory):
    """Process all C# files in the directory"""
    total_files = 0
    total_methods = 0

    for root, dirs, files in os.walk(directory):
        # Skip Editor folders
        if 'Editor' in root.split(os.sep):
            continue

        for file in files:
            if file.endswith('.cs') and not file.endswith('.asmdef'):
                file_path = os.path.join(root, file)
                relative_path = os.path.relpath(file_path, directory)

                print(f"Processing: {relative_path}")
                methods = add_junk_to_method_body(file_path)

                if methods > 0:
                    print(f"  [OK] Modified {methods} methods")
                    total_files += 1
                    total_methods += methods
                else:
                    print(f"  [SKIP] No methods modified")

    return total_files, total_methods

def main():
    """Main function"""
    # Get the directory where the script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))

    print("=" * 60)
    print("Adding random junk code to C# methods in com.gdk.core")
    print("=" * 60)

    # Process all C# files
    files_modified, methods_modified = process_directory(script_dir)

    print("=" * 60)
    print(f"[COMPLETE] Modified {methods_modified} methods across {files_modified} files")
    print("=" * 60)

if __name__ == "__main__":
    main()