## Test plan 
in this context should define a sequence of automated steps to validate PLC/OPC UA system behavior. It should include:

Metadata: Name, description, unique ID.
Test Cases: Each with a name, expected input(s), expected output(s), and validation logic.
Signal Operations: Read/write specific OPC UA nodes, subscribe to signals, invoke methods, etc.
Timing/Order: Steps to execute in sequence, with possible delays or timeouts.
Pass/Fail Criteria: What constitutes a successful or failed test case.
Result Storage: Where to store the results (e.g., database, file, in-memory for demo).

# How it works
1] create/edit test plans and cases via UI
1] Stored created TestPlan in the database (table: TestPlans), with related test cases and required signals.
2] To execute a plan: Call TestRunOrchestrator
- Connects to the OPC UA server.
- Runs each test case (reading signals, checking values).
- Collects results and signal snapshots.
3] Results: The returned TestRuns object (with results and snapshots) should be saved to the database (TestRuns. TestResults, SignalSnaphsot)