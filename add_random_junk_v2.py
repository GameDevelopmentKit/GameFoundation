#!/usr/bin/env python3
"""
Add random junk/dead code to all C# methods in the com.gdk.core package - Version 2.
More aggressive method detection and modification.
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

def is_interface_or_abstract(content):
    """Check if file contains interface or abstract class definition"""
    # Check for interface
    if re.search(r'\binterface\s+\w+', content):
        return True
    # Check for abstract class
    if re.search(r'\babstract\s+class\s+\w+', content):
        return True
    return False

def add_junk_to_methods(file_path):
    """Add junk code to all methods in a C# file"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # Skip interface files
        if is_interface_or_abstract(content):
            return 0

        modified = False
        method_count = 0

        # Pattern to match methods - much more comprehensive
        # This matches various method signatures including constructors
        method_patterns = [
            # Standard methods
            r'(public|private|protected|internal|static|virtual|override|async|sealed|partial|new)\s+(?:(?:async\s+)?(?:static\s+)?(?:virtual\s+)?(?:override\s+)?(?:sealed\s+)?)*(?!class|interface|enum|struct)(?:\w+(?:<[^>]+>)?(?:\[\])?)\s+(\w+)\s*\([^)]*\)\s*(?::\s*(?:base|this)\s*\([^)]*\))?\s*\{',
            # Constructors
            r'(public|private|protected|internal|static)\s+(\w+)\s*\([^)]*\)\s*(?::\s*(?:base|this)\s*\([^)]*\))?\s*\{',
            # Destructors
            r'~(\w+)\s*\(\s*\)\s*\{',
            # Property getters/setters with body
            r'(get|set)\s*\{(?!\s*;\s*\})',
            # Expression bodied members that we'll convert
            r'(public|private|protected|internal|static|virtual|override)\s+.*?\s+\w+\s*\([^)]*\)\s*=>\s*[^;]+;',
        ]

        for pattern in method_patterns:
            # Find all matches
            matches = list(re.finditer(pattern, content))

            for match in reversed(matches):  # Process in reverse to maintain positions
                start_pos = match.start()

                # Find the opening brace position
                brace_pos = content.find('{', start_pos)

                # Handle expression bodied members
                if '=>' in match.group():
                    # Convert expression body to block body with junk
                    arrow_pos = content.find('=>', start_pos)
                    semicolon_pos = content.find(';', arrow_pos)

                    if arrow_pos != -1 and semicolon_pos != -1:
                        # Extract the expression
                        expression = content[arrow_pos + 2:semicolon_pos].strip()

                        # Get indentation
                        line_start = content.rfind('\n', 0, start_pos) + 1
                        indent = start_pos - line_start
                        base_indent = ' ' * indent
                        inner_indent = ' ' * (indent + 4)

                        # Build replacement
                        signature = content[start_pos:arrow_pos].rstrip()
                        junk = generate_random_junk_code()

                        # Check if it's a void method or has return
                        if 'void' in signature or expression == 'null' or not expression:
                            replacement = f"{signature}\n{base_indent}{{\n{inner_indent}{junk}\n{inner_indent}{expression};\n{base_indent}}}"
                        else:
                            replacement = f"{signature}\n{base_indent}{{\n{inner_indent}{junk}\n{inner_indent}return {expression};\n{base_indent}}}"

                        # Replace in content
                        content = content[:start_pos] + replacement + content[semicolon_pos + 1:]
                        modified = True
                        method_count += 1
                        continue

                if brace_pos == -1:
                    continue

                # Find the matching closing brace
                brace_count = 1
                pos = brace_pos + 1
                closing_brace_pos = -1

                while pos < len(content) and brace_count > 0:
                    if content[pos] == '{':
                        brace_count += 1
                    elif content[pos] == '}':
                        brace_count -= 1
                        if brace_count == 0:
                            closing_brace_pos = pos
                            break
                    pos += 1

                if closing_brace_pos == -1:
                    continue

                # Check if method body is not empty (has more than just whitespace)
                body_content = content[brace_pos + 1:closing_brace_pos].strip()

                # Get indentation for the junk code
                # Find the line containing the opening brace
                line_start = content.rfind('\n', 0, brace_pos) + 1
                line_end = content.find('\n', brace_pos)
                if line_end == -1:
                    line_end = len(content)

                # Calculate proper indentation
                method_line_start = content.rfind('\n', 0, start_pos) + 1
                method_indent = len(content[method_line_start:start_pos]) - len(content[method_line_start:start_pos].lstrip())
                inner_indent = ' ' * (method_indent + 4)

                # Add junk code after opening brace
                junk = generate_random_junk_code()
                junk_line = f"\n{inner_indent}{junk}"

                # Insert junk after opening brace
                new_content = content[:brace_pos + 1] + junk_line + content[brace_pos + 1:]
                content = new_content
                modified = True
                method_count += 1

        if modified:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)

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
                methods = add_junk_to_methods(file_path)

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
    print("Adding random junk code to C# methods in com.gdk.core - V2")
    print("=" * 60)

    # Process all C# files
    files_modified, methods_modified = process_directory(script_dir)

    print("=" * 60)
    print(f"[COMPLETE] Modified {methods_modified} methods across {files_modified} files")
    print("=" * 60)

if __name__ == "__main__":
    main()