You are an expert software architect and senior code reviewer. 
Your task is to perform a deep, structured, multi‑layer architecture and SOLID analysis of the provided project.

## INPUT FILES
I am providing the following extracted project artifacts:
1. structure.txt — full project folder/file hierarchy
2. classes.txt — all class and interface declarations
3. dependencies.txt — dependency graph (.NET or Java)
4. metrics.txt — line counts and complexity indicators
5. SonarQube outputs
Use these files as the basis for your analysis.

---

## YOUR TASKS

### 1) High‑level architecture analysis
- Identify the architectural style (layered, modular, hexagonal, anemic domain, etc.)
- Detect layering violations (e.g., controllers calling repositories directly)
- Identify cyclic dependencies or suspicious module relationships
- Evaluate cohesion of modules and packages
- Identify God folders or God classes

### 2) SOLID principles evaluation
For each principle (SRP, OCP, LSP, ISP, DIP):
- Explain whether the project follows it
- Identify violations with concrete examples (based on structure + class names)
- Explain the impact of each violation
- Suggest how to fix or redesign the problematic areas

### 3) Coupling & Cohesion
- Identify tightly coupled modules or classes
- Detect overuse of concrete dependencies instead of abstractions
- Identify low‑cohesion modules or classes doing too much
- Suggest improvements (interfaces, adapters, boundaries)

### 4) Dependency graph analysis
Using dependencies.txt:
- Identify unnecessary libraries
- Detect risky or outdated dependencies
- Identify dependency clusters that indicate poor modularity
- Suggest dependency cleanup or restructuring

### 5) Complexity & maintainability
Using metrics.txt:
- Identify the most complex files
- Highlight potential God classes or methods
- Suggest refactoring strategies (splitting, extracting, reorganizing)

### 6) Testability & architecture fitness
- Evaluate how testable the architecture is
- Identify areas that block unit testing (tight coupling, static calls, hidden dependencies)
- Suggest improvements (DI, interfaces, ports/adapters)

### 7) Security & robustness (based on structure)
- Identify suspicious patterns (e.g., direct DB access from UI)
- Identify missing layers (e.g., service layer, domain layer)
- Suggest improvements

### 8) Provide a prioritized refactoring roadmap
Deliver a clear, actionable plan:
- High‑impact fixes (architecture, SOLID, coupling)
- Medium‑impact fixes (module boundaries, naming)
- Low‑impact fixes (structure cleanup)
- Quick wins

### 9) Provide a scoring summary
Give each category a score from 0–10:
- Architecture quality
- SOLID compliance
- Coupling/Cohesion
- Testability
- Maintainability
- Overall design quality

---

## OUTPUT FORMAT
Use the following structure:

1. Executive Summary (5–10 sentences)
2. Architecture Analysis
3. SOLID Analysis
4. Coupling & Cohesion
5. Dependency Graph Review
6. Complexity & Maintainability
7. Testability Review
8. Security & Robustness
9. Refactoring Roadmap (prioritized)
10. Scoring Summary (0–10 per category)

Be specific, concrete, and actionable. Base your reasoning strictly on the provided files.
