"""One-off: build voice contributor resume PDF from structured content."""
from pathlib import Path

from fpdf import FPDF

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Denish_Murawala_Gujarati_Voice_Contributor_Resume.pdf"


class ResumePDF(FPDF):
    def header(self):
        pass

    def section_title(self, title: str):
        self.ln(2)
        self.set_font("Helvetica", "B", 11)
        self.set_text_color(30, 30, 30)
        self.cell(0, 7, title, new_x="LMARGIN", new_y="NEXT")
        self.set_font("Helvetica", "", 10)
        self.set_text_color(0, 0, 0)

    def bullet(self, text: str):
        w = self.w - self.l_margin - self.r_margin
        self.set_x(self.l_margin)
        self.multi_cell(w, 5, f"- {text}")


def main():
    pdf = ResumePDF()
    pdf.set_auto_page_break(auto=True, margin=15)
    pdf.add_page()
    pdf.set_margins(18, 18, 18)

    pdf.set_font("Helvetica", "B", 18)
    w = pdf.w - pdf.l_margin - pdf.r_margin
    pdf.cell(w, 10, "Denish Murawala", new_x="LMARGIN", new_y="NEXT")

    pdf.set_font("Helvetica", "", 10)
    pdf.multi_cell(
        w,
        5,
        "Gujarati (native) | English (fluent, natural Gujarati-influenced accent)\n"
        "Phone: +91-8401275343 | Email: murawaladenishwork@gmail.com\n"
        "LinkedIn: denish178 | GitHub: denish178",
    )

    pdf.section_title("Profile")
    pdf.multi_cell(
        w,
        5,
        "Native Gujarati speaker from Gujarat with a natural, authentic Gujarati-influenced "
        "English accent (not imitated). Comfortable reading short scripts aloud, following "
        "step-by-step written instructions in English, and recording clear audio on a "
        "smartphone or computer with a microphone. Detail-oriented, reliable, and able to "
        "complete guided recording and upload tasks independently in under 15 minutes per "
        "submission. B.Tech (CSE) background; experienced in remote work, structured rubrics, "
        "and quality-focused deliverables.",
    )

    pdf.section_title("Voice & language")
    for line in [
        "Gujarati: Native (home language; schooling in Gujarat)",
        "English: Fluent; spoken with natural Gujarati-influenced accent",
        "Recording: Smartphone / laptop with microphone; quiet environment; script reading",
        "Work style: Independent; follows written English guides; meets deadlines",
    ]:
        pdf.bullet(line)

    pdf.section_title("Relevant experience")
    pdf.set_font("Helvetica", "B", 10)
    pdf.multi_cell(
        w,
        5,
        "Expert Contributor / AI Trainer - AirdawgLabs (Remote) | July 2026 - Present",
    )
    pdf.set_font("Helvetica", "", 10)
    for b in [
        "Create and validate high-quality data; follow detailed written instructions and rubrics independently.",
        "Evaluate spoken and written outputs for clarity, accuracy, and adherence to guidelines.",
        "Provide structured feedback; strong fit for scripted recording and quality-controlled submissions.",
    ]:
        pdf.bullet(b)

    pdf.ln(1)
    pdf.set_font("Helvetica", "B", 10)
    pdf.multi_cell(
        w,
        5,
        "Frontend Developer Intern - OctaNet Services Pvt. Ltd. (Remote) | June 2024 - August 2024",
    )
    pdf.set_font("Helvetica", "", 10)
    pdf.bullet("Remote collaboration; clear communication in English with distributed teams.")

    pdf.section_title("Education")
    for b in [
        "B.Tech, Computer Science & Engineering - IIIT Bhagalpur | 2022 - 2026 | CGPA 7.7",
        "12th - Taxshila Vidhayalaya | GSEB | 2020 - 2021 | 83%",
        "10th - Narayan Vidhaya Vihar | GSEB | 2018 - 2019 | 82%",
    ]:
        pdf.bullet(b)

    pdf.section_title("Technical (supports remote recording & uploads)")
    pdf.bullet(
        "Comfortable with apps, browsers, file upload workflows, and written English documentation."
    )
    pdf.bullet("Tools: Postman, Swagger; Git/GitHub; Docker (basic).")

    pdf.section_title("Achievements")
    pdf.bullet("Top 10 - Smart India Internal Hackathon 2024")
    pdf.bullet("Ranked 152 - GeeksforGeeks Weekly Contest 202")

    pdf.section_title("Certifications")
    pdf.bullet("ServiceNow Certified Application Developer (CAD) | July 2025")
    pdf.bullet("ServiceNow Certified System Administrator (CSA) | July 2025")

    pdf.output(str(OUT))
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
