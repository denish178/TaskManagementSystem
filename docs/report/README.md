# Final project report

Generates a **4–5 page** Word report (cover + written sections, figures, tables).

## Requirements

```powershell
pip install python-docx matplotlib
```

## Build

```powershell
python docs/report/build_final_report.py
```

Output: `docs/report/TaskManagementSystem_Final_Report.docx`

Figures are written to `docs/report/assets/` (architecture diagram, test chart, technology pie chart).

## Customise

Edit team names, abstract tone, or table data in `build_final_report.py`, then re-run the script.
