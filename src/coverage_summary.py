import xml.etree.ElementTree as ET
import sys

if len(sys.argv) < 2:
    print("Usage: python3 coverage_summary.py <path_to_cobertura_xml>")
    sys.exit(1)

tree = ET.parse(sys.argv[1])
root = tree.getroot()

classes = []
total_lines_valid = 0
total_lines_covered = 0

for package in root.findall(".//package"):
    for cls in package.findall(".//class"):
        name = cls.get("name")
        
        # Calculate lines from <line> elements
        all_lines = cls.findall(".//line")
        lines_valid = len(all_lines)
        lines_covered = len([l for l in all_lines if int(l.get("hits", 0)) > 0])
        
        if lines_valid > 0:
            line_rate = lines_covered / lines_valid
            classes.append((name, line_rate))
            
            total_lines_valid += lines_valid
            total_lines_covered += lines_covered

# Sort by line rate ascending
classes.sort(key=lambda x: x[1])

for name, rate in classes:
    print(f"{name}: {rate:.2%}")

if total_lines_valid > 0:
    overall_rate = total_lines_covered / total_lines_valid
    print(f"\nOVERALL COVERAGE: {overall_rate:.2%}")
    print(f"({total_lines_covered} / {total_lines_valid} lines covered)")
else:
    print("\nNo classes found for coverage calculation.")
