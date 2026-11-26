#!/usr/bin/env python3
import glob
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: verify_coverage.py <test-results-directory>", file=sys.stderr)
        return 2

    reports = glob.glob(
        f"{sys.argv[1].rstrip('/')}/**/coverage.cobertura.xml",
        recursive=True,
    )
    coverage: dict[tuple[str, str, str], bool] = {}

    for report in reports:
        root = ET.parse(report).getroot()
        for class_element in root.findall(".//class"):
            class_name = class_element.get("name", "")
            if not class_name.startswith(
                ("Ascenso.Becas.Domain.", "Ascenso.Becas.Application.")
            ):
                continue

            project = class_name.split(".", 2)[2].split(".", 1)[0]
            source = class_element.get("filename", "").replace("\\", "/")
            for line in class_element.findall("./lines/line"):
                key = (project, source, line.get("number", ""))
                covered = int(line.get("hits", "0")) > 0
                coverage[key] = coverage.get(key, False) or covered

    if not coverage:
        print("No Domain/Application coverage lines were found.", file=sys.stderr)
        return 1

    covered_lines = sum(coverage.values())
    total_lines = len(coverage)
    percentage = covered_lines / total_lines * 100
    print(
        f"Domain + Application line coverage: "
        f"{covered_lines}/{total_lines} = {percentage:.2f}% (required: 80.00%)"
    )
    return 0 if percentage >= 80 else 1


if __name__ == "__main__":
    raise SystemExit(main())
