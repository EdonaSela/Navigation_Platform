#!/usr/bin/env python3
import argparse
import os
import xml.etree.ElementTree as ET


def parse_cobertura(path: str) -> float:
    tree = ET.parse(path)
    root = tree.getroot()
    line_rate = root.attrib.get("line-rate")
    if line_rate is None:
        raise ValueError(f"Could not read line-rate from {path}")
    return float(line_rate) * 100.0


def color_for_coverage(value: float) -> str:
    if value >= 90:
        return "#4c1"
    if value >= 80:
        return "#97CA00"
    if value >= 70:
        return "#dfb317"
    return "#e05d44"


def write_badge(label: str, value: float, out_path: str) -> None:
    text = f"{value:.2f}%"
    color = color_for_coverage(value)
    svg = f"""<svg xmlns="http://www.w3.org/2000/svg" width="170" height="20" role="img" aria-label="{label}: {text}">
  <linearGradient id="s" x2="0" y2="100%">
    <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
    <stop offset="1" stop-opacity=".1"/>
  </linearGradient>
  <rect rx="3" width="170" height="20" fill="#555"/>
  <rect rx="3" x="100" width="70" height="20" fill="{color}"/>
  <path fill="{color}" d="M100 0h4v20h-4z"/>
  <rect rx="3" width="170" height="20" fill="url(#s)"/>
  <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" font-size="11">
    <text x="50" y="15">{label}</text>
    <text x="135" y="15">{text}</text>
  </g>
</svg>
"""
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(svg)


def write_markdown_report(
    out_path: str,
    backend_value: float,
    frontend_value: float | None,
    backend_badge: str,
    frontend_badge: str | None,
) -> None:
    backend_status = "PASS" if backend_value >= 80 else "FAIL"
    frontend_status = "PASS" if (frontend_value is not None and frontend_value >= 80) else "FAIL"
    if frontend_value is None:
        frontend_status = "NOT RUN"

    lines = [
        "# Test Report",
        "",
        "## Coverage badges",
        "",
        f"![Backend coverage]({backend_badge})",
    ]
    if frontend_badge:
        lines.append(f"![Frontend coverage]({frontend_badge})")
    else:
        lines.append("Frontend badge: not generated (frontend coverage file not found).")

    lines.extend([
        "",
        "## Coverage summary",
        "",
        "| Area | Coverage | Target | Status |",
        "|---|---:|---:|---|",
        f"| Back-end | {backend_value:.2f}% | 80% | {backend_status} |",
    ])

    if frontend_value is None:
        lines.append("| Front-end | N/A | 80% | NOT RUN |")
    else:
        lines.append(f"| Front-end | {frontend_value:.2f}% | 80% | {frontend_status} |")

    lines.extend([
        "",
        "## BenchmarkDotNet",
        "",
        "No BenchmarkDotNet results were provided in this run.",
    ])

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate coverage badges and a test report.")
    parser.add_argument("--backend-cobertura", required=True, help="Path to backend Cobertura XML")
    parser.add_argument("--frontend-cobertura", default="", help="Path to frontend Cobertura XML")
    parser.add_argument("--output-dir", default="reports", help="Output directory")
    args = parser.parse_args()

    backend_coverage = parse_cobertura(args.backend_cobertura)
    frontend_coverage = None
    if args.frontend_cobertura and os.path.exists(args.frontend_cobertura):
        frontend_coverage = parse_cobertura(args.frontend_cobertura)

    badges_dir = os.path.join(args.output_dir, "badges")
    backend_badge = os.path.join(badges_dir, "backend-coverage.svg")
    frontend_badge = os.path.join(badges_dir, "frontend-coverage.svg")

    write_badge("backend coverage", backend_coverage, backend_badge)
    frontend_badge_for_report = None
    if frontend_coverage is not None:
        write_badge("frontend coverage", frontend_coverage, frontend_badge)
        frontend_badge_for_report = frontend_badge

    report_path = os.path.join(args.output_dir, "test-report.md")
    write_markdown_report(
        report_path,
        backend_coverage,
        frontend_coverage,
        "badges/backend-coverage.svg",
        "badges/frontend-coverage.svg" if frontend_badge_for_report else None,
    )

    print(f"Backend coverage: {backend_coverage:.2f}%")
    if frontend_coverage is None:
        print("Frontend coverage: not available")
    else:
        print(f"Frontend coverage: {frontend_coverage:.2f}%")
    print(f"Report written to: {report_path}")


if __name__ == "__main__":
    main()
